# 05 — Persistence: soft delete, audit snapshots, migration

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [04](04-identity-seam-user-context-service.md)
Branch: `refactor/05-persistence-soft-delete-and-audit`

## Summary

The heaviest stage and the one that changes observable behaviour: deletes become soft, an `auditLogs`
table starts recording every change, and the repositories collapse onto a generic base. It carries
**three of the eight deviations** and the sharpest trap in the whole refactor.

## Scope

- `FmsDbContext`: `DbSet<AuditLog> AuditLogs`; `jsonb` column types plus indexes on
  `(TableName, EntityId)` and `Timestamp`; `ApplySoftDeleteFilter` applied **by convention** to every
  `EntityBase` type; drop `gen_random_uuid()` / `now()` defaults and `ClientUuidGenerator`.
- `AuditSaveChangesInterceptor`: adopt the template's `Capture` half — writes `AuditLogs` on the
  **same context**, so history lands in the same transaction as the change it describes. Move the file
  from `Fms.Repository/Audit/` to the `Fms.Repository/` root (the template has no `Audit/` subfolder)
  and make it `internal sealed`, granting test access via `InternalsVisibleTo`.
- The interceptor stays the **single source of truth** for the audit columns, so it keeps stamping
  `LastModifiedOn`/`LastModifiedBy` on insert as well as update (**deviation 9**).
- **New** `Fms.Interface/Repository/IDbRepository.cs` + `Fms.Repository/DatabaseRepository.cs`.
- Rewrite the five repositories on the base: generic CRUD from `DatabaseRepository`, bespoke queries
  (`SubmissionRepository.QueryAsync`, `SpaceRepository.GetByIdAsync`, …) stay on the derived type.
- Cascade the soft delete in `CatalogService`: `DeleteSpaceAsync` also soft-deletes the space's forms
  and their submissions; `DeleteFormAsync` soft-deletes its submissions.
- New EF migration: adds `auditLogs`, adds `isDeleted` ×5, drops the SQL defaults.
  `InitialCreate` is untouched. Run with `--project Fms.Repository --startup-project Fms.Api`.

## Acceptance criteria

- [ ] Every `EntityBase` type is filtered by convention — a type added later inherits the filter and
      **cannot** forget to register it
- [ ] Soft-deleted rows disappear from ordinary reads; `.IgnoreQueryFilters()` is the documented,
      deliberately-visible escape hatch for reading history
- [ ] `auditLogs` has the two indexes and `jsonb` `OldValues`/`NewValues` (a JSON document in `text` is
      a write-only blob; `jsonb` stays queryable)
- [ ] `Actor` is never null and takes three distinct values: the Entra object id, `"anonymous"` for an
      unauthenticated request, `"system"` for no request at all — a missing token and a background job
      are different problems and must not collapse into one `"unknown"`
- [ ] `EntityId` is populated **for inserts too** (the payoff of application-assigned keys)
- [ ] Audit rows are written on both `SaveChanges()` and `SaveChangesAsync()` — a synchronous save must
      not bypass auditing in silence
- [ ] `[NotAudited]` excludes a property from both snapshots
- [ ] A newly inserted row has **both** `createdOn` and `lastModifiedOn` stamped — `FormDto.UpdatedAt`
      on a freshly created form is the creation time, not `0001-01-01` (**deviation 9**)
- [ ] `AuditSaveChangesInterceptor` is `internal sealed` at the `Fms.Repository/` root, and
      `Fms.Repository.csproj` carries `<InternalsVisibleTo Include="Fms.Repository.Test" />` so the
      `[NotAudited]` tests exercise `SerializeEntity` rather than a reimplementation (**deviation 10**)
- [ ] `IDbRepository` methods take `CancellationToken cancellationToken = default` (**deviation 1**)
- [ ] `DatabaseRepository` exposes `protected FmsDbContext Context` (**deviation 2**) so
      `SubmissionRepository.QueryAsync` keeps its `.Include(s => s.User)`, composed filters and
      parameterized `data::text ILIKE` base
