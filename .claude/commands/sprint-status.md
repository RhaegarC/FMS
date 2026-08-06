---
description: Show sprint progress, DoD status, branches, and open PRs
---

Produce a concise sprint status report for FMS:

1. **Feature progress** — list every feature in `docs/features/` (00–15) with status
   (not started / in progress / archived), based on file locations and git state.
2. **Sprint DoD** — check the Definition of Done items in
   `docs/features/00-mission-1-sprint.md` against what is actually complete (scaffolding
   done? Entra auth + admin seeding? admin config with live preview? user fill + dashboard?
   permissions enforced? conditional fields + remote lookups? export? E2E verified? TDD).
   Show checked vs. unchecked.
3. **Git state** — current branch, recent commits (`git log --oneline -5`), open
   `feature/*` branches, and any open PRs (use `gh pr list` if GitHub CLI is available).
4. **Next actions** — recommend the next feature to pick up and any blocked DoD items.

Present as a short table plus a two-line "next action" summary.
