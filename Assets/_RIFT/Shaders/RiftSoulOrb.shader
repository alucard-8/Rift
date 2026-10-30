// A collectible soul: a small wisp with a bright core, two shy eyes and a slowly turning ring.
// _Fake turns it into the mimic: slit eyes and a smirk, so it reads slightly wrong up close.
Shader "RIFT/SoulOrb"
{
    Properties
    {
        _Fake ("Fake (0..1)", Range(0, 1)) = 0
        _Fade ("Fade", Range(0, 1)) = 1
        _Pulse ("Pulse (0..1)", Range(0, 1)) = 0
        _Wobble ("Wobble", Float) = 1
        _CoreColor ("Core", Color) = (1.8, 1.7, 1.4, 1)
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
                float _Fake, _Fade, _Pulse, _Wobble;
                half4 _CoreColor;
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

            float SdEllipse(float2 p, float2 r) { return (length(p / r) - 1.0) * min(r.x, r.y); }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 p = (i.uv - 0.5) * 2.0;   // -1..1
                p.x += sin(t * 2.3 + p.y * 3.0) * 0.015 * _Wobble;
                p.y += cos(t * 1.9) * 0.008 * _Wobble;

                float r = length(p);
                float flame = RiftNoise(p * 3.5 + float2(0.0, t * 1.2));
                float core = smoothstep(0.34, 0.05, r + (flame - 0.5) * 0.08);
                float glow = exp(-r * 4.2);

                float3 col = _RiftAccent.rgb * glow * (0.7 + 0.5 * _Pulse);
                col += _CoreColor.rgb * core * (0.9 + 0.6 * _Pulse);

                // Eyes: two dots looking a little sideways.
                float2 eye = p - float2(0.0, 0.03);
                float eyeW = lerp(0.045, 0.065, _Fake);
                float eyeH = lerp(0.060, 0.018, _Fake);
                float eyes = min(SdEllipse(eye - float2(-0.085, 0), float2(eyeW, eyeH)),
                                 SdEllipse(eye - float2( 0.085, 0), float2(eyeW, eyeH)));
                float eyeMask = smoothstep(0.006, -0.006, eyes) * core;
                col = lerp(col, half3(0.01, 0.01, 0.02), eyeMask * 0.9);

                // Smirk only on the mimic.
                float smirk = abs(p.y + 0.16 - (p.x * 0.5 + 0.05) * (p.x * 0.5 + 0.05) * 2.0);
                col += half3(0.9, 0.5, 0.35) * smoothstep(0.02, 0.0, smirk) * smoothstep(0.30, 0.0, r) * _Fake;

                // Turning ring.
                float ang = atan2(p.y, p.x);
                float ringR = 0.62 + 0.03 * sin(ang * 3.0 + t * 2.2);
                float ring = smoothstep(0.03, 0.0, abs(r - ringR));
                float gap = smoothstep(0.25, 0.55, sin(ang * 1.0 + t * 1.4) * 0.5 + 0.5);
                col += _RiftAccent.rgb * ring * gap * 0.5;

                float alpha = saturate(core + glow * 0.55 + ring * 0.25) * _Fade;
                return half4(col * _Fade, alpha);
            }
            ENDHLSL
        }
    }
}
