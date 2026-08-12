# Testing & TDD Strategy

Status: **Draft** (2026-08-06)

Referenced by [.claude/CLAUDE.md](../.claude/CLAUDE.md) — the TDD workflow (RED → GREEN → refactor) runs on the tiers below.

## Test tiers

| Tier | Scope | Tooling |
|---|---|---|
| Backend unit | Services, permission evaluation, JSON Schema validation, export flattening | xUnit |
| Backend integration | EF Core against PostgreSQL (`Testcontainers`) or InMemory for fast CI | xUnit + EF Core |
| Frontend unit | `form-renderer` logic: widget mapping, conditional engine, cascade ordering, value state | vitest |
| Frontend component | Widget + portal components (both apps, shared packages) | vitest + React Testing Library |
| E2E (Phase 6) | Full flow across both portals (admin config → user fill → submission → admin view → export) | manual script; Playwright deferred |

## TDD discipline

1. **RED** — write a failing test from the feature's acceptance criteria first; run it to confirm it fails for the right reason.
2. **GREEN** — minimal implementation to pass.
3. **Refactor** while green; run the full tier.

**Must be test-first (hot spots):**
- Permission evaluation service (security boundary) — feature `05`
- Conditional-field engine + cascade ordering — feature `09` / `10`
- JSON Schema validation on submit — feature `07`

## Commands

- Backend (from `src/api/`): `dotnet test`
- Frontend — the pnpm workspace root is `src/web/`. Run from there:
  - `pnpm test` (workspace) or `pnpm --filter <package> test`
  - From the repo root: `pnpm --dir src/web test`
