---
name: vfx-polish
description: Add juice to RIFT — particles, camera shake, hit-stop, post-processing — while keeping a deliberate, non-generic art direction. Use for any polish/feel pass.
---

# Polish pass

## Art bar ("not AI slop")
- Two worlds, two strict palettes. **Ember**: deep plum → burnt orange → cream. **Static**: near-black → cyan → hot magenta. Never mix them outside the lens edge.
- One accent per screen. Avoid default Unity white squares, default fonts, rainbow gradients and generic glow-on-everything.
- Shapes read before detail: strong silhouettes, consistent pixel/texel density.

## Juice checklist for every player-facing event
- **Particle burst** (short, few, purposeful — colour from the world it happens in).
- **Camera**: small shake scaled by impact; never on every frame.
- **Hit-stop**: 40–80 ms on big impacts only.
- **Sound** cue (via Audio, subscribed to the event).
- **Squash & stretch** on jumps/landings.

## Particles
- Prefabs in `Assets/Prefabs/Fx/`, spawned by `Fx/` listeners from gameplay events, pooled.
- Use URP 2D-compatible materials; additive for sparks, alpha for dust.
- Lens edge sparks spawn along the circle where it intersects Static geometry.

## Post-processing (URP Volume)
- Bloom (threshold ~1, so only HDR emissives glow), slight vignette, film grain low.
- Chromatic aberration lives in the lens shader, not global post.

Verify in play mode and take a screenshot before/after.
