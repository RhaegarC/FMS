# Backend Testing & TDD Strategy

Status: **Draft** (2026-08-06)

Referenced by [.claude/CLAUDE.md](../.claude/CLAUDE.md) — the TDD workflow (RED → GREEN → refactor) runs on the tiers below.

**Scope: the backend API** (`src/api`). The frontend UI is exported from Figma Make and is out of
scope for this TDD strategy — it is not test-first tested here.

The API is a layered solution under `src/api/` — `Fms.Model`, `Fms.Repository`, `Fms.Service`,
`Fms.Interface`, `Fms.Api` — and each layer carries a sibling xUnit test project (`Fms.Api.Test`,
`Fms.Repository.Test`, `Fms.Service.Test`). New tests go in the project matching the layer they
exercise.

## Test tiers

| Tier | Scope | Tooling |
|---|---|---|
| Backend unit | Services, permission evaluation, JSON Schema validation, export flattening | xUnit |
| Backend integration | EF Core against PostgreSQL (`Testcontainers`) or InMemory for fast CI | xUnit + EF Core |

## TDD discipline

1. **RED** — write a failing test for the behavior first; run it to confirm it fails for the right reason.
2. **GREEN** — minimal implementation to pass.
3. **Refactor** while green; run the full tier.

**Must be test-first (hot spots):**
- Permission evaluation service (security boundary)
- JSON Schema validation on submit

## Commands

- Backend (from `src/api/`): `dotnet test`
