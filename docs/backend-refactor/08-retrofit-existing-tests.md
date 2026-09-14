# 08 — Retrofit existing tests and walk the smoke paths

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [05](05-persistence-soft-delete-and-audit.md), [06](06-http-surface-health-and-openapi.md), [07](07-drop-startup-migration.md)
Branch: `refactor/08-retrofit-existing-tests`

## Summary

Close out the refactor: bring the schema and query tests up to the new model, prove the soft-delete
filter actually composes over the raw-SQL query path, and walk the four end-to-end smoke paths that no
unit test covers.

## Scope

- `Fms.Repository.Test/DbSchemaTests.cs` — assert the new schema shape.
- `Fms.Repository.Test` — prove the query filter composes over
  `SubmissionRepository.QueryAsync`'s `FromSqlInterpolated` base.
- Full suite across all three test projects; then the four smoke paths below.
- Update [docs/testing-and-tdd.md](../testing-and-tdd.md) if any tier description drifted.

## Acceptance criteria

- [ ] `DbSchemaTests` asserts `uuid` column types, `isDeleted` on all five tables, the `auditLogs`
      table with both indexes, and the **absence** of the dropped `gen_random_uuid()` / `now()` defaults
- [ ] Every assertion on *absence* or on generated SQL was confirmed to fail when the thing under test
      is removed (see the trap)
- [ ] A soft-deleted submission is excluded from keyword search — the filter composes over the raw-SQL base
- [ ] A soft-deleted user can still log in (the provisioning trap from stage 05) — covered by a test
- [ ] `dotnet build` warning-free; `dotnet test` green across all three projects
- [ ] Bug 01's supersession is recorded in [docs/bugs/00-bug-log.md](../bugs/00-bug-log.md)

## The four smoke paths

Run the app with Postgres up, after `dotnet ef database update`:

1. `GET /health` returns `Healthy` — and also with **no** database configured.
2. `GET /api/me` with a test token → a `users` row is provisioned, and `auditLogs` gains an `Added`
   entry whose `EntityId` matches the new user's id. This is the payoff of application-assigned keys:
   the id exists before the save, so an insert is as traceable as an update.
3. `POST /api/spaces` as admin → `GET /api/spaces` finds it → `DELETE /api/spaces/{id}` → the space,
   its forms and their submissions all vanish from listings, each leaving an `auditLogs` row.
4. `GET /openapi/v1.json` (Development) lists the controllers; then
   `docker build --file src/api/Dockerfile src/api` and `docker compose up --build` boots and
   `/health` responds.

## Tests (TDD)

This stage is mostly *strengthening* existing tests rather than RED-first new behaviour, so the
discipline shifts to falsification:

- For anything asserting on generated SQL or on **absence**, comment out the thing under test and
  confirm the test goes **red** before trusting it. A substring check on a column name passes whether
  or not the filter is applied; assert on the `WHERE` clause, or pair it with a control test asserting
  the opposite.
- Name the condition and the expected result — `A_soft_deleted_user_can_still_log_in`, not `TestUser2`.
- One behaviour per test; use `[Theory]` for the same behaviour across inputs.

## Traps

- ⚠️ **A test that cannot fail is not a test.** This stage's whole purpose is to avoid the failure mode
  where the suite is green and proves nothing — particularly for the dropped defaults and the
  soft-delete filter, both of which are asserted by *absence*.
- ⚠️ `DbSchemaTests` will have been red since stage 05 (it asserts the old defaults). Confirm it is
  failing **for the expected reason** before rewriting it, rather than assuming.
- ⚠️ Do not let this stage absorb new feature work. If the smoke paths surface a genuine behavioural
  regression, fix it in the owning stage's follow-up, not here.

## Out of scope

- New product behaviour — this is a structural refactor.
- `docs/features/00-mission-1-sprint.md`, `backlog.md`, and feature 08 (remote-lookup-proxy) — untouched.
- The frontend (`src/web`) — Figma-Make generated, not test-first, unaffected beyond PRD wording.
