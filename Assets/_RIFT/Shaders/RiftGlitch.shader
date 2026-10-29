// A Static-world creature drawn entirely procedurally: a diamond body with one tracking eye,
// torn by RGB-split jitter, block displacement and scanlines. Before it lunges it winds up (_Windup 0..1):
// it shakes, tears harder, the pupil pins to a point and the eye flares white-hot — the tell you dodge on.
// Block-tear / RGB-shift glitch idea after the classic Shadertoy "VHS / digital glitch" effects
// (e.g. https://www.shadertoy.com/view/XtK3W3). Diamond and circle SDFs after Inigo Quilez (MIT).
Shader "RIFT/Glitch"
{
    Properties
    {
        [HDR] _BodyColor ("Body", Color) = (0.05, 0.02, 0.08, 1)
        [HDR] _EdgeColor ("Edge", Color) = (2.6, 0.3, 1.8, 1)
        [HDR] _EyeColor ("Eye", Color) = (0.4, 2.4, 3.0, 1)
        _Phase ("Phase (0 ghost, 1 real)", Range(0, 1)) = 1
        _Windup ("Windup (0..1)", Range(0, 1)) = 0
        [HDR] _TellColor ("Windup Tell", Color) = (3.2, 2.6, 2.8, 1)
        _LookDir ("Look Direction", Vector) = (1, 0, 0, 0)
        _TearStrength ("Tear Strength", Range(0, 0.3)) = 0.08
        _Seed ("Seed", Float) = 0
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
                half4 _BodyColor, _EdgeColor, _EyeColor, _TellColor;
                float _Phase, _Windup;
                float4 _LookDir;
                float _TearStrength;
                float _Seed;
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

            // Signed distance to a diamond (rhombus) of half-size b.
            float SdRhombus(float2 p, float2 b)
            {
                p = abs(p);
                float h = clamp((-2.0 * dot(p, b) + dot(b, b)) / dot(b, b), -1.0, 1.0);
                float d = length(p - 0.5 * b * float2(1.0 - h, 1.0 + h));
                return d * sign(p.x * b.y + p.y * b.x - b.x * b.y);
            }

            // Shape colour + coverage at a point.
            float4 Shape(float2 p, float t)
            {
                float body = SdRhombus(p, float2(0.36, 0.42));
                float2 look = normalize(_LookDir.xy + 1e-4) * (0.06 + 0.03 * _Windup);
                float eye = length(p - float2(0.0, 0.02)) - 0.13 - 0.03 * _Windup;
                float pupil = length(p - float2(0.0, 0.02) - look) - lerp(0.055, 0.02, _Windup);

                half3 col = _BodyColor.rgb;
                // Inner scan pattern.
                col += _EdgeColor.rgb * 0.22 * step(0.5, frac(p.y * 22.0 - t * 3.0));
                // Glowing outline.
                col = lerp(col, _EdgeColor.rgb, smoothstep(0.05, 0.0, abs(body)));
                // Eye white and pupil.
                col = lerp(col, lerp(_EyeColor.rgb, _TellColor.rgb, _Windup), smoothstep(0.01, 0.0, eye));
                col = lerp(col, _BodyColor.rgb, smoothstep(0.01, 0.0, pupil));
                // Windup: the outline heats toward white and a ring tightens around the eye.
                col = lerp(col, _TellColor.rgb, smoothstep(0.05, 0.0, abs(body)) * _Windup * 0.7);
                float ringR = lerp(0.42, 0.16, _Windup);
                col += _TellColor.rgb * smoothstep(0.015, 0.0, abs(length(p - float2(0.0, 0.02)) - ringR)) * _Windup * step(0.01, _Windup);

                float cover = smoothstep(0.01, -0.01, body - 0.03);
                return float4(col, cover);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y + _Seed * 7.13;
                float2 p = i.uv - 0.5;
                // Windup shake: the whole body jitters harder as it charges.
                p += (float2(RiftHash(float2(floor(t * 40.0), 1.0)), RiftHash(float2(floor(t * 40.0), 2.0))) - 0.5) * 0.05 * _Windup;

                // Block tear: random horizontal slices jump sideways a few times per second (constantly while winding up).
                float slice = floor(p.y * 14.0);
                float tick = floor(t * (9.0 + _Windup * 21.0));
                float tear = (RiftHash(float2(slice, tick)) - 0.5) * step(0.82 - _Windup * 0.5, RiftHash(float2(tick, slice + 3.0)));
                p.x += tear * (_TearStrength + _Windup * 0.12) * 2.0;

                // RGB split along x.
                float split = 0.02 + 0.03 * step(0.9, RiftHash(float2(tick, 9.0)));
                float4 r = Shape(p + float2(split, 0), t);
                float4 g = Shape(p, t);
                float4 b = Shape(p - float2(split, 0), t);
                half3 col = half3(r.r * r.a, g.g * g.a, b.b * b.a);
                float a = max(max(r.a, g.a), b.a);

                // Scanlines.
                col *= 0.8 + 0.2 * sin(i.uv.y * 160.0 + t * 20.0);

                // Out of the lens: only a faint flickering dotted outline remains.
                float body = SdRhombus(i.uv - 0.5, float2(0.36, 0.42));
                float dots = step(0.5, frac(atan2(i.uv.y - 0.5, i.uv.x - 0.5) * 6.0 + t));
                float ghost = smoothstep(0.02, 0.0, abs(body)) * dots * (0.25 + 0.2 * step(0.5, RiftHash(float2(tick, 1.0))));
                half3 ghostCol = _EdgeColor.rgb * ghost * 0.6;

                col = lerp(ghostCol, col, _Phase);
                a = lerp(ghost * 0.5, a, _Phase);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
