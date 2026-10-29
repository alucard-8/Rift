// Level03 sky. Fear (outside the lens): a burnt-out night — heavy smoke rolling over a far fire line that
// glows low on the horizon, with ash flakes falling. Courage (inside the lens): a cold, clear dark with
// crisp stars and a pale aurora — the same dark, but you can see what's in it.
// fbm after Inigo Quilez (MIT). Domain-warped smoke after IQ's "warp" article (iquilezles.org/articles/warp).
Shader "RIFT/BackgroundAsh"
{
    Properties
    {
        [Header(Fear smoke)]
        _SmokeTop ("Smoke Top", Color) = (0.012, 0.01, 0.01, 1)
        _SmokeLow ("Smoke Low", Color) = (0.09, 0.06, 0.05, 1)
        [HDR] _FireColor ("Far Fire", Color) = (1.6, 0.45, 0.1, 1)
        _AshColor ("Falling Ash", Color) = (0.5, 0.47, 0.45, 1)
        [Header(Courage clear)]
        _ClearTop ("Clear Top", Color) = (0.01, 0.02, 0.05, 1)
        _ClearLow ("Clear Low", Color) = (0.05, 0.1, 0.18, 1)
        [HDR] _StarColor ("Stars", Color) = (2.0, 2.3, 2.8, 1)
        [HDR] _AuroraColor ("Aurora", Color) = (0.4, 1.3, 1.6, 1)
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
                half4 _SmokeTop, _SmokeLow, _FireColor, _AshColor;
                half4 _ClearTop, _ClearLow, _StarColor, _AuroraColor;
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

            half3 Fear(float2 uv, float2 cam, float t, float aspect)
            {
                half3 col = lerp(_SmokeLow.rgb, _SmokeTop.rgb, smoothstep(0.1, 0.9, uv.y));

                // Far fire line glowing under the smoke.
                float fireY = 0.18 + (RiftFbm(float2(uv.x * 3.0 + cam.x * _Parallax * 0.5, t * 0.05)) - 0.5) * 0.08;
                float flicker = 0.75 + 0.25 * RiftNoise(float2(uv.x * 8.0, t * 2.0));
                col += _FireColor.rgb * exp(-max(uv.y - fireY, 0.0) * 9.0) * 0.28 * flicker;

                // Rolling, domain-warped smoke, lit from below by the fire.
                float2 sp = float2(uv.x * aspect * 1.5 + cam.x * _Parallax, uv.y * 2.0) + float2(t * 0.015, 0.0);
                float2 q = float2(RiftFbm(sp), RiftFbm(sp + 5.2));
                float smoke = RiftFbm(sp + q * 2.0 + float2(0.0, -t * 0.02));
                half3 smokeCol = lerp(_SmokeTop.rgb, _SmokeLow.rgb * 1.8 + _FireColor.rgb * 0.08 * (1.0 - uv.y), q.x);
                col = lerp(col, smokeCol, smoothstep(0.35, 0.8, smoke) * 0.85);

                // Ash flakes falling slowly, swaying.
                [unroll] for (int l = 0; l < 2; l++)
                {
                    float depth = 1.0 + l * 0.8;
                    float2 ap = uv * float2(aspect, 1.0) * 24.0 * depth;
                    ap += float2(cam.x * _Parallax * 6.0 * depth + sin(t * 0.5 + uv.y * 4.0) * 0.4, t * 0.6 / depth);
                    float2 ac = floor(ap);
                    float flake = step(0.94, RiftHash(ac + l * 13.0)) * smoothstep(0.25, 0.0, length(frac(ap) - 0.5));
                    col += _AshColor.rgb * flake * (0.35 / depth);
                }
                return col;
            }

            half3 Courage(float2 uv, float2 cam, float t, float aspect)
            {
                half3 col = lerp(_ClearLow.rgb, _ClearTop.rgb, smoothstep(0.0, 0.9, uv.y));

                // Crisp twinkling stars.
                float2 stp = (uv * float2(aspect, 1.0) + cam * _Parallax * 0.1) * 90.0;
                float2 sc = floor(stp);
                float star = step(0.985, RiftHash(sc)) * smoothstep(0.18, 0.0, length(frac(stp) - 0.5));
                col += _StarColor.rgb * star * (0.6 + 0.4 * sin(t * 3.0 + RiftHash(sc + 3.0) * 30.0));

                // Pale aurora curtain.
                float ax = uv.x * 2.0 + cam.x * _Parallax * 0.3;
                float curtain = RiftFbm(float2(ax * 2.0, t * 0.08));
                float band = exp(-abs(uv.y - 0.65 - (curtain - 0.5) * 0.25) * 10.0);
                col += _AuroraColor.rgb * band * smoothstep(0.3, 0.7, RiftNoise(float2(ax * 6.0, t * 0.2))) * 0.4;
                return col;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 cam = _WorldSpaceCameraPos.xy;
                float inside = RiftMask(i.worldPos, 3.0, t * 0.4);
                half3 col = lerp(Fear(i.uv, cam, t, aspect), Courage(i.uv, cam, t, aspect), inside);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
