---
description: Select the next feature and implement it (TDD)
---

You are executing the FMS `/next` command. This is equivalent to `/implement` with no
arguments, using Phase 1 selection from `.claude/CLAUDE.md`.

1. **Select** — determine the next feature: the **lowest-numbered** file in `docs/features/`
   — excluding `00-mission-1-sprint.md`, `backlog.md`, and anything
   under `archive/` — that is not already done, archived, or in progress (check feature
   branches and open PRs).
2. If there is **no remaining feature**, report that and suggest pruning/adding to
   `docs/features/backlog.md` — then stop.
3. Otherwise, state which feature you are starting (file, number, priority, dependencies),
   then execute the full `/implement` workflow for it (Phases 1–5, stopping before commit
   for user approval).
