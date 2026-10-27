# AGENTS.md — RIFT

Rules for any AI agent (OpenCode) working in this repo. Read this before touching anything.

## The game
**RIFT** is a 2D platformer. The player moves with the keyboard and holds a **lens** with the mouse.
The lens shows an alternate world, and anything inside the lens is *physically real*:
platforms, walls, hazards and enemies from the other world only exist (collide, hurt, move) inside the circle.
Hold the lens over a gap to bridge it; slide it off an enemy to erase it.

- **World A — "Ember"**: warm, painterly, dusk light.
- **World B — "Static"**: cold neon, scanlines, glitch.
- The lens edge is the star of the show: chromatic aberration, ripple, heat haze, sparks.

## Tech
- Unity **6000.6** (Unity 6), **URP 2D Renderer**, new **Input System**.
- Shaders: hand-written HLSL `.shader` files for URP in `Assets/_RIFT/Shaders/` (shared code in `.hlsl` includes).
  Shader Graph only when a hand-written shader is clearly worse.
- Unity MCP (`com.coplaydev.unity-mcp`) is installed; agents may drive the editor through it.

## Where you may write
- **Write only inside `Assets/_RIFT/`** (all game code, shaders, art, prefabs, scenes live there).
- You may *read* anything else (packages, `ProjectSettings/`, third-party folders) but never modify it,
  except `Packages/manifest.json` and `ProjectSettings/` when a feature explicitly needs it and the user agreed.
- Always use the **new Input System** (`UnityEngine.InputSystem`), never `Input.GetKey`/`Input.GetAxis`.
- Talk to the editor through **Unity MCP**; check the console after every change.

## Architecture
```
Assets/_RIFT/
  Scripts/
    Core/       bootstrapping, services, event bus, ScriptableObject configs
    Rift/       lens input, lens mask, world-membership & physics toggling
    Player/     movement, jump, death/respawn
    Enemies/    enemy behaviours
    Levels/     checkpoints, goals, level flow
    Fx/         camera shake, hit-stop, particle spawners (react to events only)
    Audio/      sound (reacts to events only)
    UI/         HUD, menus
  Shaders/      .shader + .hlsl
  Materials/  Prefabs/  Scenes/  Art/  Audio/  Data/ (ScriptableObjects)
```
- **One responsibility per class.** Small MonoBehaviours, composition over inheritance.
- **Gameplay systems announce, presentation reacts.** Gameplay raises C# events; Fx/Audio/UI subscribe. Gameplay never references Fx/Audio/UI.
- **Tunables live in ScriptableObjects** (`Data/`), never as magic numbers in code.
- **Lens is the single source of truth** for "is this point inside the rift": everything (shaders via global shader properties, physics, enemies) asks `RiftLens`.
- No `FindObjectOfType` / `GameObject.Find` in gameplay code; wire references in the inspector or via a service.
- No singletons except a minimal service locator in `Core/`.
- Naming: `PascalCase` types/methods, `_camelCase` private fields, `[SerializeField] private` over public fields.
- Every feature must leave the project compiling with **zero console errors**.

## Workflow
- **One feature at a time.** Plan it first (plan mode for anything bigger than a small fix), build it, test it in play mode, commit it, then move on. Never one-shot several features.
- Check the Unity console after every script change.
- Keep the game playable at the end of every feature.

## GitHub rules
- `main` always builds and plays. Never commit broken code to `main`.
- Each feature gets its own branch: `feature/<short-name>` (e.g. `feature/lens-mask`), fixes use `fix/<short-name>`.
- Small, focused commits. Message format: imperative, ≤ 72 chars, e.g. `Add lens chromatic edge shader`.
- **Commit and push only when the user asks.** Never force-push, never rewrite pushed history, never delete branches without asking.
- **No AI co-author lines** (`Co-Authored-By: …` or similar) in commits or PR descriptions. The author is the user.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, IDE files — `.gitignore` covers them; don't override it.
- Always commit `.meta` files together with their assets.
- Before committing: console has no errors, the scene is saved, `git status` contains only files belonging to this feature.
- Merge a feature into `main` only after it was played and works.

## Skills
Project skills live in `.opencode/skills/`:
- `rift-feature` — how to plan, build and verify one feature.
- `shader-port` — converting Shadertoy / Godot shaders to URP HLSL.
- `vfx-polish` — particles, camera juice and the "no AI slop" art bar.
- `git-workflow` — the branch/commit routine above, step by step.
- `unity-mcp-skill` — how to drive the Unity editor through MCP (scenes, GameObjects, scripts, console, play mode).
