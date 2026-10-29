// Level03 Static world — COURAGE. Clear crystal: faceted cells bend what's behind them, a hard white-blue rim
// and a shatter-shimmer sweep that flashes across the facets. Exists inside the lens only.
// Facets from Voronoi edges after Inigo Quilez (iquilezles.org, MIT). Fake refraction + sweep after the
// "crystal / ice" Godot shader family (https://godotshaders.com), done as a facet-normal UV offset instead of a screen grab.
Shader "RIFT/GlassSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _GlassDeep ("Glass Deep", Color) = (0.05, 0.09, 0.16, 1)
        _GlassClear ("Glass Clear", Color) = (0.55, 0.75, 0.95, 1)
        [HDR] _FacetColor ("Facet Lines", Color) = (1.4, 2.0, 2.6, 1)
        [HDR] _ShimmerColor ("Shimmer", Color) = (2.5, 2.8, 3.2, 1)
        [HDR] _EdgeColor ("Edge", Color) = (2.6, 3.0, 3.4, 1)
        [HDR] _RimColor ("Dissolve Glow", Color) = (1.4, 2.2, 3.0, 1)
        _FacetScale ("Facet Scale", Float) = 1.3
        _Opacity ("Glass Opacity", Range(0, 1)) = 0.8
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
                half4 _GlassDeep, _GlassClear, _FacetColor, _ShimmerColor, _EdgeColor, _RimColor;
                float _FacetScale, _Opacity;
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

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float t = _Time.y;
                float2 p = i.worldPos;
                float dTop = (1.0 - i.uv.y) * i.worldSize.y;
                float dEdge = min(min(i.uv.x * i.worldSize.x, (1.0 - i.uv.x) * i.worldSize.x),
                                  min(i.uv.y * i.worldSize.y, dTop));

                // Facets: each Voronoi cell tilts the "view" by its own offset — a cheap refraction.
                float2 fp = p * _FacetScale;
                float2 cell = floor(fp + RiftNoise(fp * 0.5) * 0.8);
                float2 tilt = float2(RiftHash(cell), RiftHash(cell + 9.7)) - 0.5;
                float depth = RiftFbm((p + tilt * 1.5) * 0.8 + t * 0.03);
                half3 col = lerp(_GlassDeep.rgb, _GlassClear.rgb, depth * 0.7);

                float facet = smoothstep(0.05, 0.0, RiftVoronoiEdge(fp));
                col += _FacetColor.rgb * facet * 0.45;

                // Shatter-shimmer: a diagonal band sweeps across; facets catch it at their own angle.
                float sweep = frac((p.x + p.y * 0.6) * 0.06 - t * 0.12);
                float band = smoothstep(0.06, 0.0, abs(sweep - 0.5 - tilt.x * 0.08));
                col += _ShimmerColor.rgb * band * (0.3 + facet * 1.5);

                // Glint stars that pop on random facet corners.
                float glint = step(0.985, RiftHash(cell + floor(t * 2.0))) * facet;
                col += _ShimmerColor.rgb * glint * 2.0;

                // Hard cold rim on every edge (thicker on top, where it catches the light).
                float rim = max(smoothstep(0.06, 0.0, dEdge), smoothstep(0.1, 0.0, dTop));
                col = lerp(col, _EdgeColor.rgb, rim * 0.85);

                // Exists inside the lens; icy glow at the dissolving rim.
                float inside = RiftMask(p, 6.0, t * 0.8);
                col += _RimColor.rgb * inside * (1.0 - inside) * 4.0 * 1.6;
                float alpha = lerp(_Opacity, 1.0, saturate(rim + facet * 0.5));
                return half4(col * tex.rgb, tex.a * alpha * inside);
            }
            ENDHLSL
        }
    }
}
