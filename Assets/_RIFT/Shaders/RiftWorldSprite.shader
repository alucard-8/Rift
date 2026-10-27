// Sprite that belongs to one reality. Ember is visible outside the lens, Static only inside.
// Texturing is done in world space so stretched platforms keep a consistent texel density.
Shader "RIFT/WorldSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [Enum(Ember,0,Static,1)] _World ("World", Float) = 0
        _ShadowColor ("Shadow Color", Color) = (0.23, 0.08, 0.20, 1)
        _LightColor ("Light Color", Color) = (1.0, 0.55, 0.25, 1)
        [HDR] _EdgeColor ("Edge Color (Ember top rim / Static outline)", Color) = (1.5, 1.2, 0.85, 1)
        [HDR] _GridColor ("Grid Color (Static)", Color) = (0.2, 1.2, 1.6, 1)
        [HDR] _CrackColor ("Crack Color (Ember)", Color) = (3.0, 1.1, 0.3, 1)
        [HDR] _RimColor ("Boundary Glow", Color) = (1.0, 0.85, 0.6, 1)
        _EdgeWidth ("Edge Width (world)", Float) = 0.1
        _TextureScale ("Texture Scale", Float) = 1.2
        _NoiseScale ("Dissolve Noise Scale", Float) = 6
        _ScanlineDensity ("Scanlines (Static)", Float) = 90
        _Flicker ("Flicker (Static)", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
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
                float  _World;
                half4  _ShadowColor;
                half4  _LightColor;
                half4  _EdgeColor;
                half4  _GridColor;
                half4  _CrackColor;
                half4  _RimColor;
                float  _EdgeWidth;
                float  _TextureScale;
                float  _NoiseScale;
                float  _ScanlineDensity;
                float  _Flicker;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float2 worldPos : TEXCOORD1;
                float2 worldSize : TEXCOORD2;
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldPos = world.xy;
                o.uv = i.uv;
                o.color = i.color;
                // Object scale from the model matrix columns (the square sprite is 1x1 units).
                o.worldSize = float2(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21));
                return o;
            }

            // Volcanic basalt: dark faceted rock split by glowing, slowly breathing magma cracks.
            half3 EmberSurface(Varyings i, float dTop, float dBottom, float t)
            {
                float2 p = i.worldPos * _TextureScale;

                // Rock facets: low-frequency fbm plus fine grit.
                float facets = RiftFbm(p * 0.9);
                float grit = RiftNoise(p * 14.0);
                half3 col = lerp(_ShadowColor.rgb, _LightColor.rgb, saturate(facets * 0.9 + (grit - 0.5) * 0.15));

                // Crack network, warped so the lines wander like real fractures.
                float2 warp = float2(RiftFbm(p * 0.7 + 3.1), RiftFbm(p * 0.7 + 8.4)) * 0.9;
                float edge = RiftVoronoiEdge(p * 0.7 + warp);
                float crack = smoothstep(0.06, 0.0, edge);
                float halo = smoothstep(0.25, 0.0, edge) * 0.2;

                // Magma breathes: brightness travels slowly along the cracks.
                float pulse = 0.65 + 0.35 * sin(t * 1.3 + RiftFbm(p * 0.5) * 9.0);
                // Cracks glow hotter near the surface and cool off deeper down.
                float heat = lerp(1.0, 0.35, saturate(dTop / 2.5));
                col += _CrackColor.rgb * (crack + halo) * pulse * heat;

                // Hot rim of light on the walkable top edge, dark underside.
                float rimNoise = RiftNoise(float2(i.worldPos.x * 6.0, 0.0)) * 0.04;
                col = lerp(col, _EdgeColor.rgb, smoothstep(_EdgeWidth + rimNoise, 0.0, dTop));
                col *= lerp(0.45, 1.0, smoothstep(0.0, 0.35, dBottom));
                return col;
            }

            half3 StaticSurface(Varyings i, float dEdge, float t)
            {
                half3 col = _ShadowColor.rgb;

                // World-space grid, anti-aliased with fwidth.
                float2 gp = i.worldPos * 2.0;
                float2 g = abs(frac(gp) - 0.5);
                float2 w = fwidth(gp) * 1.2;
                float grid = max(smoothstep(w.x, 0.0, g.x - 0.0), smoothstep(w.y, 0.0, g.y - 0.0));
                col += _GridColor.rgb * grid * 0.6;

                // Faint inner fill pulse.
                col += _LightColor.rgb * 0.08 * (0.5 + 0.5 * sin(t * 2.0 + i.worldPos.x * 0.7));

                // Hot outline that bloom picks up.
                col = lerp(col, _EdgeColor.rgb, smoothstep(_EdgeWidth, _EdgeWidth * 0.3, dEdge));

                half scan = 0.85 + 0.15 * sin(i.worldPos.y * _ScanlineDensity + t * 8.0);
                half flick = 1.0 - _Flicker * step(0.93, RiftHash(float2(floor(t * 18.0), 7.0)));
                return col * scan * flick;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float t = _Time.y;
                float inside = RiftMask(i.worldPos, _NoiseScale, t * 0.6);
                float visible = _World > 0.5 ? inside : 1.0 - inside;

                // Distances to each edge in world units.
                float dTop = (1.0 - i.uv.y) * i.worldSize.y;
                float dBottom = i.uv.y * i.worldSize.y;
                float dSide = min(i.uv.x, 1.0 - i.uv.x) * i.worldSize.x;
                float dEdge = min(min(dTop, dBottom), dSide);

                half3 col = _World > 0.5 ? StaticSurface(i, dEdge, t) : EmberSurface(i, dTop, dBottom, t);
                col *= tex.rgb;

                // Hot glow where the world is dissolving at the rim.
                half band = visible * (1.0 - visible) * 4.0;
                col += _RimColor.rgb * band * 1.5;

                return half4(col, tex.a * visible);
            }
            ENDHLSL
        }
    }
}
