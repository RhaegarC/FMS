# 03 — Model: `EntityBase`, `NotAudited`, `DatabaseEntity/`

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [02](02-foundation-config-and-composition.md)
Branch: `refactor/03-model-entitybase-and-database-entity`

## Summary

Replace FMS's `IAuditable` interface with the template's `EntityBase` abstract class, add the audit
types, and move the entities into `Model/DatabaseEntity/`. **Wide but behaviour-neutral** — the
schema, the key strategy and the delete semantics all stay as they are until
stage [05](05-persistence-soft-delete-and-audit.md).

## Scope

- **New** `Fms.Model/DatabaseEntity/EntityBase.cs` — abstract; `Id` = `Guid.NewGuid().ToString()`
  in the initializer, plus `CreatedBy`, `CreatedOn`, `LastModifiedBy`, `LastModifiedOn`, `IsDeleted`.
- **New** `Fms.Model/DatabaseEntity/AuditLog.cs` — deliberately **not** an `EntityBase` (history is
  append-only, so soft-delete and last-modified columns would describe something that cannot happen).
- **New** `Fms.Model/NotAuditedAttribute.cs`.
- Move the five entities (`User`, `Space`, `Form`, `Submission`, `Permission`) from
  `Fms.Model/Entities/` to `Fms.Model/DatabaseEntity/`; delete `IAuditable`.
- Namespace `Fms.Model.Entities` → `Fms.Model.DatabaseEntity` (~30 files across all layers).
- Mechanical follow-on: `FmsDbContext.MapAudit<T>` constraint and the interceptor's
  `Entries<IAuditable>()` → `Entries<EntityBase>()`.

## Acceptance criteria

- [ ] `EntityBase` is abstract, in `Fms.Model.DatabaseEntity`, with the six members above
- [ ] `EntityBase.Id` is assigned at construction — a new entity is identifiable before it is saved
- [ ] `AuditLog` does not derive from `EntityBase`; `Actor`, `TableName` and `Action` are `required`;
      `Timestamp` is `DateTimeOffset`; `OldValues`/`NewValues` are nullable strings
- [ ] `NotAuditedAttribute` targets properties, `Inherited = true`, `AllowMultiple = false`
- [ ] `Fms.Model.Entities` no longer exists; no file references `IAuditable`
- [ ] Keys still round-trip as Postgres `uuid` (the `string ⇄ Guid` converter is untouched)
- [ ] `Fms.Model` still references **nothing** (no EF Core, no ASP.NET)
- [ ] `dotnet build` warning-free; `dotnet test` green — behaviour is unchanged

## Tests (TDD)

**RED first** — `Fms.Repository.Test/KeyStrategyTests.cs`: a newly constructed entity has a
non-empty `Id` before any save, and two constructions produce different ids. Assert the *observable*
claim (identifiable before save), not the implementation.

The template's §12 notes its own `KeyStrategyTests` exists because a database-generated key cannot
satisfy this — the row's id does not exist until after the save, which is what makes an audit
`EntityId` impossible to populate on insert.

## Traps

- ⚠️ **This is the highest-churn, lowest-semantics step.** Land it as its own commit. `gitnexus` in
  this repo has **no `rename` subcommand** (that is MCP-only and not wired up here), so drive the move
  from compiler errors — `dotnet build` after deleting `IAuditable` gives you the exact worklist.
  Do **not** blind find-and-replace across the solution.
- ⚠️ `ControllerBase.User` already names `ClaimsPrincipal`; FMS's base controller fully qualifies
  `Fms.Model.Entities.User` for that reason. That qualification must be updated too, not just the usings.
- ⚠️ Keep the DB defaults (`gen_random_uuid()`, `now()`) and `ClientUuidGenerator` in place at this
  stage. Removing them is stage 05's job, together with the migration that drops them — do it here and
  you will ship a model/schema mismatch with no migration to reconcile it.
- ⚠️ `Fms.Model` must not gain a project or package reference. It is the bottom of the graph; the
  moment it depends on something, every project depends on that something.

## Out of scope

- `IsDeleted` behaviour, the query filter, `AuditLog` writes and its indexes — stage [05](05-persistence-soft-delete-and-audit.md).
  This stage only introduces the *types*.
- `DatabaseRepository` / `IDbRepository` — also stage 05.
