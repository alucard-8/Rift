// A falling chunk of the level: dark rock with ember cracks, so it reads as something torn
// out of the world rather than a generic boulder. Fades as it shatters.
Shader "RIFT/Debris"
{
    Properties
    {
        _Fade ("Fade", Range(0, 1)) = 1
        _Heat ("Heat", Range(0, 1)) = 0.7
        _RockColor ("Rock", Color) = (0.06, 0.04, 0.055, 1)
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

            CBUFFER_START(UnityPerMaterial)
                float _Fade, _Heat;
                half4 _RockColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float r = length(p);
                // Chipped silhouette.
                float edge = 0.82 + (RiftNoise(p * 3.1 + 4.7) - 0.5) * 0.22;
                float inside = smoothstep(edge, edge - 0.06, r);

                float cracks = 1.0 - smoothstep(0.0, 0.07, RiftVoronoiEdge(p * 3.2 + 1.3));
                float veins = smoothstep(0.35, 1.0, RiftFbm(p * 4.0 + 9.0)) * cracks;

                half3 col = _RockColor.rgb * (0.8 + 0.6 * RiftNoise(p * 8.0));
                col += _RiftAccent.rgb * veins * _Heat * 2.0;
                col += _RiftAccent.rgb * smoothstep(0.09, 0.0, abs(edge - r)) * 0.55;
                col += _RiftAccent.rgb * exp(-r * 7.0) * 0.12 * _Heat;

                float alpha = saturate(inside + exp(-r * 5.0) * 0.15) * _Fade;
                return half4(col * inside * _Fade, alpha);
            }
            ENDHLSL
        }
    }
}
