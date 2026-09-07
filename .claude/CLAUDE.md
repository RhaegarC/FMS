# Project: FMS

## Project Overview

A web platform where admins configure forms as standard JSON Schema and common users
fill them out and manage their own submissions — with space/dataset-level access control.

## Related Docs

- **Product requirements:** [docs/PRD.md](docs/PRD.md) — incl. [data model](docs/PRD.md#data-model)
- **TDD strategy & test tiers:** [docs/testing-and-tdd.md](docs/testing-and-tdd.md)
- **Backend implementation standard:** [docs/backend-standard.md](docs/backend-standard.md) — the common ASP.NET Core API layering & DI/EF rules (applies beyond FMS)
- **Bug log:** [docs/bugs/00-bug-log.md](docs/bugs/00-bug-log.md)

## Development Workflow

Work is done **test-first (TDD)**: write a failing test (RED) → minimal code to pass (GREEN) →
refactor while green, on the tiers in [docs/testing-and-tdd.md](docs/testing-and-tdd.md). TDD covers
the **backend API**; the frontend UI is Figma Make–generated and is not test-first tested.
Run backend tests with `dotnet test` (from `src/api/`).

Branch per change and land it via a pull request to `develop`; never commit directly to
`master` or `develop`.

The concrete steps live in on-demand tooling, not here:
- **Implement a change with TDD** → the `tdd-implement` agent → [.claude/agents/tdd-implement.md](.claude/agents/tdd-implement.md)
- **Fix a bug** → the `bug-fix` agent → [.claude/agents/bug-fix.md](.claude/agents/bug-fix.md)
- **Capture a new feature spec or bug report** → `/capture`
