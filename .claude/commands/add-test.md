---
description: Add missing TDD tests (RED) for a feature without implementing them
argument-hint: "[number]"
---

You are executing the FMS `/add-test` command (TDD **RED** step). `$ARGUMENTS` is the
feature number (e.g. `05`) — resolve to `docs/features/<number>-<name>.md` and read it.

1. **Read** the feature's acceptance criteria and its **Tests (TDD)** block.
2. **Inspect** the existing backend test projects under `src/api/` (`Fms.Api.Test`, `Fms.Repository.Test`, `Fms.Service.Test`) to see what the layer(s) the feature touches already cover.
3. **Identify gaps** — acceptance-criteria behaviors not yet covered by a test.
4. **Write NEW failing test(s)** for those gaps, following the test tiers in
   `docs/testing-and-tdd.md` (backend xUnit unit + EF Core InMemory integration). Place them in
   the matching layer's test project under `src/api/` (e.g. `Fms.Service.Test`, `Fms.Api.Test`).
5. **Run the relevant suite** (`dotnet test`) and confirm
   the new tests **fail for the expected reason (RED)**.
6. **Report** the tests added and what each asserts — then **STOP**. Do not implement
   (GREEN is a separate step, e.g. via `/implement`).
