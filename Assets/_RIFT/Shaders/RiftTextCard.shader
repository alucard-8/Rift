// Soft lit card behind a wall line: a rounded glow that fades out toward its edges, with a slow
// drifting shimmer, so the words read as light written on the sky. Colour and alpha come from the sprite.
Shader "RIFT/TextCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Softness ("Edge Softness", Range(0.05, 0.5)) = 0.35
        _Shimmer ("Shimmer", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "RiftCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Softness, _Shimmer;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 world : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.world = TransformObjectToWorld(i.positionOS.xyz).xy;
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = abs(i.uv - 0.5) * 2.0;                       // 0 centre, 1 edge
                float edge = smoothstep(1.0, 1.0 - _Softness * 2.0, max(p.x, p.y * 0.85 + p.x * 0.15));
                float core = 1.0 - p.y * p.y;
                float shimmer = 1.0 - _Shimmer + _Shimmer * RiftFbm(i.world * 0.6 + float2(_Time.y * 0.15, 0));
                float a = edge * core * shimmer * i.color.a;
                return half4(i.color.rgb, a * 0.6);
            }
            ENDHLSL
        }
    }
}
