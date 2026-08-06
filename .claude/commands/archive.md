---
description: Archive a completed feature file after its PR is merged to develop
argument-hint: "[number]"
---

You are executing the FMS `/archive` command (Phase 8 in `.claude/CLAUDE.md`).
`$ARGUMENTS` is the feature number (e.g. `02`) — resolve it to
`docs/features/<number>-<name>.md`.

**Only archive after the feature PR is merged to `develop`:**
1. **Verify the merge** — confirm the feature's changes are in `origin/develop` (check
   `git branch -a --contains <feature-branch>` on `develop`/`origin/develop`, or that the
   PR is closed/merged). If **not merged, stop** and explain why.
2. Create `docs/features/archive/` if it doesn't exist.
3. Move the file: `git mv docs/features/<file> docs/features/archive/<file>`.
4. Confirm the Phase 1 selection scan now skips the archived feature.
5. Report the archived feature and suggest updating sprint tracking
   (`docs/features/00-mission-1-sprint.md`).
