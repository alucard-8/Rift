// Title logo. The cold glass letters (the sprite) get a slow light sweep, and a small "rift" circle wanders
// across them: inside it the glass turns warm gold and the outline flares, with a thin chromatic rim —
// the game's two worlds in the logo itself. UI-compatible (Canvas Image).
// Sweep after the classic Shadertoy "shine" pass; noisy rim reuses RiftCommon noise (after Inigo Quilez, MIT).
Shader "RIFT/UI Logo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _WarmColor ("Warm (inside the rift)", Color) = (1.5, 1.0, 0.5, 1)
        [HDR] _RimColor ("Rift Rim", Color) = (1.6, 2.2, 3.0, 1)
        [HDR] _ShineColor ("Shine", Color) = (1.6, 1.8, 2.0, 1)
        _LogoRiftRadius ("Rift Radius (uv)", Range(0.02, 0.5)) = 0.16
        _Speed ("Wander Speed", Float) = 0.12

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "RiftCommon.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _WarmColor, _RimColor, _ShineColor;
                float _LogoRiftRadius, _Speed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color * _Color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float aspect = _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1.0);

                // The wandering rift: a slow Lissajous path across the word.
                float2 c = float2(0.5 + 0.42 * sin(t * _Speed * 6.2831), 0.5 + 0.22 * sin(t * _Speed * 4.1 + 1.3));
                float2 d = (uv - c) * float2(aspect, 1.0);
                float r = length(d) + (RiftNoise(uv * 14.0 + t * 0.6) - 0.5) * 0.015;
                float inside = 1.0 - smoothstep(_LogoRiftRadius - 0.01, _LogoRiftRadius + 0.01, r);
                float rim = smoothstep(0.012, 0.0, abs(r - _LogoRiftRadius));

                // Chromatic split right on the rim.
                float2 split = normalize(d + 1e-5) * 0.004 * rim;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half rC = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + split).r;
                half bC = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - split).b;
                half3 col = half3(rC, tex.g, bC);

                // Glass vs outline: the outline is the dark part of the sprite.
                float lum = dot(col, half3(0.299, 0.587, 0.114));
                float glass = smoothstep(0.35, 0.6, lum);

                // Inside the rift the cold glass turns warm and the outline glows.
                // Recolour the glass by its own brightness so the stripes survive, then a soft warm lift.
                half3 warm = _WarmColor.rgb * (0.35 + 0.55 * lum);
                col = lerp(col, warm, inside * glass * 0.85);
                col += _WarmColor.rgb * inside * (1.0 - glass) * 0.12;

                // Slow diagonal shine across the glass.
                float sweep = frac((uv.x * aspect * 0.35 + uv.y * 0.6) * 0.5 - t * 0.08);
                col += _ShineColor.rgb * smoothstep(0.035, 0.0, abs(sweep - 0.5)) * glass * 0.55;

                // Gentle breathing glow on the whole word.
                col *= 0.94 + 0.06 * sin(t * 1.3);

                // The rim itself, only over the letters (and a faint halo just outside them).
                col += _RimColor.rgb * rim * 0.9;
                float a = tex.a * i.color.a;
                a = max(a, rim * 0.25 * smoothstep(0.0, 0.2, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a + 0.05));
                return half4(col * i.color.rgb, a);
            }
            ENDHLSL
        }
    }
}
