// Level03 Ember world — FEAR. Burnt ground: charred grey crust split by crack veins that smoulder orange
// underneath, breathing hotter with the global _AshHeat. Exists outside the lens only.
// Crack network from Voronoi edges and fbm after Inigo Quilez (iquilezles.org, MIT). Smoulder idea after
// the "lava / burning paper" family of Shadertoy effects (e.g. https://www.shadertoy.com/view/XsjSDV), rebuilt in 2D.
Shader "RIFT/AshSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _CharDark ("Char Dark", Color) = (0.025, 0.022, 0.022, 1)
        _CharLight ("Ash Light", Color) = (0.2, 0.19, 0.18, 1)
        [HDR] _EmberColor ("Smoulder", Color) = (2.4, 0.7, 0.15, 1)
        [HDR] _HotColor ("White Hot", Color) = (3.0, 2.0, 1.1, 1)
        [HDR] _EdgeColor ("Top Rim", Color) = (0.55, 0.5, 0.48, 1)
        [HDR] _RimColor ("Dissolve Glow", Color) = (2.2, 0.8, 0.25, 1)
        _CrackScale ("Crack Scale", Float) = 0.9
        _BaseHeat ("Base Heat", Range(0, 1)) = 0.25
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

            // Set by gameplay (0 = cold, 1 = burning). Defaults to 0 when nobody sets it.
            float _AshHeat;

            CBUFFER_START(UnityPerMaterial)
                half4 _CharDark, _CharLight, _EmberColor, _HotColor, _EdgeColor, _RimColor;
                float _CrackScale, _BaseHeat;
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

                // Crust: flaky ash plates over charcoal.
                float plates = RiftFbm(p * 0.9);
                float flakes = RiftNoise(p * 14.0);
                half3 col = lerp(_CharDark.rgb, _CharLight.rgb, saturate(plates * 0.8 + (flakes - 0.5) * 0.3));

                // Crack veins, warped so they don't look like a tiled grid.
                float2 warp = float2(RiftFbm(p * 0.6 + 3.1), RiftFbm(p * 0.6 - 7.4)) - 0.5;
                float edge = RiftVoronoiEdge(p * _CrackScale + warp * 1.2);
                float vein = smoothstep(0.09, 0.0, edge);
                float core = smoothstep(0.03, 0.0, edge);

                // Heat breathes slowly, travels along the cracks, and _AshHeat pushes it to white.
                // Most cracks are dead and dark; only patches still smoulder, and _AshHeat wakes the rest.
                float heat = saturate(_BaseHeat + _AshHeat);
                float alive = smoothstep(0.62 - heat * 0.5, 0.78 - heat * 0.4, RiftFbm(p * 0.35 + 11.0 + t * 0.01));
                col *= 1.0 - vein * 0.6;
                float breath = 0.6 + 0.4 * sin(t * 1.3 + plates * 9.0);
                float flow = RiftNoise(p * 2.5 + float2(0.0, -t * 0.4));
                float glow = vein * alive * breath * (0.35 + flow * 0.65) * (0.5 + heat * 1.5);
                col = lerp(col, _EmberColor.rgb, saturate(glow));
                col += _HotColor.rgb * core * alive * heat * breath;
                // Faint warmth bleeding into the crust around live cracks.
                col += _EmberColor.rgb * alive * smoothstep(0.25, 0.0, edge) * 0.06;

                // Crust cools and greys at the top; embers flicker on the very rim.
                float rim = smoothstep(0.07, 0.0, dTop);
                float spark = step(0.96, RiftHash(float2(floor(p.x * 9.0), floor(t * 5.0)))) * smoothstep(0.12, 0.0, dTop);
                col = lerp(col, _EdgeColor.rgb, rim * 0.7) + _EmberColor.rgb * spark * (0.6 + heat);

                // Exists outside the lens; ember glow on the dissolving rim.
                float inside = RiftMask(p, 5.0, t * 0.6);
                float visible = 1.0 - inside;
                col += _RimColor.rgb * visible * (1.0 - visible) * 4.0 * 1.4;
                return half4(col * tex.rgb, tex.a * visible);
            }
            ENDHLSL
        }
    }
}
