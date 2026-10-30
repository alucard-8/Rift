// A hooded stranger. Taller and leaner than the player's soul, with a bright collar of eyes,
// a speaking mouth and a faint scanline shimmer. The player's soul is drawn with SDFs too,
// so the two read as the same world. "Watcher" mode hollows the eyes and adds glitch bands.
Shader "RIFT/Npc"
{
    Properties
    {
        _Look ("Eye Look Offset", Vector) = (0, 0, 0, 0)
        _Talk ("Talking (0..1)", Range(0, 1)) = 0
        _Watcher ("Watcher (0..1)", Range(0, 1)) = 0
        _Fade ("Fade", Range(0, 1)) = 1
        _Glitch ("Glitch", Range(0, 1)) = 0
        _Outfit ("Outfit (0 none, 1 beggar, 2 rich)", Float) = 0
        _BodyColor ("Cloak", Color) = (0.03, 0.02, 0.05, 1)
        _RimColor ("Rim Light", Color) = (0.9, 0.9, 1.1, 1)
        _EyeColor ("Eyes", Color) = (1.6, 1.5, 1.4, 1)
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
                float4 _Look;
                float _Talk, _Watcher, _Fade, _Glitch, _Outfit;
                half4 _BodyColor, _RimColor, _EyeColor;
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

            float SdCloak(float2 p, float top, float bottom, float wTop, float wBottom)
            {
                float k = saturate((p.y - bottom) / (top - bottom));
                float w = lerp(wBottom, wTop, k);
                float2 d = float2(abs(p.x) - w, max(bottom - p.y, p.y - top));
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            float SdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            float SdCapsule(float2 p, float2 a, float2 b, float r)
            {
                float2 pa = p - a, ba = b - a;
                return length(pa - ba * saturate(dot(pa, ba) / dot(ba, ba))) - r;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                // Quad is 1.0 wide x 1.6 tall, origin near the waist.
                float2 p = (i.uv - float2(0.5, 0.46)) * float2(1.0, 1.6);

                // Watchers lean out of the air in torn vertical slices.
                float band = floor(i.uv.y * 34.0);
                float tear = (RiftNoise(float2(band * 0.37, t * 2.0)) - 0.5);
                p.x += tear * (0.012 + 0.05 * _Glitch) * saturate(_Watcher + _Glitch);

                float breathe = sin(t * 1.6) * 0.006;
                float hoodC = 0.42 + breathe;

                // Tall hood + narrow shoulders, then a cloak that widens to the hem.
                float hood = SdEllipse(p - float2(0.0, hoodC), float2(0.20, 0.27));
                float shoulders = SdEllipse(p - float2(0.0, 0.16 + breathe), float2(0.30, 0.14));
                float cloak = SdCloak(p, 0.20 + breathe, -0.78, 0.16, 0.29);
                float body = SmoothMin(SmoothMin(hood, shoulders, 0.07), cloak, 0.07);

                // Ragged hem.
                float hem = (RiftNoise(float2(p.x * 16.0, t * 1.2)) - 0.5) * 0.06;
                body = max(body, -(p.y + 0.74 + hem));

                float inside = smoothstep(0.006, -0.006, body);
                half3 col = _BodyColor.rgb;

                // The face cavity.
                float face = SdEllipse(p - float2(0.0, hoodC - 0.01), float2(0.135, 0.15));
                col = lerp(col, half3(0.005, 0.003, 0.01), smoothstep(0.01, -0.01, face));

                // Eyes: two bright slits that follow the player. Watching hollows them into rings.
                float2 look = _Look.xy * 0.028;
                float2 eyeBase = float2(0.0, hoodC + 0.01) + look;
                float eyeH = lerp(0.030, 0.011, saturate(_Watcher));
                float eyes = min(SdEllipse(p - eyeBase - float2(-0.055, 0), float2(0.027, eyeH)),
                                 SdEllipse(p - eyeBase - float2( 0.055, 0), float2(0.027, eyeH)));
                float eyeMask = smoothstep(0.004, -0.004, eyes) * smoothstep(0.0, -0.012, face);
                half3 eyeGlow = _EyeColor.rgb * (1.0 + _Watcher * 0.8);
                col = lerp(col, eyeGlow, eyeMask);

                // Hollow rings for the watcher: keep only the eye rim.
                float ring = smoothstep(0.008, 0.0, abs(eyes) - 0.006);
                col += _EyeColor.rgb * ring * _Watcher * 0.9 * smoothstep(0.0, -0.012, face);

                // Mouth: only while speaking.
                float mouthOpen = _Talk * (0.35 + 0.65 * abs(sin(t * 16.0)));
                float mouth = SdEllipse(p - float2(0.0, hoodC - 0.12), float2(0.045, 0.004 + mouthOpen * 0.022));
                col = lerp(col, _EyeColor.rgb * 0.85, smoothstep(0.006, -0.006, mouth) * (1.0 - _Watcher));

                // Scanline shimmer + accent rim.
                col *= 1.0 - 0.06 * (0.5 + 0.5 * sin(i.uv.y * 150.0 + t * 3.0));
                col += _RimColor.rgb * smoothstep(0.018, 0.0, abs(body)) * 0.5;
                col += _RiftAccent.rgb * smoothstep(0.05, 0.0, abs(body)) * (0.18 + 0.4 * _Talk);

                // Faint aura so they read against dark walls.
                half3 aura = _RiftAccent.rgb * exp(-max(body, 0.0) * 30.0) * (0.16 + 0.18 * _Talk);

                if (_Outfit > 1.5)
                {
                    // Rich: gold chain, black beret, baguette under the arm.
                    float chain = abs(SdEllipse(p - float2(0.0, 0.14), float2(0.17, 0.11))) - 0.009;
                    col = lerp(col, half3(2.2, 1.6, 0.45), smoothstep(0.004, -0.004, chain) * inside * step(p.y, 0.14));

                    float beret = min(SdEllipse(p - float2(0.03, hoodC + 0.27), float2(0.25, 0.075)),
                                      SdCapsule(p, float2(0.03, hoodC + 0.33), float2(0.05, hoodC + 0.38), 0.012));
                    float beretMask = smoothstep(0.005, -0.005, beret);
                    col = lerp(col, half3(0.025, 0.02, 0.03), beretMask);
                    col += _RimColor.rgb * smoothstep(0.012, 0.0, abs(beret)) * 0.5 * (1.0 - inside);

                    float2 a = float2(0.22, -0.36), b = float2(0.41, 0.34);
                    float bread = SdCapsule(p, a, b, 0.055);
                    float breadMask = smoothstep(0.005, -0.005, bread);
                    float along = dot(p - a, normalize(b - a));
                    float scores = smoothstep(0.75, 0.95, sin(along * 34.0));
                    col = lerp(col, lerp(half3(1.05, 0.62, 0.22), half3(1.5, 1.2, 0.72), scores), breadMask);
                    inside = max(inside, max(beretMask, breadMask));
                }
                else if (_Outfit > 0.5)
                {
                    // Beggar: faded headscarf with a knot, patches sewn on the cloak.
                    float scarf = hood;
                    float scarfMask = smoothstep(0.005, -0.005, scarf) * smoothstep(-0.01, 0.01, face);
                    col = lerp(col, half3(0.32, 0.1, 0.13), scarfMask);
                    float knot = SdEllipse(p - float2(0.21, hoodC - 0.2), float2(0.05, 0.035));
                    float knotMask = smoothstep(0.005, -0.005, knot);
                    col = lerp(col, half3(0.4, 0.13, 0.16), knotMask);
                    float patches = min(SdBox(p - float2(-0.12, -0.2), float2(0.065, 0.055)),
                                        SdBox(p - float2(0.13, -0.5), float2(0.055, 0.07)));
                    col = lerp(col, half3(0.26, 0.17, 0.09), smoothstep(0.004, -0.004, patches) * inside);
                    inside = max(inside, knotMask);
                }

                float alpha = saturate(inside + dot(aura, 0.33)) * _Fade;
                return half4((col * inside + aura * (1.0 - inside)) * _Fade, alpha);
            }
            ENDHLSL
        }
    }
}
