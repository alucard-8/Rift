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
        [Header(Static)]
        _VoidColor ("Void", Color) = (0.01, 0.02, 0.05, 1)
        _GridColor ("Grid", Color) = (0.15, 1.4, 1.8, 1)
        _HorizonGlow ("Horizon Glow", Color) = (1.6, 0.2, 1.0, 1)
        _GridDensity ("Grid Density", Float) = 10
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
                half4 _VoidColor, _GridColor, _HorizonGlow;
                float _GridDensity;
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

                // Soft sun sitting on the horizon.
                float2 sp = float2((uv.x - 0.12) * aspect, uv.y - 0.6);
                float sd = length(sp);
                col += _SunColor.rgb * (smoothstep(0.075, 0.068, sd) + 0.35 * exp(-sd * 9.0));

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
                return col;
            }

            half3 Static(float2 uv, float camX, float t)
            {
                half3 col = _VoidColor.rgb;
                float horizon = 0.42;

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
                float band = step(0.96, RiftHash(float2(floor(t * 6.0), 1.0)));
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
