---
name: rift-feature
description: Plan, build and verify ONE gameplay feature in RIFT. Use whenever starting a new feature or mechanic (lens, player, enemies, levels, UI).
---

# Building one RIFT feature

1. **Scope it.** Write one sentence: what the player can do after this feature that they couldn't before. If it needs "and", split it.
2. **Plan.** For anything beyond a small fix, use plan mode. List: scripts to add/change (with their folder from AGENTS.md), ScriptableObject configs, prefabs/scene changes, events raised/consumed.
3. **Branch.** `git checkout -b feature/<short-name>` (see `git-workflow`).
4. **Build in this order:**
   1. Data (ScriptableObject config with tunables).
   2. Gameplay logic (raises events, knows nothing about Fx/Audio/UI).
   3. Scene/prefab wiring.
   4. Presentation hooks (Fx/Audio/UI subscribe to the events).
5. **Verify.**
   - Read the Unity console: zero errors, no new warnings.
   - Enter play mode and actually exercise the feature (Unity MCP: `manage_editor` play, screenshots via `manage_camera`).
   - Check the edge cases: lens edge, lens off-screen, player inside a platform when the lens leaves it.
6. **Report** what works, what's not done, and stop. Commit only when the user asks.

## RIFT-specific rules
- Anything that belongs to the other world uses the `RiftMember` component and asks `RiftLens` whether it's inside — never compute the circle test yourself.
- The player must never be crushed unfairly: if a Static platform vanishes while the player stands inside it, push them out instead of killing them.
