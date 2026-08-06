---
description: Add missing TDD tests (RED) for a feature without implementing them
argument-hint: "[number]"
---

You are executing the FMS `/add-test` command (TDD **RED** step). `$ARGUMENTS` is the
feature number (e.g. `05`) — resolve to `docs/features/<number>-<name>.md` and read it.

1. **Read** the feature's acceptance criteria and its **Tests (TDD)** block.
2. **Inspect** the existing test projects (backend `tests/Fms.Tests`; frontend vitest suites) to see what is already covered.
3. **Identify gaps** — acceptance-criteria behaviors not yet covered by a test.
4. **Write NEW failing test(s)** for those gaps, following the test tiers in
   `docs/testing-and-tdd.md` (backend xUnit unit + EF Core InMemory integration; frontend
   vitest + React Testing Library unit/component). Place them in the correct test files under
   `tests/Fms.Tests` or the relevant frontend package.
5. **Run the relevant suite** (`dotnet test` for backend, `pnpm test` for frontend) and confirm
   the new tests **fail for the expected reason (RED)**.
6. **Report** the tests added and what each asserts — then **STOP**. Do not implement
   (GREEN is a separate step, e.g. via `/implement`).
