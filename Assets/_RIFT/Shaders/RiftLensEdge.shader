// Lens rim: noisy energy ring + chromatic aberration + ripple refraction of what's behind it.
// Ring/aberration idea adapted from Shadertoy "Chromatic Aberration" patterns
// (e.g. https://www.shadertoy.com/view/Mds3zn — RGB split along the radial direction).
Shader "RIFT/LensEdge"
{
    Properties
    {
        _RingColorA ("Ring Color (Ember side)", Color) = (1.0, 0.55, 0.2, 1)
        _RingColorB ("Ring Color (Static side)", Color) = (0.2, 0.95, 1.0, 1)
        _RingIntensity ("Ring Intensity", Float) = 2.2
        _RingWidth ("Ring Width (world)", Float) = 0.07
        _Aberration ("Chromatic Aberration", Range(0, 0.05)) = 0.012
        _Ripple ("Ripple Strength", Range(0, 0.05)) = 0.01
        _Speed ("Animation Speed", Float) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "RiftCommon.hlsl"

            TEXTURE2D(_CameraSortingLayerTexture); SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _RingColorA;
                half4 _RingColorB;
                float _RingIntensity;
                float _RingWidth;
                float _Aberration;
                float _Ripple;
                float _Speed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 worldPos : TEXCOORD0; float4 screenPos : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldPos = world.xy;
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 rel = i.worldPos - _RiftCenter;
                float r = length(rel);
                float ang = atan2(rel.y, rel.x);

                // Wobbly rim radius driven by angular noise.
                float wobble = (RiftNoise(float2(ang * 3.0, t)) - 0.5) * _RingWidth * 2.0;
                float d = r - _RiftRadius - wobble;

                // Only work in a band around the rim.
                float band = 1.0 - smoothstep(0.0, _RingWidth * 6.0, abs(d));
                if (band <= 0.001) discard;

                float2 uv = i.screenPos.xy / i.screenPos.w;
                float2 dir = r > 1e-4 ? rel / r : float2(0, 0);
                dir.y *= _ScreenParams.x / _ScreenParams.y;

                // Ripple refraction + RGB split pushed outward from the centre.
                float ripple = sin(d * 60.0 - t * 6.0) * _Ripple * band;
                float split = _Aberration * band;
                half3 behind;
                behind.r = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, uv + dir * (ripple + split)).r;
                behind.g = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, uv + dir * ripple).g;
                behind.b = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, uv + dir * (ripple - split)).b;

                // Thin hot core + soft glow, colour shifting from Ember (outside) to Static (inside).
                float core = exp(-abs(d) / _RingWidth * 2.5);
                float sparks = step(0.97, RiftNoise(float2(ang * 40.0, t * 4.0))) * core;
                half3 ringCol = lerp(_RingColorB.rgb, _RingColorA.rgb, saturate(d / _RingWidth * 0.5 + 0.5));
                half3 glow = ringCol * (core + sparks * 2.0) * _RingIntensity;

                half alpha = band;
                return half4(behind * alpha + glow * band, alpha);
            }
            ENDHLSL
        }
    }
}
