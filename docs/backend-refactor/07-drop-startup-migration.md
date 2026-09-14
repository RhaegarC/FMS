# 07 — Drop startup migration

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [02](02-foundation-config-and-composition.md) (and any stage that changed the schema, i.e. [05](05-persistence-soft-delete-and-audit.md))
Branch: `refactor/07-drop-startup-migration`

## Summary

The template's `Program.cs` does not migrate on boot — migrations are applied out-of-band with
`dotnet ef database update`. FMS currently calls `Database.Migrate()` at startup, and **three
integration test classes depend on that call** to build their Testcontainers schema. This stage removes
the call and gives those classes their own migration step, in one change.

## Scope

- Remove the `using (var scope = app.Services.CreateScope()) { … Database.Migrate() … }` block from
  [Program.cs](../../src/api/Fms.Api/Program.cs).
- Add `Fms.Api.Test/TestSupport/` with a helper that migrates the container **before** the
  `WebApplicationFactory` is created.
- Update the three classes that relied on startup migration:
  [AuthIntegrationTests](../../src/api/Fms.Api.Test/AuthIntegrationTests.cs),
  [SpaceFormCrudTests](../../src/api/Fms.Api.Test/SpaceFormCrudTests.cs),
  [SubmissionApiTests](../../src/api/Fms.Api.Test/SubmissionApiTests.cs).

## Acceptance criteria

- [ ] `Program.cs` contains no `Database.Migrate()` call
- [ ] The three Testcontainers classes build their schema explicitly before creating the factory
- [ ] The no-DB classes (`HealthEndpointTests`, `CorsEndpointTests`, `SwaggerSpecTests`) still boot —
      they rely on the context being registered **without a provider** when the connection string is
      absent
- [ ] `dotnet test` green from a clean checkout with no pre-existing database
- [ ] `dotnet ef database update` is documented in `src/api/STANDARD.md` as the replacement workflow

## Tests (TDD)

No new behaviour to test — this stage **restores** green to tests that a naive implementation would
break. The RED is real and easy to see: remove the migrate block without the helper and all three
classes fail with `relation "users" does not exist`. Confirm that failure first, then add the helper.

Reuse the pattern already in
[DbSchemaTests.cs:36-43](../../src/api/Fms.Repository.Test/DbSchemaTests.cs#L36-L43) — a
`DbContextOptionsBuilder` + `MigrateAsync`.

## Traps

- ⚠️ **This stage exists because ordering matters.** Dropping the migrate call in stage
  [02](02-foundation-config-and-composition.md) — where `Program.cs` is rewritten — would make that stage
  non-green, violating the "each stage independently green" rule. The call is deliberately retained
  through stages 02–06.
- ⚠️ The helper must run **before** the factory is built. `WebApplicationFactory` creates the host
  lazily on first use, and the classes then assert `/health` in `InitializeAsync` — a helper that runs
  afterwards leaves them racing an un-migrated database.
- ⚠️ Keep `AddRepositoryPersistence(null)` registering the context with no provider. If the new
  implementation requires a connection string, the no-DB tests stop booting and the failure looks
  unrelated to this stage.
- ⚠️ Removing startup migration changes the dev loop: a fresh `docker compose up` no longer creates the
  schema. Document the `dotnet ef database update` step in `STANDARD.md` **in this change**, or the next
  person meets an unexplained "relation does not exist".

## Out of scope

- Any schema change — stage [05](05-persistence-soft-delete-and-audit.md).
- Retrofitting `DbSchemaTests`' column assertions — stage [08](08-retrofit-existing-tests.md).
