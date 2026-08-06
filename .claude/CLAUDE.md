# Project: FMS

## Project Overview

A web platform where admins configure forms as standard JSON Schema and common users
fill them out and manage their own submissions — with space/dataset-level access control.

## Related Project Docs

- **Product requirements:** [docs/PRD.md](docs/PRD.md) — incl. [data model](docs/PRD.md#data-model)
- **Sprint goal / Definition of Done:** 
- **Feature backlog:** [docs/features/](docs/features/) — features `00-*` … `15-*`
- **Bug tracker:** [docs/bugs/00-bug-log.md](docs/bugs/00-bug-log.md) — bugs are tracked via the Bug Fix Workflow below (not part of the feature scan)
- **TDD strategy & test tiers:** [docs/testing-and-tdd.md](docs/testing-and-tdd.md)

## Development Workflow — TDD with Scrum

### Branch Strategy
- **`master`**: Production-ready code only
- **`develop`**: Integration branch for features
- **Feature branches**: `feature/[number]-[feature-name]` branched from `develop`
  - Example: `feature/02-entra-auth`
- **Never commit directly to** `master` or `develop`
- **One-time bootstrap** (do once, before features start): merge the initial scaffolding (feature `00-scaffolding`) into `develop`, then delete the temporary branch.

### Feature Naming Convention
Features are numbered by priority/order, matching the files in `docs/features/`:

**Rules**:
- Lower numbers = higher priority
- Features must be implemented in numerical order
- Dependencies: Feature `N` may depend on features `0` through `N-1`
- After completion, move the feature file to `docs/features/archive/` (create the folder)

**Exceptions (not features — excluded from the numeric scan):**
- `00-mission-1-sprint.md` — sprint plan / tracking, not a feature
- `backlog.md` — future ideas, not yet scheduled

**Foundation (before feature 01):** the TDD strategy + test scaffold — [docs/testing-and-tdd.md](docs/testing-and-tdd.md) — is set up in Phase 0.

### TDD Workflow (Strictly Follow)

#### Phase 0: Foundation (once, before feature 01)
1. Scaffold the solution + test projects per [docs/testing-and-tdd.md](docs/testing-and-tdd.md): backend `src/Fms.Api` + `tests/Fms.Tests` (xUnit); frontend vitest configured in `apps/*` and `packages/form-renderer`.
2. Confirm `dotnet test` runs green on an empty test project.

#### Phase 1: Feature Selection
1. Scan `docs/features/` for the **lowest numbered** feature file (ignore the exceptions: `00-mission-1-sprint.md`, `backlog.md`, and anything under `archive/`)
2. Verify it's not already in progress: check **branches and open PRs**
3. Read the feature file completely
4. Understand acceptance criteria and success metrics
5. Identify dependencies on lower-numbered features

**Selection Logic**:
Current feature = min(number of all available feature files, excluding exceptions)

#### Phase 2: Branch Creation
```bash
# Ensure develop is up to date
git checkout develop
git pull origin develop

# Create feature branch with number
git checkout -b feature/[number]-[feature-name]
# Example: git checkout -b feature/02-entra-auth
```

#### Phase 3: Write Tests First (RED)
1. Create or update test files based on acceptance criteria
2. Write failing tests that define expected behavior — tiers per [docs/testing-and-tdd.md](docs/testing-and-tdd.md): backend xUnit (unit) + EF Core InMemory (integration); frontend vitest + React Testing Library (unit/component)
3. Ensure tests fail (validate test correctness)
4. Test command: backend `dotnet test`; frontend `pnpm test` (workspace) — run the one(s) covering the feature's code

#### Phase 4: Implement Feature (GREEN)
1. Write minimal code to make tests pass
2. Follow existing code patterns and conventions
3. Add comments for complex logic
4. Ensure code is clean and maintainable

#### Phase 5: Refactor & Verify
1. Run all tests: backend `dotnet test`; frontend `pnpm test`
2. If tests fail:
  - Analyze failures
  - Fix issues (code or tests)
  - Re-run tests
3. Repeat until ALL tests pass
4. Refactor code while keeping tests green
5. Update documentation if needed — if the schema changed, update the [data model](docs/PRD.md#data-model) in `docs/PRD.md` and the feature file

#### Phase 6: Commit & Push
```bash
git status              # review what changed first
git add <scoped paths>  # e.g. git add src/ docs/features/<file> — NOT `git add .`
git commit -m "[type]: [description] (#[number]-[feature-name])"
# Types: feat, fix, docs, style, refactor, test, chore
# Example: git commit -m "feat: Implement Entra login (#01-entra-auth)"
git push -u origin feature/[number]-[feature-name]
```

#### Phase 7: Create Pull Request
1. Rebase the branch onto latest `develop` and resolve conflicts locally
2. Create PR targeting the `develop` branch
3. PR Title: `[#01] Implement Entra Auth`
4. PR Description must include:
```markdown
## Feature: [number]-[feature-name]

### Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

### Test Results
- All tests passing: ✅
- Test coverage: XX% (optional — add coverlet if coverage is tracked)
- New tests added: [count]

### Dependencies
- None (or list lower-numbered features)

### Deployment Notes
- Any special considerations
```
5. Wait for human review and approval
6. Do NOT merge without approval

#### Phase 8: Archive Feature (After PR Merge)
1. Verify feature PR is merged to `develop`
2. Move the completed feature file to `docs/features/archive/`
3. Update sprint tracking (if applicable)
4. Switch back to `develop` and sync with the remote: `git checkout develop && git pull --prune origin develop`
5. Delete the merged feature branch locally and remotely: `git branch -D feature/[number]-[feature-name]`

### Bug Fix Workflow

Bugs are tracked separately from features in `docs/bugs/` — they are **not** part of the `docs/features/` scan.

**Bug file conventions:**
- `docs/bugs/00-bug-log.md` — running bug tracker (tracking table; see Bug Fix Workflow below)
- `docs/bugs/[number]-[name].md` — one file per reported bug (no `bug-` prefix; the folder implies it)
- `docs/bugs/archive/` — resolved bugs moved here after the fix PR merges
- The number in the filename is the bug's identity; the Azure DevOps work item ID lives inside the file

**Flow (same TDD + PR core as features):**
1. **Triage** on report — severity decides the path:
   - **Normal** → branch `fix/[name]` off `develop`
   - **Critical / production-down** → branch `hotfix/[name]` off `master`
2. **RED**: write a failing regression test that reproduces the bug.
3. **GREEN**: minimal code to make it pass.
4. **Verify**: run `dotnet test`; refactor while green.
5. **PR**: create PR (`fix:` / `hotfix:` commit type) to `develop`, or to `master` for hotfixes.
6. **Close**: merge; for hotfixes, **merge `master` back into `develop`** so the fix isn't lost; move the bug file to `docs/bugs/archive/`; close the work item. The regression test stays in the suite.
7. **Cleanup** (mirrors Phase 8 steps 4–5): switch back to `develop` and sync with the remote: `git checkout develop && git pull --prune origin develop`.
8. **Delete the merged fix branch locally**: `git branch -D fix/[name]` (or `hotfix/[name]`) — the remote branch is auto-deleted when the PR completes.

### Requirements & Bug Capture Workflow

Creates the spec files that the Feature and Bug Fix workflows then consume. Runs **before** feature selection (Phase 1) or bug triage: a prioritized idea becomes `docs/features/NN-name.md`; a reported bug becomes `docs/bugs/NN-name.md`.

**Clarify first — the tool depends on the artifact:**
- **New feature** → use `/grill-me` to stress-test the idea into scope, acceptance criteria, and success metrics.
- **Reported bug** → use a triage checklist, not grilling: repro steps, expected vs actual, severity, environment/version, component. Create the Azure DevOps work item here so its ID goes into the file.

**Flow (docs-only — no TDD here; TDD happens in the downstream workflow):**
1. **Branch** off `develop`: `docs/feature-[nn]-[name]` or `docs/bug-[nn]-[name]`.
2. **Write the file** using the established template:
   - **Feature** → follow the structure of `docs/features/NN-name.md` (status, summary, dependencies, user story, acceptance criteria, tasks). The number claims the priority slot — only spec features in implementation order; vague ideas stay in `backlog.md`.
   - **Bug** → follow the archived bug file template (triage, reproduction, fix plan, close checklist); add a row to `docs/bugs/00-bug-log.md`.
3. **Commit & push**: `docs:` commit type.
4. **PR** to `develop`; the description quotes the acceptance criteria (feature) or repro + severity (bug). Review/merge gate as usual.
5. **Close-out**: same as Phase 8 — `git checkout develop && git pull --prune origin develop`, then `git branch -D` the spec branch.
