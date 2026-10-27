// Bounce pad: a glowing plate with chevrons streaming upward. Dim and still while its world isn't real,
// bright and flowing when active; flashes on each bounce. Colour from the level accent (_RiftAccent).
Shader "RIFT/Pad"
{
    Properties
    {
        _Active ("Active (0..1)", Range(0, 1)) = 1
        _Flash ("Bounce Flash (0..1)", Range(0, 1)) = 0
        _Chevrons ("Chevron Count", Float) = 3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _RiftAccent;
            CBUFFER_START(UnityPerMaterial)
                float _Active, _Flash, _Chevrons;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                half3 accent = _RiftAccent.rgb;
                float t = _Time.y * 2.5 * _Active;

                // Base plate at the bottom quarter of the quad.
                float plate = step(uv.y, 0.22) * smoothstep(0.0, 0.04, uv.x) * smoothstep(1.0, 0.96, uv.x);
                half3 col = lerp(half3(0.05, 0.04, 0.07), accent * 0.6, _Active) * plate;
                col += accent * smoothstep(0.03, 0.0, abs(uv.y - 0.22)) * plate * (0.6 + 1.4 * _Active);

                // Chevrons rising above the plate.
                float y = frac(uv.y * _Chevrons - t);
                float chev = smoothstep(0.08, 0.0, abs(y - 0.5 - abs(uv.x - 0.5) * 0.9)) * step(0.24, uv.y);
                float fade = (1.0 - smoothstep(0.3, 1.0, uv.y)) * _Active;
                col += accent * chev * fade * 1.4;

                col += accent * _Flash * (1.0 - uv.y) * 2.0;
                float alpha = saturate(plate + chev * fade + _Flash * (1.0 - uv.y));
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
