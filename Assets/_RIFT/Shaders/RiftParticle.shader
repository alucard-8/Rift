// Additive soft-dot particle. Colour (HDR) comes from the particle's vertex colour, so one material serves every effect.
Shader "RIFT/Particle"
{
    Properties
    {
        _Softness ("Softness", Range(0.01, 1)) = 0.6
        _Boost ("HDR Boost", Float) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Softness;
                float _Boost;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0;
                float a = smoothstep(1.0, 1.0 - _Softness, d);
                return half4(i.color.rgb * i.color.a * a * _Boost, 0);
            }
            ENDHLSL
        }
    }
}
