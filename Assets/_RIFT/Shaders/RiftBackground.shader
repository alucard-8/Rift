// Two skies behind the level. Ember: painted dusk with parallax hills. Static (inside the lens): synthwave grid void.
// fbm / value noise after Inigo Quilez (iquilezles.org/articles/fbm, MIT).
// Perspective grid floor after the many Shadertoy synthwave scenes (e.g. https://www.shadertoy.com/view/tsScRK).
Shader "RIFT/Background"
{
    Properties
    {
        [Header(Ember)]
        _SkyTop ("Sky Top", Color) = (0.13, 0.05, 0.16, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.98, 0.47, 0.22, 1)
        _SunColor ("Sun", Color) = (1.6, 1.15, 0.75, 1)
        _HillNear ("Hills Near", Color) = (0.12, 0.04, 0.10, 1)
        _HillFar ("Hills Far", Color) = (0.42, 0.14, 0.20, 1)
        _Parallax ("Parallax", Float) = 0.04
        _SunPos ("Sun Position (uv)", Vector) = (0.12, 0.6, 0, 0)
        _SunSize ("Sun Size", Float) = 0.075
        _Rain ("Rain Amount", Range(0, 1)) = 0
        _RainColor ("Rain Color", Color) = (0.6, 0.7, 0.85, 1)
        [Header(Static)]
        _StaticSky ("Sky Above Horizon", Color) = (0.01, 0.02, 0.05, 1)
        _VoidColor ("Void", Color) = (0.01, 0.02, 0.05, 1)
        _GridColor ("Grid", Color) = (0.15, 1.4, 1.8, 1)
        _HorizonGlow ("Horizon Glow", Color) = (1.6, 0.2, 1.0, 1)
        _GridDensity ("Grid Density", Float) = 10
        _GlitchBands ("Glitch Bands", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
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
                half4 _SkyTop, _SkyHorizon, _SunColor, _HillNear, _HillFar;
                float _Parallax;
                float4 _SunPos;
                float _SunSize, _Rain;
                half4 _RainColor;
                half4 _VoidColor, _StaticSky, _GridColor, _HorizonGlow;
                float _GridDensity, _GlitchBands;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 worldPos : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldPos = world.xy;
                o.uv = i.uv;
                return o;
            }

            half3 Ember(float2 uv, float camX, float t)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                half3 col = lerp(_SkyHorizon.rgb, _SkyTop.rgb, smoothstep(0.25, 1.0, uv.y));

                // Soft sun (or moon) disc with a glow halo.
                float2 sp = float2((uv.x - _SunPos.x) * aspect, uv.y - _SunPos.y);
                float sd = length(sp);
                col += _SunColor.rgb * (smoothstep(_SunSize, _SunSize * 0.9, sd) + 0.35 * exp(-sd * 9.0));

                // Drifting cloud streaks.
                float cloud = RiftFbm(float2(uv.x * 3.0 + camX * _Parallax * 0.3 + t * 0.01, uv.y * 14.0));
                col = lerp(col, _SkyTop.rgb * 1.6 + 0.05, smoothstep(0.55, 0.8, cloud) * smoothstep(0.45, 0.9, uv.y) * 0.5);

                // Three layers of hills, far to near, each with more parallax.
                [unroll] for (int l = 0; l < 3; l++)
                {
                    float k = l / 2.0;
                    float px = uv.x * (1.5 + l) + camX * _Parallax * (1.0 + l * 1.8) + l * 13.7;
                    float h = 0.5 - k * 0.1 + (RiftFbm(float2(px, l * 3.1)) - 0.5) * (0.22 + k * 0.08);
                    float edge = smoothstep(h + 0.003, h - 0.003, uv.y);
                    half3 hill = lerp(_HillFar.rgb, _HillNear.rgb, k);
                    // Atmospheric rim light on the top of each ridge.
                    hill += _SkyHorizon.rgb * 0.25 * smoothstep(h - 0.03, h, uv.y) * (1.0 - k);
                    col = lerp(col, hill, edge);
                }

                // Falling rain: thin slanted streaks in a few depth layers.
                if (_Rain > 0.0)
                {
                    [unroll] for (int r = 0; r < 3; r++)
                    {
                        float depth = 1.0 + r * 0.6;
                        float2 rp = float2(uv.x * aspect * 60.0 * depth + uv.y * 6.0 + camX * _Parallax * 20.0 * depth, uv.y * 3.0 * depth + t * (2.2 + r));
                        float col_id = floor(rp.x);
                        float drop = frac(rp.y + RiftHash(float2(col_id, r)) * 10.0);
                        float streak = smoothstep(0.0, 0.08, drop) * smoothstep(0.35, 0.1, drop);
                        float lane = smoothstep(0.5, 0.0, abs(frac(rp.x) - 0.5)) * step(0.55, RiftHash(float2(col_id, r + 7.0)));
                        col += _RainColor.rgb * streak * lane * _Rain * (0.35 / depth);
                    }
                }
                return col;
            }

            half3 Static(float2 uv, float camX, float t)
            {
                float horizon = 0.42;
                half3 col = lerp(_VoidColor.rgb, _StaticSky.rgb, smoothstep(horizon - 0.05, 1.0, uv.y));

                // Stars above the horizon.
                float2 cell = floor(uv * float2(90.0, 50.0) + float2(camX * 0.5, 0));
                float star = step(0.985, RiftHash(cell));
                float twinkle = 0.5 + 0.5 * sin(t * 3.0 + RiftHash(cell + 3.3) * 40.0);
                col += _GridColor.rgb * star * twinkle * 0.6 * step(horizon, uv.y);

                // Horizon glow.
                col += _HorizonGlow.rgb * exp(-abs(uv.y - horizon) * 22.0) * 0.6;

                // Perspective grid floor below the horizon, scrolling with the camera.
                if (uv.y < horizon)
                {
                    float depth = 1.0 / max(horizon - uv.y, 0.002);
                    float gx = (uv.x - 0.5) * depth * 0.6 + camX * 0.15;
                    float gz = depth * 0.25 + t * 0.4;
                    float2 g = abs(frac(float2(gx, gz) * _GridDensity * 0.1) - 0.5);
                    float2 w = fwidth(float2(gx, gz) * _GridDensity * 0.1) * 1.5;
                    float gridLine = max(smoothstep(w.x, 0, g.x), smoothstep(w.y, 0, g.y));
                    float fade = saturate((horizon - uv.y) * 4.0);
                    col += _GridColor.rgb * gridLine * fade;
                }

                // Rare horizontal glitch band.
                float band = step(0.96, RiftHash(float2(floor(t * 6.0), 1.0))) * _GlitchBands;
                float by = RiftHash(float2(floor(t * 6.0), 2.0));
                col += _HorizonGlow.rgb * band * step(abs(uv.y - by), 0.01) * 0.8;
                return col;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float camX = _WorldSpaceCameraPos.x;
                float inside = RiftMask(i.worldPos, 4.0, t * 0.6);
                half3 col = lerp(Ember(i.uv, camX, t), Static(i.uv, camX, t), inside);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
