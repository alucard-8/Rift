// Sprite that belongs to one reality. Ember is visible outside the lens, Static only inside.
Shader "RIFT/WorldSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [Enum(Ember,0,Static,1)] _World ("World", Float) = 0
        _ShadowColor ("Shadow Color", Color) = (0.23, 0.08, 0.20, 1)
        _LightColor ("Light Color", Color) = (1.0, 0.55, 0.25, 1)
        _RimColor ("Boundary Glow", Color) = (1.0, 0.85, 0.6, 1)
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
                half4  _RimColor;
                float  _NoiseScale;
                float  _ScanlineDensity;
                float  _Flicker;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 worldPos : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldPos = world.xy;
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float t = _Time.y;
                float inside = RiftMask(i.worldPos, _NoiseScale, t * 0.6);
                float visible = _World > 0.5 ? inside : 1.0 - inside;

                // Two-tone grade: luminance picks between the world's shadow and light colours.
                half lum = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half3 col = lerp(_ShadowColor.rgb, _LightColor.rgb, saturate(lum * 1.1 + (i.uv.y - 0.5) * 0.25));

                if (_World > 0.5)
                {
                    half scan = 0.85 + 0.15 * sin(i.worldPos.y * _ScanlineDensity + t * 8.0);
                    half flick = 1.0 - _Flicker * step(0.93, RiftHash(float2(floor(t * 18.0), 7.0)));
                    col *= scan * flick;
                }

                // Hot glow where the world is dissolving at the rim.
                half band = visible * (1.0 - visible) * 4.0;
                col += _RimColor.rgb * band * 1.5;

                return half4(col, tex.a * visible);
            }
            ENDHLSL
        }
    }
}
