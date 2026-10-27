---
name: shader-port
description: Convert a Shadertoy (GLSL) or godotshaders.com shader into a Unity 6 URP HLSL shader for RIFT. Use when porting or writing any shader.
---

# Porting a shader to URP

## Template
```hlsl
Shader "RIFT/Name"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha  ZWrite Off  Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial) float4 _MainTex_ST; CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings  { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Varyings vert (Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = TRANSFORM_TEX(i.uv, _MainTex); o.color = i.color; return o; }
            half4 frag (Varyings i) : SV_Target { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color; }
            ENDHLSL
        }
    }
}
```

## GLSL → HLSL cheat sheet
| Shadertoy / Godot | Unity HLSL |
|---|---|
| `vec2/3/4`, `mat2/3` | `float2/3/4`, `float2x2/3x3` |
| `fract`, `mix`, `mod(x,y)` | `frac`, `lerp`, `x - y*floor(x/y)` (NOT `fmod` for negatives) |
| `texture(iChannel0, uv)` / `texture(TEXTURE, UV)` | `SAMPLE_TEXTURE2D(_Tex, sampler_Tex, uv)` |
| `iTime` / `TIME` | `_Time.y` |
| `iResolution` | `_ScreenParams.xy` |
| `fragCoord` | `i.positionCS.xy` (pixels) |
| `m * v` | `mul(m, v)` (check row/column order) |
| `atan(y, x)` | `atan2(y, x)` |
| Godot `SCREEN_TEXTURE` | URP `_CameraOpaqueTexture` / `_CameraSortingLayerTexture` (2D) |
| `const float` | `static const float` |

## Rules
- Credit the source: comment at the top with the original URL and author; respect its license.
- Expose the look as material properties (colour, speed, strength) — no hard-coded magic numbers.
- Global rift data comes from `_RiftCenter` (world xy), `_RiftRadius`, `_RiftEdge` set by `RiftLens` via `Shader.SetGlobal*`. Put shared helpers in `Assets/_RIFT/Shaders/RiftCommon.hlsl`.
- After writing: check the Unity console for shader compile errors and look at it in play mode.
