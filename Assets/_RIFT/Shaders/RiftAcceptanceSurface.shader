// Level02 Static world — ACCEPTANCE. Warm frosted glass: sunlight caustics ripple through it and soft bokeh
// lights drift slowly upward inside. Exists inside the lens only.
// Caustics: the iterative-sine "tileable water caustic" technique by Dave_Hoskins / joltz0r
// (https://www.shadertoy.com/view/MdlXz8). fbm after Inigo Quilez (MIT).
Shader "RIFT/AcceptanceSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _GlassLow ("Glass Low", Color) = (0.55, 0.38, 0.22, 1)
        _GlassHigh ("Glass High", Color) = (1.0, 0.86, 0.62, 1)
        [HDR] _CausticColor ("Caustics", Color) = (1.6, 1.25, 0.7, 1)
        [HDR] _BokehColor ("Bokeh", Color) = (1.8, 1.4, 0.8, 1)
        [HDR] _EdgeColor ("Edge", Color) = (2.2, 1.9, 1.4, 1)
        [HDR] _RimColor ("Dissolve Glow", Color) = (2.0, 1.5, 0.7, 1)
        _CausticScale ("Caustic Scale", Float) = 0.35
        _Opacity ("Glass Opacity", Range(0, 1)) = 0.88
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

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassLow, _GlassHigh, _CausticColor, _BokehColor, _EdgeColor, _RimColor;
                float _CausticScale, _Opacity;
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

            float Caustic(float2 uv, float time)
            {
                const float TAU = 6.28318530718;
                float2 p = fmod(abs(uv * TAU), TAU) - 250.0;
                float2 i = p;
                float c = 1.0;
                float inten = 0.005;
                [unroll] for (int n = 0; n < 5; n++)
                {
                    float tt = time * (1.0 - (3.5 / float(n + 1)));
                    i = p + float2(cos(tt - i.x) + sin(tt + i.y), sin(tt - i.y) + cos(tt + i.x));
                    c += 1.0 / length(float2(p.x / (sin(i.x + tt) / inten), p.y / (cos(i.y + tt) / inten)));
                }
                c /= 5.0;
                c = 1.17 - pow(c, 1.4);
                return pow(abs(c), 8.0);
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float t = _Time.y;
                float2 p = i.worldPos;
                float dTop = (1.0 - i.uv.y) * i.worldSize.y;
                float dBottom = i.uv.y * i.worldSize.y;
                float dSide = min(i.uv.x, 1.0 - i.uv.x) * i.worldSize.x;
                float dEdge = min(min(dTop, dBottom), dSide);

                // Frosted glass body: lighter near the top, a soft cloudy frost.
                float frost = RiftFbm(p * 1.3 + t * 0.03);
                half3 col = lerp(_GlassLow.rgb, _GlassHigh.rgb, saturate(0.35 + frost * 0.5 + (1.0 - saturate(dTop / 1.2)) * 0.3));

                // Sunlight caustics rippling through.
                col += _CausticColor.rgb * Caustic(p * _CausticScale, t * 0.5) * 0.55;

                // Bokeh lights drifting up inside the glass.
                float2 bp = p * 2.2 + float2(0.0, -t * 0.25);
                float2 cell = floor(bp);
                float2 f = frac(bp) - 0.5;
                float rnd = RiftHash(cell);
                float2 jitter = (float2(RiftHash(cell + 1.7), RiftHash(cell + 4.3)) - 0.5) * 0.6;
                float bokeh = smoothstep(0.22, 0.15, length(f - jitter)) * step(0.72, rnd) * (0.5 + 0.5 * sin(t * 1.5 + rnd * 20.0));
                col += _BokehColor.rgb * bokeh * 0.45;

                // Soft white-gold edge.
                col = lerp(col, _EdgeColor.rgb, smoothstep(0.09, 0.0, dEdge) * 0.9);

                // Exists only inside the lens; warm glow where it forms.
                float inside = RiftMask(p, 4.0, t * 0.5);
                col += _RimColor.rgb * inside * (1.0 - inside) * 4.0;
                return half4(col * tex.rgb, tex.a * inside * _Opacity);
            }
            ENDHLSL
        }
    }
}
