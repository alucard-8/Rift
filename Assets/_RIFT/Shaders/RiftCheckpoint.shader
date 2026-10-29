// Checkpoint "memory shard": a floating crystal that sleeps dim and cracked, then wakes with an inner
// flame, a soft light beam and expanding rings. Colour comes from the level's accent (_RiftAccent).
// It stirs when you come close, flares a pillar of light when reached, keeps a rune circle lit on the ground,
// and flashes a beam down when it calls you back after a death.
// Polar swirl / fbm after Inigo Quilez (iquilezles.org, MIT); rhombus SDF after IQ's 2D distance functions.
// Rune ring: polar tick marks on a flattened circle, after the "magic circle" Shadertoy family (e.g. https://www.shadertoy.com/view/4tGXzt).
Shader "RIFT/Checkpoint"
{
    Properties
    {
        _Lit ("Lit (0 asleep, 1 awake)", Range(0, 1)) = 0
        _Pulse ("Activation Pulse (0..1)", Range(0, 1)) = 0
        _Near ("Player Near (0..1)", Range(0, 1)) = 0
        _Flare ("Pillar Flare (1 -> 0)", Range(0, 1)) = 0
        _Recall ("Recall Flash (1 -> 0)", Range(0, 1)) = 0
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
                float _Lit, _Pulse, _Near, _Flare, _Recall, _Seed, _BobAmount;
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
                float2 ground = p;   // un-bobbed, for things that sit on the floor
                // Bobs quicker when you're close, as if it's noticed you.
                p.y -= sin(t * (1.4 + _Near * 1.6)) * _BobAmount * 2.5 * (1.0 + _Near * 0.6);
                // Lit shards breathe slowly so a row of them doesn't look frozen.
                float breath = 0.85 + 0.15 * sin(_Time.y * 1.4 + _Seed * 5.0);

                // The shard.
                float shard = SdRhombus(p, float2(0.2, 0.42));
                float inside = smoothstep(0.01, -0.01, shard);

                // Asleep: dark glass with faint inner cracks.
                float cracks = smoothstep(0.05, 0.0, RiftVoronoiEdge(p * 9.0 + _Seed));
                half3 sleep = _SleepTint.rgb * (0.25 + 0.35 * cracks) + _SleepTint.rgb * 0.5 * smoothstep(0.03, 0.0, abs(shard));
                // Stirring: the cracks catch a little of the accent as you approach, flickering like a held breath.
                float stir = _Near * (0.6 + 0.4 * RiftNoise(float2(t * 6.0, _Seed)));
                sleep += accent * cracks * stir * 0.9 + accent * smoothstep(0.03, 0.0, abs(shard)) * stir * 0.4;

                // Awake: an inner flame swirling upward, a hot core and a bright rim.
                float r = length(p);
                float a = atan2(p.y, p.x);
                float swirl = RiftFbm(float2(a * 1.5 + r * 6.0 - t * 1.2, p.y * 4.0 - t * 2.0));
                half3 awake = accent * (0.6 + 1.6 * swirl) * (1.0 - smoothstep(0.0, 0.45, r)) + accent * 2.5 * exp(-r * 12.0);
                awake += accent * 2.0 * smoothstep(0.025, 0.0, abs(shard));

                half3 col = lerp(sleep, awake * lerp(1.0, breath, _Lit), _Lit) * inside;
                float alpha = inside;

                // Light beam rising from the shard, flickering softly. When reached it flares into a wide pillar first.
                float beamW = 0.05 + 0.02 * sin(t * 3.0 + p.y * 4.0) + _Flare * 0.22;
                float beam = smoothstep(beamW, 0.0, abs(p.x)) * smoothstep(0.3 - _Flare * 0.6, 1.0, p.y) * (1.0 - smoothstep(1.2 + _Flare, 3.0, p.y));
                beam *= 0.6 + 0.4 * RiftNoise(float2(p.y * 3.0 - t * (2.0 + _Flare * 6.0), 0.0));
                col += accent * beam * (_Lit * 1.4 * breath + _Flare * 2.5 + _Near * (1.0 - _Lit) * 0.25);

                // Recall: a beam of light thrown straight down to where you reform, after a death.
                float recallBeam = smoothstep(0.12 * _Recall + 0.01, 0.0, abs(p.x)) * step(p.y, 0.4);
                col += accent * recallBeam * _Recall * 3.0;

                // Rune circle on the ground under the shard: dim outline asleep, lit and slowly turning once reached.
                float2 g = float2(ground.x, (ground.y + 0.6) * 3.2);
                float gr = length(g);
                float ga = atan2(g.y, g.x);
                float ringA = smoothstep(0.018, 0.0, abs(gr - 0.4));
                float ringB = smoothstep(0.012, 0.0, abs(gr - 0.31));
                float ticks = step(0.5, frac(ga * 3.8197 + t * 0.15)) * smoothstep(0.02, 0.0, abs(gr - 0.355)) ;
                float runes = (ringA + ringB * 0.7 + ticks) * smoothstep(0.0, 0.08, 0.5 - abs(ground.x));
                col += accent * runes * (_Lit * (0.8 + 0.4 * breath) + _Flare * 1.5 + _Recall * 2.0) + _SleepTint.rgb * runes * 0.25 * (1.0 - _Lit);
                col += accent * exp(-gr * 6.0) * 0.25 * (_Lit + _Flare);

                // Ground glow + halo around the shard.
                col += accent * exp(-max(shard, 0.0) * 14.0) * 0.5 * _Lit;
                col += _SleepTint.rgb * exp(-max(shard, 0.0) * 18.0) * 0.15 * (1.0 - _Lit);

                // Activation rings expanding outward.
                float ring = abs(r - _Pulse * 1.4);
                col += accent * smoothstep(0.03, 0.0, ring) * (1.0 - _Pulse) * 2.0 * step(0.001, _Pulse);
                // A second, softer ring trailing behind the first.
                float p2 = saturate(_Pulse * 1.35 - 0.35);
                float ring2 = abs(r - p2 * 1.1);
                col += accent * smoothstep(0.05, 0.0, ring2) * (1.0 - p2) * 1.2 * step(0.001, p2);

                alpha = saturate(max(alpha, dot(col, 0.33)));
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
