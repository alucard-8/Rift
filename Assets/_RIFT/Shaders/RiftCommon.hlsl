#ifndef RIFT_COMMON_INCLUDED
#define RIFT_COMMON_INCLUDED

// Set every frame by RiftLens (Shader.SetGlobal*).
float2 _RiftCenter;
float  _RiftRadius;
float  _RiftEdge;

// Hash / value noise — adapted from Inigo Quilez's noise articles (iquilezles.org, MIT).
float RiftHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float RiftNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(RiftHash(i), RiftHash(i + float2(1, 0)), u.x),
                lerp(RiftHash(i + float2(0, 1)), RiftHash(i + float2(1, 1)), u.x), u.y);
}

// Fractal Brownian motion, 4 octaves.
float RiftFbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * RiftNoise(p);
        p = p * 2.03 + float2(17.1, 9.2);
        a *= 0.5;
    }
    return v;
}

// Signed distance from the lens rim: < 0 inside, > 0 outside.
float RiftDistance(float2 worldPos)
{
    return length(worldPos - _RiftCenter) - _RiftRadius;
}

// 1 inside the lens, 0 outside, with a noisy dissolve band of width _RiftEdge.
float RiftMask(float2 worldPos, float noiseScale, float time)
{
    float d = RiftDistance(worldPos);
    float n = (RiftNoise(worldPos * noiseScale + time) - 0.5) * _RiftEdge;
    return 1.0 - smoothstep(-_RiftEdge * 0.5, _RiftEdge * 0.5, d + n);
}

#endif
