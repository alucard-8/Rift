// Level04 sky. Loneliness (outside the lens): a vast cold void — layered fog banks drifting very slowly, one far
// star, nothing else. Connection (inside the lens): the same sky, laced with a web of light threads linking distant
// points, every strand brightening on the same beat as the Threads platforms.
// fbm after Inigo Quilez (MIT). Web: jittered-grid "plexus" links after the Shadertoy network family
// (e.g. https://www.shadertoy.com/view/lscczl).
Shader "RIFT/BackgroundHollow"
{
    Properties
    {
        [Header(Loneliness void)]
        _VoidTop ("Void Top", Color) = (0.008, 0.012, 0.025, 1)
        _VoidLow ("Void Low", Color) = (0.05, 0.07, 0.11, 1)
        _FogColor ("Fog", Color) = (0.12, 0.15, 0.21, 1)
        [HDR] _StarColor ("Far Star", Color) = (1.6, 1.8, 2.2, 1)
        [Header(Connection web)]
        _WebTop ("Web Sky Top", Color) = (0.03, 0.02, 0.04, 1)
        _WebLow ("Web Sky Low", Color) = (0.14, 0.08, 0.06, 1)
        [HDR] _WebColor ("Threads", Color) = (1.8, 1.2, 0.55, 1)
        [HDR] _KnotColor ("Knots", Color) = (2.6, 1.9, 1.0, 1)
        _BeatSpeed ("Beats per Second (match Threads)", Float) = 0.7
        _Parallax ("Parallax", Float) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
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
                half4 _VoidTop, _VoidLow, _FogColor, _StarColor;
                half4 _WebTop, _WebLow, _WebColor, _KnotColor;
                float _BeatSpeed, _Parallax;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 worldPos : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldPos = world.xy;
                o.uv = i.uv;
                return o;
            }

            half3 Void(float2 uv, float2 cam, float t, float aspect)
            {
                half3 col = lerp(_VoidLow.rgb, _VoidTop.rgb, smoothstep(0.0, 0.85, uv.y));

                // One far star, high and to the right, barely breathing.
                float2 sp = float2((uv.x - 0.82) * aspect, uv.y - 0.78);
                float sd = length(sp);
                col += _StarColor.rgb * (smoothstep(0.006, 0.0, sd) + 0.04 * exp(-sd * 30.0)) * (0.8 + 0.2 * sin(t * 0.7));

                // Fog banks: very slow, very wide.
                [unroll] for (int l = 0; l < 3; l++)
                {
                    float y = 0.18 + l * 0.16;
                    float f = RiftFbm(float2(uv.x * 1.2 + cam.x * _Parallax * (0.5 + l * 0.5) + t * 0.004 * (l + 1), uv.y * 3.0 + l * 5.0));
                    col = lerp(col, _FogColor.rgb, smoothstep(0.45, 0.85, f) * exp(-abs(uv.y - y) * 7.0) * 0.5);
                }
                return col;
            }

            float2 WebAnchor(float2 c) { return c + 0.5 + (float2(RiftHash(c), RiftHash(c + 13.1)) - 0.5) * 0.8; }

            float WebSeg(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            half3 Web(float2 uv, float2 cam, float t, float aspect)
            {
                half3 col = lerp(_WebLow.rgb, _WebTop.rgb, smoothstep(0.0, 0.9, uv.y));
                float2 p = float2(uv.x * aspect, uv.y) * 7.0 + float2(cam.x * _Parallax * 4.0, 0.0);
                float2 cell = floor(p);
                float strands = 0.0, knots = 0.0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 c = cell + float2(x, y);
                    float2 a = WebAnchor(c);
                    knots += smoothstep(0.07, 0.0, length(p - a));
                    float2 r = c + float2(1, 0), u = c + float2(0, 1);
                    if (RiftHash(c + 2.1) > 0.4) strands += smoothstep(0.025, 0.0, WebSeg(p, a, WebAnchor(r)));
                    if (RiftHash(c + 5.3) > 0.5) strands += smoothstep(0.025, 0.0, WebSeg(p, a, WebAnchor(u)));
                }
                float together = 0.5 + 0.5 * sin(t * _BeatSpeed * 6.2831);
                col += _WebColor.rgb * saturate(strands) * (0.3 + 0.5 * together) * smoothstep(0.05, 0.4, uv.y);
                col += _KnotColor.rgb * saturate(knots) * (0.4 + 0.6 * together) * smoothstep(0.05, 0.4, uv.y);
                return col;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 cam = _WorldSpaceCameraPos.xy;
                float inside = RiftMask(i.worldPos, 3.0, t * 0.4);
                half3 col = lerp(Void(i.uv, cam, t, aspect), Web(i.uv, cam, t, aspect), inside);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
