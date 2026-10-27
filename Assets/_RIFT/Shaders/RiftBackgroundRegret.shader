// Level02 sky. Regret (outside the lens): a foggy rain night — moon halo, drifting mist banks, a far
// shoreline of dim windows, rain. Acceptance (inside the lens): sunrise — lavender to gold sky, sun on the
// horizon pouring god rays, lit clouds and dust motes rising.
// fbm after Inigo Quilez (MIT). God rays as noise sampled along angle from the sun, after common Shadertoy
// "crepuscular rays" sketches.
Shader "RIFT/BackgroundRegret"
{
    Properties
    {
        [Header(Regret night)]
        _NightTop ("Night Top", Color) = (0.02, 0.025, 0.05, 1)
        _NightLow ("Night Horizon", Color) = (0.14, 0.17, 0.24, 1)
        _MoonColor ("Moon", Color) = (0.85, 0.9, 1.05, 1)
        _MistColor ("Mist", Color) = (0.35, 0.4, 0.5, 1)
        [HDR] _WindowColor ("Windows", Color) = (1.4, 0.95, 0.5, 1)
        _RainColor ("Rain", Color) = (0.55, 0.65, 0.8, 1)
        [Header(Acceptance sunrise)]
        _DawnTop ("Dawn Top", Color) = (0.45, 0.42, 0.7, 1)
        _DawnMid ("Dawn Mid", Color) = (1.0, 0.62, 0.48, 1)
        _DawnLow ("Dawn Horizon", Color) = (1.2, 0.85, 0.4, 1)
        [HDR] _SunColor ("Sun", Color) = (2.6, 2.0, 1.2, 1)
        _Parallax ("Parallax", Float) = 0.03
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
                half4 _NightTop, _NightLow, _MoonColor, _MistColor, _WindowColor, _RainColor;
                half4 _DawnTop, _DawnMid, _DawnLow, _SunColor;
                float _Parallax;
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

            half3 Night(float2 uv, float2 cam, float t, float aspect)
            {
                half3 col = lerp(_NightLow.rgb, _NightTop.rgb, smoothstep(0.2, 1.0, uv.y));

                // Moon with a wide soft halo.
                float2 mp = float2((uv.x - 0.78) * aspect, uv.y - 0.74);
                float md = length(mp);
                col += _MoonColor.rgb * (smoothstep(0.035, 0.03, md) + 0.25 * exp(-md * 7.0) + 0.1 * exp(-md * 2.0));

                // Far shoreline with a scatter of dim windows.
                float shore = 0.3 + (RiftFbm(float2(uv.x * 4.0 + cam.x * _Parallax, 1.0)) - 0.5) * 0.06;
                float below = smoothstep(shore + 0.002, shore - 0.002, uv.y);
                col = lerp(col, _NightTop.rgb * 0.6, below);
                float2 wcell = floor(float2((uv.x + cam.x * _Parallax * 0.25) * 140.0, uv.y * 90.0));
                float win = step(0.97, RiftHash(wcell)) * below * smoothstep(shore - 0.08, shore - 0.01, uv.y);
                col += _WindowColor.rgb * win * (0.5 + 0.5 * step(0.3, RiftHash(wcell + floor(t * 0.3))));

                // Mist banks drifting across.
                [unroll] for (int l = 0; l < 3; l++)
                {
                    float y = 0.25 + l * 0.12;
                    float m = RiftFbm(float2(uv.x * 2.0 + cam.x * _Parallax * (1.0 + l) + t * 0.02 * (l + 1), uv.y * 6.0 + l * 4.0));
                    col = lerp(col, _MistColor.rgb, smoothstep(0.45, 0.8, m) * exp(-abs(uv.y - y) * 9.0) * 0.45);
                }

                // Rain.
                [unroll] for (int r = 0; r < 3; r++)
                {
                    float depth = 1.0 + r * 0.6;
                    float2 rp = float2(uv.x * aspect * 60.0 * depth + uv.y * 6.0 + cam.x * _Parallax * 20.0 * depth, uv.y * 3.0 * depth + t * (2.2 + r));
                    float id = floor(rp.x);
                    float drop = frac(rp.y + RiftHash(float2(id, r)) * 10.0);
                    float streak = smoothstep(0.0, 0.08, drop) * smoothstep(0.35, 0.1, drop);
                    float lane = smoothstep(0.5, 0.0, abs(frac(rp.x) - 0.5)) * step(0.55, RiftHash(float2(id, r + 7.0)));
                    col += _RainColor.rgb * streak * lane * (0.3 / depth);
                }
                return col;
            }

            half3 Dawn(float2 uv, float2 cam, float t, float aspect)
            {
                half3 col = lerp(_DawnLow.rgb, _DawnMid.rgb, smoothstep(0.25, 0.5, uv.y));
                col = lerp(col, _DawnTop.rgb, smoothstep(0.5, 1.0, uv.y));

                // Sun on the horizon, with god rays fanning upward.
                float2 sunPos = float2(0.5, 0.3);
                float2 sp = float2((uv.x - sunPos.x) * aspect, uv.y - sunPos.y);
                float sd = length(sp);
                float ang = atan2(sp.y, sp.x);
                float rays = RiftFbm(float2(ang * 7.0, t * 0.05)) * smoothstep(-0.1, 0.4, sp.y);
                col += _SunColor.rgb * (smoothstep(0.07, 0.06, sd) + 0.5 * exp(-sd * 5.0) + rays * 0.35 * exp(-sd * 1.2));

                // Soft lit clouds.
                float cl = RiftFbm(float2(uv.x * 2.5 + cam.x * _Parallax + t * 0.01, uv.y * 7.0));
                col = lerp(col, _DawnLow.rgb * 1.2, smoothstep(0.55, 0.85, cl) * smoothstep(0.4, 0.9, uv.y) * 0.5);

                // Dust motes rising in the light.
                float2 dp = uv * float2(aspect * 40.0, 40.0) + float2(cam.x * 0.4, -t * 0.6);
                float2 dc = floor(dp);
                float mote = step(0.96, RiftHash(dc)) * smoothstep(0.3, 0.0, length(frac(dp) - 0.5));
                col += _SunColor.rgb * mote * 0.35;
                return col;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 cam = _WorldSpaceCameraPos.xy;
                float inside = RiftMask(i.worldPos, 3.0, t * 0.4);
                half3 col = lerp(Night(i.uv, cam, t, aspect), Dawn(i.uv, cam, t, aspect), inside);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
