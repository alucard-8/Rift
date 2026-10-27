---
name: git-workflow
description: The RIFT branch/commit/push routine. Use whenever creating branches, committing, pushing or merging in this repo.
---

# Git workflow

1. Start a feature: `git checkout main && git pull && git checkout -b feature/<short-name>`
2. Before committing:
   - Unity console has zero errors; scene and prefabs saved.
   - `git status` — only files of this feature; every asset has its `.meta`.
3. Commit (only when the user asks):
   `git add <files>` then `git commit -m "Add <thing>"` — imperative, ≤ 72 chars.
   **Never add `Co-Authored-By` or any AI attribution line.**
4. Push (only when the user asks): `git push -u origin <branch>`.
5. Merge after the feature was played and works: `git checkout main && git merge --no-ff feature/<short-name> && git push`.

Never: force-push, rewrite pushed history, commit `Library/`/`Temp/`/`Logs/`/`UserSettings/`/`Builds/`, or delete branches without asking.
