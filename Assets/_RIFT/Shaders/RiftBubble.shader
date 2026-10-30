// Speech bubble card. Rounded, dark, with a thin accent border and a soft inner scanline.
// Used on a UI Image inside a world-space Canvas; ignores the sprite texture and drives
// everything from the UV, so any white sprite works.
Shader "RIFT/Bubble"
{
    Properties
    {
        _Fade ("Fade", Range(0, 1)) = 1
        _Fill ("Fill", Color) = (0.015, 0.012, 0.025, 0.88)
        _BorderColor ("Border", Color) = (0.9, 0.9, 1.0, 1)
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.22
        _Glow ("Border Glow", Float) = 1.2

        // UI plumbing.
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float _Fade, _Radius, _Glow;
                half4 _Fill, _BorderColor, _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            float RoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                // Slight wobble so the card feels hand-drawn.
                p.x += (RiftNoise(float2(i.uv.y * 14.0, _Time.y * 0.7)) - 0.5) * 0.006;

                float d = RoundedBox(p, float2(0.5, 0.5), _Radius);
                float fill = smoothstep(0.004, -0.004, d);
                float border = smoothstep(0.012, 0.0, abs(d + 0.012));

                half3 accent = _BorderColor.rgb * 0.35 + _RiftAccent.rgb * 0.65;
                half3 col = _Fill.rgb * fill + accent * border * _Glow;

                // Faint scanlines and a glow just outside the card.
                col *= 1.0 - 0.05 * (0.5 + 0.5 * sin(i.uv.y * 80.0 - _Time.y * 2.0));
                col += accent * exp(-max(d, 0.0) * 26.0) * 0.25;

                float alpha = saturate(fill + border * 0.9 + exp(-max(d, 0.0) * 26.0) * 0.25);
                alpha *= i.color.a * _Color.a * _Fade;
                col *= i.color.rgb * _Color.rgb;
                return half4(col * _Fade, alpha);
            }
            ENDHLSL
        }
    }
}
