// Level02 Ember world — REGRET. Rain-soaked stone: drops run down the faces leaving wet trails, splashes
// spark on the top edge, a cold moonlit sheen slides across the wet surface. Exists outside the lens only.
// Value noise / fbm after Inigo Quilez (MIT). Running-drop streaks after the classic Shadertoy
// "rain on glass" idea (e.g. BigWings, "Heartfelt", https://www.shadertoy.com/view/ltffzl), simplified to 2D streaks.
Shader "RIFT/RegretSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _StoneDark ("Stone Dark", Color) = (0.035, 0.045, 0.06, 1)
        _StoneLight ("Stone Light", Color) = (0.16, 0.19, 0.24, 1)
        [HDR] _WetColor ("Wet Streaks", Color) = (0.45, 0.6, 0.85, 1)
        [HDR] _SheenColor ("Moon Sheen", Color) = (0.7, 0.8, 1.0, 1)
        [HDR] _EdgeColor ("Top Rim", Color) = (0.9, 1.0, 1.2, 1)
        [HDR] _RimColor ("Dissolve Glow", Color) = (0.6, 0.85, 1.3, 1)
        _DropDensity ("Drop Columns per Unit", Float) = 3
        _DropSpeed ("Drop Speed", Float) = 0.6
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
                half4 _StoneDark, _StoneLight, _WetColor, _SheenColor, _EdgeColor, _RimColor;
                float _DropDensity, _DropSpeed;
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

                // Stone: large slabs with fine pitting.
                float slabs = RiftFbm(p * float2(0.7, 1.1));
                float pits = RiftNoise(p * 11.0);
                half3 col = lerp(_StoneDark.rgb, _StoneLight.rgb, saturate(slabs * 0.9 + (pits - 0.5) * 0.25));

                // Running drops: each column has a drop sliding down, leaving a fading wet trail above it.
                float colId = floor(p.x * _DropDensity);
                float colX = frac(p.x * _DropDensity) - 0.5;
                float rnd = RiftHash(float2(colId, 3.1));
                float speed = _DropSpeed * (0.6 + rnd);
                float headY = frac(-(p.y * 0.25) - t * speed * 0.25 + rnd * 7.0);   // 0 at the head, grows up the trail
                float lane = smoothstep(0.12, 0.0, abs(colX + (rnd - 0.5) * 0.4)) * step(0.35, rnd);
                float head = smoothstep(0.03, 0.0, headY) * lane;
                float trail = exp(-headY * 6.0) * lane * 0.5;
                col += _WetColor.rgb * (trail * 0.6 + head * 2.0);

                // Darker where it's been soaked longest (just under the top edge).
                col *= lerp(0.75, 1.0, smoothstep(0.0, 0.8, dTop));

                // Cold moonlight sheen sliding slowly across the wet face.
                float sheen = smoothstep(0.75, 1.0, RiftNoise(float2(p.x * 0.5 - t * 0.08, p.y * 0.3)));
                col += _SheenColor.rgb * sheen * 0.18;

                // Top rim: wet edge catching light, with splash glints.
                float rim = smoothstep(0.08, 0.0, dTop);
                float splash = step(0.93, RiftHash(float2(floor(p.x * 8.0), floor(t * 6.0)))) * smoothstep(0.15, 0.0, dTop);
                col = lerp(col, _EdgeColor.rgb, rim * 0.8) + _EdgeColor.rgb * splash * 1.5;

                // Exists outside the lens; cold glow at the dissolving rim.
                float inside = RiftMask(p, 5.0, t * 0.6);
                float visible = 1.0 - inside;
                col += _RimColor.rgb * visible * (1.0 - visible) * 4.0 * 1.4;
                return half4(col * tex.rgb, tex.a * visible);
            }
            ENDHLSL
        }
    }
}