- [ ] `uuid` column type retained; `gen_random_uuid()` / `now()` defaults and `ClientUuidGenerator`
      gone (**deviation 3**)
- [ ] `DELETE /api/spaces/{id}` makes the space, its forms and their submissions all vanish from listings
- [ ] The new migration applies cleanly from scratch to an empty database
- [ ] `dotnet build` warning-free; `dotnet test` green

## Tests (TDD)

**RED first**, in `Fms.Repository.Test`, following the template's suite:
`AuditInterceptorTests`, `AuditRedactionTests`, `SoftDeleteTests`, and `KeyStrategyTests` extended.

Reuse the template's key insight: **EF Core runs save interception before it opens a connection**.
Point the context at a port nothing listens on and the audit entries are already staged in the change
tracker by the time the save fails — so audit behaviour is testable with **no database at all**. Build
the equivalent `TestSupport/AuditHarness.cs`, wiring the context through `AddRepositoryPersistence`
rather than by hand, so tests exercise the composition the application actually uses.

Use `.ToQueryString()` to inspect generated SQL without executing it.

## Traps

- ⚠️ **The sharpest trap in the refactor.** `UserService.GetOrCreateAsync()` looks up by
  `entraObjectId`, which carries a **unique index**. Once the soft-delete filter exists, a soft-deleted
  user becomes invisible to that lookup — so the next login tries to INSERT a duplicate and 500s. The
  lookup must use `.IgnoreQueryFilters()` (or the provisioning path must resurrect the row). Cover it
  with a test that soft-deletes a user and logs in again.
- ⚠️ **Do not copy the template's `Apply` verbatim.** It stamps only `CreatedOn`/`CreatedBy` on
  `Added`; FMS stamps `LastModifiedOn`/`LastModifiedBy` too. Adopting the template's version in the
  same change that drops the `now()` DB default (deviation 3) leaves a new row's `lastModifiedOn` at
  `default(DateTimeOffset)` — the column is `NOT NULL` but Postgres accepts year 1, so **the insert
  succeeds silently** and `FormDto.UpdatedAt` reports `0001-01-01` for every newly created form. These
  two halves must not be applied independently. Assert the stamped value in a test.
- ⚠️ **A soft delete has no DB cascade.** Today `DeleteBehavior.Cascade` removes forms and submissions
  when a space is hard-deleted; with soft delete that cascade does not fire, so forms would stay listed
  under a deleted space. Cascade explicitly in `CatalogService` (decided), or the API silently changes
  meaning.
- ⚠️ Verify the query filter **composes over** `SubmissionRepository.QueryAsync`'s
  `FromSqlInterpolated` base, so deleted submissions also drop out of keyword search. Assert on the
  generated `WHERE` clause, not on a substring of the column name — a substring check passes whether or
  not the filter is applied.
- ⚠️ `MapUuidId` currently sets both `HasValueGenerator<ClientUuidGenerator>()` and
  `HasDefaultValueSql("gen_random_uuid()")` for primary keys. Removing them is a **schema change** —
  the migration must drop the defaults, not just the C# code.
- ⚠️ `DatabaseRepository.DeleteAsync` in the template hardcodes `LastModifiedBy = "sys"` and issues one
  `FindAsync` per id with no transaction. Do **not** copy that: attribute the actor via
  `IUserContextService` so a real user's action is not recorded as the system's. Note the improvement
  in `STANDARD.md`.
- ⚠️ GitNexus before editing `FmsDbContext` / the interceptor:
  `gitnexus impact FmsDbContext.OnModelCreating --direction upstream`. `risk: UNKNOWN` is unresolved,
  not safe.

## Out of scope

- Startup auto-migration — stage [07](07-drop-startup-migration.md).
- Retrofitting `DbSchemaTests` to the new schema — stage [08](08-retrofit-existing-tests.md). It will
  be red on the dropped defaults until then; that is expected, not a regression to chase.
