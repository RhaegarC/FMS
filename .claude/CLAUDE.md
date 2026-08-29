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
- **Backend implementation standard:** [docs/backend-standard.md](docs/backend-standard.md) — the common ASP.NET Core API layering & DI/EF rules (applies beyond FMS)

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

### TDD Workflow
Executed by the **`tdd-implement`** agent → [.claude/agents/tdd-implement.md](.claude/agents/tdd-implement.md). Covers Phases 0–8: feature selection, branch creation, RED → GREEN → refactor & verify, commit & push, PR, and archive.

### Bug Fix Workflow
Executed by the **`bug-fix`** agent → [.claude/agents/bug-fix.md](.claude/agents/bug-fix.md). Covers: triage → RED regression test → GREEN → verify → PR → close → cleanup. Bugs are tracked in `docs/bugs/`, separate from the `docs/features/` scan.

### Requirements & Bug Capture Workflow
Executed by the **`/capture`** command → [.claude/commands/capture.md](.claude/commands/capture.md). Turns a prioritized idea into `docs/features/NN-name.md` or a bug report into `docs/bugs/NN-name.md`, before feature selection (Phase 1) or bug triage.
