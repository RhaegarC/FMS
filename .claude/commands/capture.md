---
description: Capture a new feature spec or bug report into docs/ (requirements & bug capture workflow)
argument-hint: "[feature|bug] [NN]"
---

You are executing the FMS `/capture` command (Requirements & Bug Capture Workflow in
`.claude/CLAUDE.md`).

`$ARGUMENTS` is a hint like `feature 04` or `bug 02`. Clarify with the user what is being captured.

1. **Clarify** the item before writing anything:
   - **Feature** → use the `/grill-me` skill to stress-test scope, acceptance criteria, and success metrics.
   - **Bug** → run a triage checklist (repro steps, expected vs actual, severity, environment/version, component) and create/note the Azure DevOps work item ID.
2. **Branch** off `develop`: `docs/feature-[nn]-[name]` or `docs/bug-[nn]-[name]`.
3. **Write the file** using the established template:
   - **Feature** → `docs/features/NN-name.md` following the structure of the existing feature files (status, summary, dependencies, user story, acceptance criteria, tasks).
   - **Bug** → follow the bug file template (triage, reproduction, fix plan, close checklist); also add a row to `docs/bugs/00-bug-log.md`.
4. **Commit & push**: `docs:` commit type; push the branch.
5. **Create the PR** to `develop` quoting the acceptance criteria (feature) or repro + severity (bug). Do NOT merge without approval.
6. **Report** the branch + PR link. After the PR merges, close-out is Phase 8 (`git checkout develop && git pull --prune origin develop`, then `git branch -D` the spec branch).
