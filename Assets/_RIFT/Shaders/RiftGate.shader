// The goal: a tear in reality. A tall jagged slit whose edges crackle like lightning; inside, the world
// behind it swirls and refracts toward the level's accent light. It breathes, and tears wide open when reached.
// fbm after Inigo Quilez (MIT). Screen refraction idea after common Shadertoy "portal / heat haze" shaders.
Shader "RIFT/Gate"
{
    Properties
    {
        _Open ("Open Amount", Range(0, 4)) = 1
        _Width ("Slit Width (uv)", Float) = 0.12
        _Jag ("Edge Jaggedness", Float) = 0.06
        _Refraction ("Refraction", Range(0, 0.1)) = 0.03
        _Seed ("Seed", Float) = 0
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

            half4 _RiftAccent;
            TEXTURE2D(_CameraSortingLayerTexture); SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float _Open, _Width, _Jag, _Refraction, _Seed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 screenPos : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y + _Seed;
                half3 accent = _RiftAccent.rgb;
                float2 uv = i.uv;
                float v = uv.y;

                // Slit shape: tapered at both ends, edges displaced by moving noise (a crack, not a line).
                float breathe = 0.85 + 0.15 * sin(t * 1.7);
                float taper = pow(saturate(sin(v * 3.14159)), 0.7);
                float centre = 0.5 + (RiftFbm(float2(v * 5.0, t * 0.4)) - 0.5) * _Jag;
                float halfW = _Width * _Open * breathe * taper;
                float edgeNoise = (RiftNoise(float2(v * 40.0, t * 3.0)) - 0.5) * 0.02 * _Open;
                float d = abs(uv.x - centre) - halfW + edgeNoise;

                // Inside: refract the world behind, swirling, washed toward the accent light.
                float inside = smoothstep(0.004, -0.004, d);
                float2 screen = i.screenPos.xy / i.screenPos.w;
                float2 swirl = float2(RiftFbm(float2(v * 6.0 - t, uv.x * 8.0)), RiftFbm(float2(uv.x * 8.0, v * 6.0 + t))) - 0.5;
                half3 behind = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screen + swirl * _Refraction * _Open).rgb;
                behind = min(behind, 0.6);
                float core = exp(-abs(uv.x - centre) / max(halfW, 1e-3) * 2.5);
                half3 interior = behind * 0.55 + accent * 0.12 + accent * core * 0.35 + 0.25 * core * core;

                // The edge: a hot crackling line with flickering lightning brightness.
                float flicker = 0.7 + 0.6 * RiftNoise(float2(v * 12.0, t * 9.0));
                half3 edge = accent * smoothstep(0.012, 0.0, abs(d)) * 1.6 * flicker * taper;

                // Halo around the tear and a few sparks drifting off it.
                half3 halo = accent * exp(-max(d, 0.0) * 22.0) * 0.6 * taper;
                float2 sp = float2((uv.x - centre) * 6.0, v * 18.0 - t * 2.5);
                float spark = step(0.985, RiftHash(floor(sp))) * smoothstep(0.35, 0.0, length(frac(sp) - 0.5)) * exp(-max(d, 0.0) * 8.0);
                half3 col = interior * inside + edge + halo + accent * spark * 1.5;

                float alpha = saturate(inside + dot(edge + halo, 0.33) + spark);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
