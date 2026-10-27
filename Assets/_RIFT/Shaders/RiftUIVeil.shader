// Full-screen UI veil behind menus and the win screen: darkens from the edges inward, with a slow
// accent-tinted mist and a soft light band behind the title. Colour comes from the level accent (_RiftAccent).
// fbm after Inigo Quilez (MIT).
Shader "RIFT/UIVeil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Base ("Base Darkness", Color) = (0.02, 0.012, 0.03, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.88
        _MistAmount ("Mist", Range(0, 1)) = 0.5
        _BandY ("Light Band Height", Range(0, 1)) = 0.6
        _BandAmount ("Light Band", Range(0, 1)) = 0.5

        // Standard UI masking support.
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
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

            half4 _RiftAccent;

            CBUFFER_START(UnityPerMaterial)
                half4 _Base;
                float _Opacity, _MistAmount, _BandY, _BandAmount;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float vignette = smoothstep(0.15, 0.95, length((uv - 0.5) * float2(1.5, 1.0)));
                float mist = RiftFbm(uv * float2(3.0, 2.0) + float2(t * 0.02, t * 0.01));
                float band = exp(-abs(uv.y - _BandY) * 7.0) * (1.0 - abs(uv.x - 0.5) * 1.2);

                half3 accent = _RiftAccent.rgb;
                half3 col = _Base.rgb + accent * (smoothstep(0.4, 0.9, mist) * 0.06 * _MistAmount + saturate(band) * 0.1 * _BandAmount);
                float alpha = _Opacity * lerp(0.82, 1.0, vignette) * i.color.a;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
