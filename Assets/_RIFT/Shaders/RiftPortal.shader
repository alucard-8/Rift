// The goal: a swirling tear in reality. Polar-coordinate fbm vortex.
// Swirl technique after Inigo Quilez's domain-warping article (iquilezles.org/articles/warp, MIT).
Shader "RIFT/Portal"
{
    Properties
    {
        [HDR] _Inner ("Inner", Color) = (0.3, 2.2, 2.6, 1)
        [HDR] _Outer ("Outer", Color) = (2.2, 0.25, 1.4, 1)
        _Spin ("Spin Speed", Float) = 1.2
        _Arms ("Arms", Float) = 3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
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
                half4 _Inner, _Outer;
                float _Spin, _Arms;
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
                float t = _Time.y * _Spin;
                // Tall ellipse: stretch the quad's uv into a circle.
                float2 p = (i.uv - 0.5) * float2(2.0, 2.0);
                float r = length(p);
                float a = atan2(p.y, p.x);

                // Twist angle with radius, then warp with fbm.
                float twist = a * _Arms / 6.2831 + r * 2.5 - t;
                float n = RiftFbm(float2(twist * 2.0, r * 4.0 - t * 0.7));
                float arms = smoothstep(0.35, 0.8, n);

                float body = smoothstep(1.0, 0.55, r);
                float core = exp(-r * 6.0);
                half3 col = lerp(_Inner.rgb, _Outer.rgb, saturate(r * 1.2)) * arms * body;
                col += _Inner.rgb * core * 1.5;
                col += _Outer.rgb * smoothstep(0.08, 0.0, abs(r - 0.92 + (n - 0.5) * 0.1)) * 0.8;

                return half4(col * i.color.a, 0);
            }
            ENDHLSL
        }
    }
}
