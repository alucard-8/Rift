// A moving laser: hot white core, danger-red glow and energy pulses travelling along the beam.
// UV.x runs end to end, UV.y across the beam.
Shader "RIFT/Laser"
{
    Properties
    {
        _Along ("Energy Offset", Float) = 0
        _On ("On (0..1)", Range(0, 1)) = 1
        _Charge ("Charge / Warning", Range(0, 1)) = 0
        _CoreColor ("Core", Color) = (3.2, 2.4, 2.0, 1)
        _BeamColor ("Glow", Color) = (2.4, 0.55, 0.28, 1)
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
            #include "RiftCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Along, _On, _Charge;
                half4 _CoreColor, _BeamColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float x = i.uv.x;
                float y = abs(i.uv.y - 0.5) * 2.0;   // 0 centre, 1 edge

                // The beam is only hot along its middle; the rest is soft glow.
                float core = exp(-y * y * 20.0);
                float glow = exp(-y * 3.2);

                // Energy travelling outward from the middle.
                float travel = frac(x * 3.0 - t * 1.7 + _Along);
                float pulse = 0.65 + 0.35 * smoothstep(0.0, 0.25, travel) * smoothstep(0.55, 0.25, travel);
                float flicker = 0.9 + 0.1 * sin(t * 40.0 + x * 30.0);

                // Ends dip so the beam reads as anchored to its emitters.
                float ends = smoothstep(0.0, 0.04, x) * smoothstep(1.0, 0.96, x);

                half3 col = _CoreColor.rgb * core * 1.5 + _BeamColor.rgb * glow * 1.1;
                col *= pulse * flicker;

                float alpha = saturate(core + glow * 0.8) * ends * _On;
                alpha += _Charge * core * ends * 0.35;
                return half4(col * _On * ends, alpha);
            }
            ENDHLSL
        }
    }
}
