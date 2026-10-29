// Level04 Static world — CONNECTION, "Threads". A faint pane of light crossed by glowing strings strung between
// anchor points. One shared pulse runs along every string in the level at once (driven by _Time only), so all the
// Threads platforms beat together. Exists inside the lens only.
// Segment SDF after Inigo Quilez's 2D distance functions (iquilezles.org, MIT); string web after the "plexus / network"
// family of Shadertoy effects (e.g. https://www.shadertoy.com/view/lscczl), reduced to a grid of jittered anchors.
Shader "RIFT/ThreadsSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _PaneColor ("Pane", Color) = (0.08, 0.06, 0.04, 1)
        [HDR] _ThreadColor ("Threads", Color) = (2.2, 1.5, 0.7, 1)
        [HDR] _PulseColor ("Pulse", Color) = (3.0, 2.4, 1.6, 1)
        [HDR] _KnotColor ("Knots", Color) = (3.0, 2.2, 1.2, 1)
        [HDR] _EdgeColor ("Edge", Color) = (2.4, 1.8, 1.0, 1)
        [HDR] _RimColor ("Dissolve Glow", Color) = (2.2, 1.5, 0.7, 1)
        _AnchorCell ("Anchor Spacing", Float) = 0.9
        _BeatSpeed ("Pulse Beats per Second", Float) = 0.7
        _Opacity ("Pane Opacity", Range(0, 1)) = 0.35
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

            // Extra pulse strength pushed by the admin panel (0..1).
            float _ThreadBurst;

            CBUFFER_START(UnityPerMaterial)
                half4 _PaneColor, _ThreadColor, _PulseColor, _KnotColor, _EdgeColor, _RimColor;
                float _AnchorCell, _BeatSpeed, _Opacity;
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

            // Jittered anchor point inside a grid cell (world units).
            float2 Anchor(float2 cell)
            {
                return (cell + 0.5 + (float2(RiftHash(cell), RiftHash(cell + 13.1)) - 0.5) * 0.7) * _AnchorCell;
            }

            // Distance to segment a-b, and how far along it (0..1) the closest point is.
            float2 Segment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return float2(length(pa - ba * h), h);
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float t = _Time.y;
                float2 p = i.worldPos;
                float dTop = (1.0 - i.uv.y) * i.worldSize.y;
                float dEdge = min(min(i.uv.x * i.worldSize.x, (1.0 - i.uv.x) * i.worldSize.x), min(i.uv.y * i.worldSize.y, dTop));

                // Strings: each anchor links to its right and upper neighbours (only some links exist).
                float2 cell = floor(p / _AnchorCell);
                float threads = 0.0, pulse = 0.0, knots = 0.0;
                float beat = t * _BeatSpeed;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 c = cell + float2(x, y);
                    float2 a = Anchor(c);
                    knots += smoothstep(0.06, 0.0, length(p - a));
                    [unroll] for (int k = 0; k < 2; k++)
                    {
                        float2 nc = c + (k == 0 ? float2(1, 0) : float2(1, 1));
                        if (RiftHash(c + nc * 3.7) < 0.35) continue;
                        float2 s = Segment(p, a, Anchor(nc));
                        float strand = smoothstep(0.022, 0.0, s.x);
                        threads += strand;
                        // One shared beat for the whole level: the pulse position depends only on time and world x.
                        float along = frac(beat - (a.x + (Anchor(nc).x - a.x) * s.y) * 0.08);
                        pulse += strand * smoothstep(0.12, 0.0, abs(along - 0.5)) ;
                    }
                }
                threads = saturate(threads);
                float together = 0.55 + 0.45 * sin(beat * 6.2831);   // every string brightens at the same moment
                half3 col = _PaneColor.rgb;
                col += _ThreadColor.rgb * threads * (0.35 + 0.35 * together + _ThreadBurst);
                col += _PulseColor.rgb * saturate(pulse) * (0.8 + _ThreadBurst * 1.5);
                col += _KnotColor.rgb * saturate(knots) * (0.5 + 0.5 * together);

                // Soft edge line so you can still read the platform's shape.
                float edge = smoothstep(0.05, 0.0, dEdge);
                col = lerp(col, _EdgeColor.rgb, edge * 0.7);

                // Exists inside the lens; warm glow at the dissolving rim.
                float inside = RiftMask(p, 6.0, t * 0.8);
                col += _RimColor.rgb * inside * (1.0 - inside) * 4.0 * 1.4;
                float alpha = saturate(_Opacity + threads * 0.6 + saturate(knots) * 0.6 + edge);
                return half4(col * tex.rgb, tex.a * alpha * inside);
            }
            ENDHLSL
        }
    }
}
