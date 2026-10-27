// The player: a small hooded soul. A dark, slightly hunched cloak with a pale rim, two shy eyes in the hood,
// and a light in its chest that grows brighter (and the posture straighter) as it nears the rift.
// SDF primitives and smooth-min after Inigo Quilez's 2D distance functions (iquilezles.org, MIT).
Shader "RIFT/Soul"
{
    Properties
    {
        _Confidence ("Confidence (0..1)", Range(0, 1)) = 0.2
        _Facing ("Facing (-1 left, 1 right)", Float) = 1
        _Look ("Eye Look Offset", Vector) = (0, 0, 0, 0)
        _Blink ("Blink (0 open, 1 closed)", Range(0, 1)) = 0
        _BodyColor ("Cloak", Color) = (0.07, 0.045, 0.11, 1)
        [HDR] _RimColor ("Rim Light", Color) = (1.1, 1.0, 1.25, 1)
        [HDR] _EyeColor ("Eyes", Color) = (1.8, 1.7, 1.5, 1)
        [HDR] _PulseColor ("Voice Pulse (driven by script)", Color) = (0, 0, 0, 0)
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
                float _Confidence, _Facing, _Blink;
                float4 _Look;
                half4 _BodyColor, _RimColor, _EyeColor, _PulseColor;
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

            float SmoothMin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            float SdEllipse(float2 p, float2 r) { return (length(p / r) - 1.0) * min(r.x, r.y); }

            // Cloak: a trapezoid narrowing toward the shoulders.
            float SdCloak(float2 p, float top, float bottom, float wTop, float wBottom)
            {
                float k = saturate((p.y - bottom) / (top - bottom));
                float w = lerp(wBottom, wTop, k);
                float2 d = float2(abs(p.x) - w, max(bottom - p.y, p.y - top));
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float facing = _Facing >= 0 ? 1.0 : -1.0;
                // Quad is ~1.2 x 1.5: square units, origin near the waist; mirror x to face the move direction.
                float2 p = (i.uv - float2(0.5, 0.45)) * float2(1.2, 1.5);
                p.x *= facing;

                float breathe = sin(t * 2.2) * 0.008;
                float hunch = (1.0 - _Confidence) * 0.07;

                // Head/hood leans forward and down when shy; straightens as confidence grows.
                float2 hoodC = float2(hunch * 0.9, 0.26 - hunch * 0.6 + breathe);
                float hood = length(p - hoodC) - 0.19;
                float2 cp = p; cp.x -= hunch * 0.5 * saturate((p.y + 0.5) / 0.7);
                float cloak = SdCloak(cp, 0.16 + breathe, -0.52, 0.15, 0.27);

                // Ragged hem dissolving into light.
                float hem = (RiftNoise(float2(p.x * 18.0, t * 1.5)) - 0.5) * 0.05;
                cloak = max(cloak, -(p.y + 0.5 + hem));
                float body = SmoothMin(hood, cloak, 0.06);

                float inside = smoothstep(0.008, -0.008, body);
                half3 col = _BodyColor.rgb;

                // Face opening, slightly toward where it faces.
                float face = SdEllipse(p - hoodC - float2(0.045, -0.01), float2(0.12, 0.11));
                col = lerp(col, half3(0.01, 0.005, 0.02), smoothstep(0.01, -0.01, face));

                // Eyes: small, close together, glancing around; blink squashes them.
                float2 look = _Look.xy * float2(facing, 1.0) * 0.035;
                float eyeH = lerp(0.028, 0.004, _Blink);
                float2 eyeBase = hoodC + float2(0.05, -0.01) + look;
                float eyes = min(SdEllipse(p - eyeBase - float2(-0.035, 0), float2(0.018, eyeH)),
                                 SdEllipse(p - eyeBase - float2(0.035, 0), float2(0.018, eyeH)));
                col = lerp(col, _EyeColor.rgb, smoothstep(0.004, -0.004, eyes) * smoothstep(0.0, -0.01, face));

                // The inner light: grows, warms and pulses as confidence rises.
                float2 heart = float2(hunch * 0.3, -0.08);
                float dh = length(p - heart);
                float pulse = 0.85 + 0.15 * sin(t * (2.0 + _Confidence * 2.0));
                float lightR = lerp(16.0, 6.0, _Confidence);
                half3 glow = _RiftAccent.rgb * exp(-dh * lightR) * lerp(0.35, 1.8, _Confidence) * pulse;
                col += glow * inside;

                // Pale rim light around the silhouette.
                col += _RimColor.rgb * smoothstep(0.02, 0.0, abs(body)) * 0.55;
                col += _PulseColor.rgb * smoothstep(0.12, 0.0, abs(body));   // a voice spoke: the outline answers in its colour

                // Soft aura outside the body that grows with confidence.
                half3 aura = _RiftAccent.rgb * exp(-max(body, 0.0) * 28.0) * 0.25 * _Confidence;

                float alpha = saturate(inside + dot(aura, 0.33));
                return half4(col * inside + aura * (1.0 - inside), alpha);
            }
            ENDHLSL
        }
    }
}
