// Checkpoint "memory shard": a floating crystal that sleeps dim and cracked, then wakes with an inner
// flame, a soft light beam and expanding rings. Colour comes from the level's accent (_RiftAccent).
// Polar swirl / fbm after Inigo Quilez (iquilezles.org, MIT); rhombus SDF after IQ's 2D distance functions.
Shader "RIFT/Checkpoint"
{
    Properties
    {
        _Lit ("Lit (0 asleep, 1 awake)", Range(0, 1)) = 0
        _Pulse ("Activation Pulse (0..1)", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
        [HDR] _SleepTint ("Asleep Tint", Color) = (0.35, 0.3, 0.45, 1)
        _BobAmount ("Bob Amount (uv)", Float) = 0.02
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

            half4 _RiftAccent;

            CBUFFER_START(UnityPerMaterial)
                float _Lit, _Pulse, _Seed, _BobAmount;
                half4 _SleepTint;
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

            float SdRhombus(float2 p, float2 b)
            {
                p = abs(p);
                float h = clamp((-2.0 * dot(p, b) + dot(b, b)) / dot(b, b), -1.0, 1.0);
                float d = length(p - 0.5 * b * float2(1.0 - h, 1.0 + h));
                return d * sign(p.x * b.y + p.y * b.x - b.x * b.y);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y + _Seed * 3.7;
                half3 accent = _RiftAccent.rgb;
                // Quad is 1 wide x 2.5 tall: work in a space where x and y units match.
                float2 p = float2((i.uv.x - 0.5), (i.uv.y - 0.3) * 2.5);
                p.y -= sin(t * 1.4) * _BobAmount * 2.5;

                // The shard.
                float shard = SdRhombus(p, float2(0.2, 0.42));
                float inside = smoothstep(0.01, -0.01, shard);

                // Asleep: dark glass with faint inner cracks.
                float cracks = smoothstep(0.05, 0.0, RiftVoronoiEdge(p * 9.0 + _Seed));
                half3 sleep = _SleepTint.rgb * (0.25 + 0.35 * cracks) + _SleepTint.rgb * 0.5 * smoothstep(0.03, 0.0, abs(shard));

                // Awake: an inner flame swirling upward, a hot core and a bright rim.
                float r = length(p);
                float a = atan2(p.y, p.x);
                float swirl = RiftFbm(float2(a * 1.5 + r * 6.0 - t * 1.2, p.y * 4.0 - t * 2.0));
                half3 awake = accent * (0.6 + 1.6 * swirl) * (1.0 - smoothstep(0.0, 0.45, r)) + accent * 2.5 * exp(-r * 12.0);
                awake += accent * 2.0 * smoothstep(0.025, 0.0, abs(shard));

                half3 col = lerp(sleep, awake, _Lit) * inside;
                float alpha = inside;

                // Light beam rising from the shard, flickering softly.
                float beamW = 0.05 + 0.02 * sin(t * 3.0 + p.y * 4.0);
                float beam = smoothstep(beamW, 0.0, abs(p.x)) * smoothstep(0.3, 1.0, p.y) * (1.0 - smoothstep(1.2, 3.0, p.y));
                beam *= 0.6 + 0.4 * RiftNoise(float2(p.y * 3.0 - t * 2.0, 0.0));
                col += accent * beam * _Lit * 1.4;

                // Ground glow + halo around the shard.
                col += accent * exp(-max(shard, 0.0) * 14.0) * 0.5 * _Lit;
                col += _SleepTint.rgb * exp(-max(shard, 0.0) * 18.0) * 0.15 * (1.0 - _Lit);

                // Activation rings expanding outward.
                float ring = abs(r - _Pulse * 1.4);
                col += accent * smoothstep(0.03, 0.0, ring) * (1.0 - _Pulse) * 2.0 * step(0.001, _Pulse);

                alpha = saturate(max(alpha, dot(col, 0.33)));
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
