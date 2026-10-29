// Level04 Ember world — LONELINESS, "Hollow". Cold blue-grey stone, frost creeping in from the edges, and now and
// then a faint echo ripple spreading across the face — a sound that came back with nobody behind it.
// Exists outside the lens only. fbm / value noise after Inigo Quilez (iquilezles.org, MIT). Ripple rings after the
// common Shadertoy "raindrop ripples" construction (e.g. https://www.shadertoy.com/view/ldfyzl), one ring per cell, rare.
Shader "RIFT/HollowSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _StoneDark ("Stone Dark", Color) = (0.03, 0.04, 0.06, 1)
        _StoneLight ("Stone Light", Color) = (0.13, 0.16, 0.21, 1)
        _FrostColor ("Frost", Color) = (0.55, 0.68, 0.82, 1)
        [HDR] _EchoColor ("Echo Ripple", Color) = (0.6, 0.85, 1.2, 1)
        [HDR] _EdgeColor ("Top Rim", Color) = (0.6, 0.72, 0.88, 1)
        [HDR] _RimColor ("Dissolve Glow", Color) = (0.7, 0.95, 1.4, 1)
        _EchoCell ("Echo Cell Size", Float) = 6
        _EchoRate ("Echoes per Cell per 10s", Float) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // Set by the admin panel / later gameplay to force an echo everywhere (0..1 ring progress, 0 = none).
            float _HollowEcho;

            CBUFFER_START(UnityPerMaterial)
                half4 _StoneDark, _StoneLight, _FrostColor, _EchoColor, _EdgeColor, _RimColor;
                float _EchoCell, _EchoRate;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 worldPos : TEXCOORD1; float2 worldSize : TEXCOORD2; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldPos = world.xy;
                o.uv = i.uv;
                o.color = i.color;
                o.worldSize = float2(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21));
                return o;
            }

            // One rare ring per cell: returns ring brightness at p.
            float Echo(float2 p, float t)
            {
                float2 cell = floor(p / _EchoCell);
                float2 f = p - (cell + 0.5) * _EchoCell;
                float period = 10.0 / max(_EchoRate, 0.01);
                float phase = frac(t / period + RiftHash(cell + 4.2));
                float life = phase * period;                    // seconds since this cell's echo began
                float2 origin = (float2(RiftHash(cell), RiftHash(cell + 7.7)) - 0.5) * _EchoCell * 0.6;
                float r = length(f - origin);
                float front = life * 1.4;                       // slow, like sound in a big room
                float ring = smoothstep(0.08, 0.0, abs(r - front)) + 0.5 * smoothstep(0.08, 0.0, abs(r - front * 0.6));
                return ring * exp(-life * 0.9) * step(life, 4.0);
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float t = _Time.y;
                float2 p = i.worldPos;
                float dTop = (1.0 - i.uv.y) * i.worldSize.y;
                float dEdge = min(min(i.uv.x * i.worldSize.x, (1.0 - i.uv.x) * i.worldSize.x), min(i.uv.y * i.worldSize.y, dTop));

                // Smooth cold stone, broad slabs, very little detail — empty.
                float slabs = RiftFbm(p * float2(0.35, 0.6));
                float grain = RiftNoise(p * 9.0);
                half3 col = lerp(_StoneDark.rgb, _StoneLight.rgb, saturate(slabs * 0.9 + (grain - 0.5) * 0.12));

                // Frost creeping in from every edge, feathery.
                float frostN = RiftFbm(p * 2.2 + 3.0);
                float frost = smoothstep(0.55, 0.0, dEdge - frostN * 0.45);
                col = lerp(col, _FrostColor.rgb, frost * 0.55);
                col += _FrostColor.rgb * step(0.985, RiftHash(floor(p * 18.0))) * frost * 0.5;

                // Echo ripples: rare, faint, slow.
                float echo = Echo(p, t);
                // Forced echo (admin / later gameplay): one ring from every 8-unit cell centre at once.
                float forcedR = length(p - (floor(p / 8.0) + 0.5) * 8.0);
                echo += smoothstep(0.1, 0.0, abs(forcedR - _HollowEcho * 6.0)) * step(0.001, _HollowEcho) * (1.0 - _HollowEcho) * 2.0;
                col += _EchoColor.rgb * echo * 0.35;

                // Top rim: a thin line of rime.
                col = lerp(col, _EdgeColor.rgb, smoothstep(0.06, 0.0, dTop) * 0.7);

                // Exists outside the lens; cold glow at the dissolving rim.
                float inside = RiftMask(p, 5.0, t * 0.6);
                float visible = 1.0 - inside;
                col += _RimColor.rgb * visible * (1.0 - visible) * 4.0 * 1.2;
                return half4(col * tex.rgb, tex.a * visible);
            }
            ENDHLSL
        }
    }
}
