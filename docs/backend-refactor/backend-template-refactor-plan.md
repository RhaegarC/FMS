# Refactor `src/api` onto the `Rg.Backend.Api` solution template

## Context

`src/api` (FMS) was built against `docs/backend-standard.md` — an org-wide standard that the
solution is described as *reference-implementing*. Since then a proper solution template has
been installed: the NuGet package **`Rg.Backend.Api`** (`dotnet new BackendSolution`), whose
source repo is `/Users/rhaegar/Dev/github/BackendTemplate` and whose rules ship as
`src/content/STANDARD.md`.

The two disagree on real behaviour, not just style — keys, delete semantics, audit, error
contract, config keys and the test-project split. The goal is to make `src/api` follow the
template's structure and design so the codebase stops documenting one standard and
implementing another, while keeping the FMS domain (spaces/forms/submissions/permissions)
intact and green.

Analysis was done with **GitNexus** (FMS freshly indexed: 1,273 nodes / 2,840 edges / 98 flows;
`BackendTemplate` was already indexed). Two graph facts bound the risk:
`AuditSaveChangesInterceptor` has a **LOW** blast radius (2 files), but `Fms.Api/Program.cs`
and `Fms.Repository/FmsDbContext.cs` are the real chokepoints — nearly every change routes
through them.

## Decisions locked (from Q&A)

| Question | Decision |
|---|---|
| Project names | **Keep `Fms.*`** — adopt template structure/design, not its naming |
| Entity / key / audit | **Full adoption** — `EntityBase`, app-assigned keys, `IsDeleted` soft delete, `AuditLog` table |
| Test projects | **Keep the 3 tiers** (`Fms.Api.Test`, `Fms.Repository.Test`, `Fms.Service.Test`) |
| OpenAPI | **Adopt** — `AddOpenApi`, `/openapi/v1.json` |
| Error contract | **Do NOT adopt** — keep `ExceptionHandlingMiddleware` + `ApiError` |
| Config keys | **Adopt** — flat keys via `Constant.ConfigKey` |
| Health + identity seam | **Adopt** — `UseHealthChecks`, `IUserContextService`, provisioning in `UserService` |
| Dockerfile | **Adopt** — `src/api/Dockerfile`, build context `src/api` |
| Startup migration | **Drop** `Database.Migrate()` — match the template |
| Standard doc | **Replace** `docs/backend-standard.md` with `src/api/STANDARD.md` |
| Soft delete | **Cascade** — deleting a space/form also soft-deletes its children |

## Target structure

```
src/api/
  Fms.slnx
  STANDARD.md                     <- new, template-derived (replaces docs/backend-standard.md)
  Dockerfile                      <- moved from Fms.Api/Dockerfile; context = src/api
  Fms.Api/
    Program.cs                    <- composition root only, thin
    ServiceExt.cs                 <- new: RegistService / AddEntraAuthentication / AllowCORS
    UserContextService.cs         <- new: IUserContextService over HttpContext claims
    Auth/                         <- AdminOnlyPolicy, UserProvisioningMiddleware (thin), UserService-backed
    Controllers/                  <- HealthController deleted
    Contracts/
  Fms.Interface/
    Repository/  Service/  Infrastructure/   <- Infrastructure/ is new (IUserContextService)
  Fms.Model/
    Constant.cs                   <- new: App / Message / ConfigKey
    NotAuditedAttribute.cs        <- new
    DatabaseEntity/               <- Entities/ renamed; EntityBase, AuditLog, + 5 entities
  Fms.Repository/
    FmsDbContext.cs               <- OnModelCreating + ApplySoftDeleteFilter
    DatabaseRepository.cs         <- new: generic IDbRepository base
    PersistenceExtensions.cs      <- moved from Extensions/, adds repositories
    AuditSaveChangesInterceptor.cs
    Migrations/
  Fms.Service/
    Users/UserService.cs          <- replaces UserProvisioner
    (Extensions/ deleted — registration moves to Api/ServiceExt.cs)
  Fms.Api.Test/  Fms.Repository.Test/  Fms.Service.Test/
```

## Deviations the template cannot express (record in `STANDARD.md` §12)

These are the cases where blind adoption breaks FMS. Each needs a code change *and* a written
rule, because the next person will otherwise "simplify" it away.

1. **`IDbRepository` gains `CancellationToken`.** FMS threads cancellation through every
   repository call; the template's generic methods take none. Add
   `CancellationToken cancellationToken = default` to each method.
2. **`DatabaseRepository` exposes `protected FmsDbContext Context`.** The template holds it
   `private readonly`. FMS needs `SubmissionRepository.QueryAsync` — `.Include(s => s.User)`,
   composed filters, and a parameterized `data::text ILIKE` raw-SQL base that no
   `Expression<Func<T,bool>>` predicate can express. Generic CRUD comes from the base; bespoke
   queries stay on the derived repository.
3. **`uuid` columns are retained.** FMS's DB contract (PRD, `DbSchemaTests`) is a Postgres
   `uuid` type. Keep the `string ⇄ Guid` converter, but move *assignment* into
   `EntityBase.Id = Guid.NewGuid().ToString()`, drop `ClientUuidGenerator` and the
   `gen_random_uuid()` / `now()` DB defaults so the interceptor stays the single source of truth.
4. **Auth keeps a test escape hatch.** `Entra:TestSigningKey` + `Issuer` (self-issued RS256)
   exists so `AuthIntegrationTests` can validate real token validation with no tenant. The
   template has no equivalent. `AddEntraAuthentication` therefore has three branches:
   TestSigningKey → test scheme; TenantId + Audience → Entra; neither → no scheme + startup warning.
5. **`ADMIN_USER_IDS` is retained**, resolved at the composition root and passed *down* as a
   value. Today `UserProvisioner` injects `IConfiguration` into the Service layer, violating
   the template's §6 ("only `Api` reads configuration") — the refactor fixes that.
6. **Provisioning stays in middleware.** The template provisions inside
   `UserService.GetOrCreateAsync()` called by `MeController`, so only that endpoint has a user.
   FMS's `AdminOnly` policy handler and every `FmsApiControllerBase.CurrentUser` read the
   provisioned row from `HttpContext.Items`. Keep `UserProvisioningMiddleware` as a thin
   Api-layer adapter that calls `IUserService.GetOrCreateAsync()` — the template's service and
   interface, FMS's per-request timing.
7. **Errors.** Keep `ExceptionHandlingMiddleware` + `ApiError{message}` (chosen), not
   `AddProblemDetails`/RFC 9457.
8. **Three test projects**, not one `*.Tests`.
9. **The interceptor stamps `LastModified*` on insert too.** The template's `Apply` leaves
   `LastModifiedOn` untouched when an entry is `Added`; FMS stamps it. Adopting the template verbatim
   *while also* dropping the `now()` default (deviation 3) leaves a new row's `lastModifiedOn` at
   `default(DateTimeOffset)` — and because the column is `NOT NULL` but Postgres accepts year 1, the
   insert **succeeds silently** with a sentinel. `DtoMapper.ToFormDto` maps `FormDto.UpdatedAt` from
   `LastModifiedOn`, so every newly created form would report `0001-01-01`. Keep FMS's insert stamping.
10. **The interceptor is `internal`, and tests reach it via `InternalsVisibleTo`.** The template keeps
    `AuditSaveChangesInterceptor` internal (it is wired up through `AddRepositoryPersistence`, not part
    of the layer's public surface) and exposes `SerializeEntity` as `internal` so the test project can
    call the real redaction rules rather than a reimplementation. Adopting that needs
    `<InternalsVisibleTo Include="Fms.Repository.Test" />` in `Fms.Repository.csproj` — FMS has no
    `InternalsVisibleTo` today. Testing a reimplementation would prove nothing about these rules.

## Sub-tasks

Eight PR-sized stages. Each is independently green (`dotnet build` warning-free, `dotnet test`
passing), lands on its own branch -> PR into `develop`, and is TDD'd per `.claude/CLAUDE.md`. This is
a ~5,000-line backend; one PR would be unreviewable.

| # | Stage | Depends on |
|---|---|---|
| [01](01-capture-spec-and-standard.md) | Capture the spec and the standard | -- |
| [02](02-foundation-config-and-composition.md) | Foundation: `Constant`, config keys, `ServiceExt`, Dockerfile | 01 |
| [03](03-model-entitybase-and-database-entity.md) | Model: `EntityBase`, `NotAudited`, `DatabaseEntity/` | 02 |
| [04](04-identity-seam-user-context-service.md) | Identity seam: `IUserContextService` + `UserService` | 03 |
| [05](05-persistence-soft-delete-and-audit.md) | Persistence: soft delete, audit snapshots, migration | 04 |
| [06](06-http-surface-health-and-openapi.md) | HTTP surface: health checks, OpenAPI | 02 |
| [07](07-drop-startup-migration.md) | Drop startup migration | 02, 05 |
| [08](08-retrofit-existing-tests.md) | Retrofit existing tests, walk the smoke paths | 05, 06, 07 |

**Two ordering corrections** to the original S0-S7 sequence, both to preserve "each stage
independently green":

1. **The identity seam now precedes the persistence rework** (originally S4 after S3). The template's
   new `AuditSaveChangesInterceptor` needs `IUserContextService.HasActiveRequest` to distinguish
   `"anonymous"` from `"system"`, so that seam must already exist.
2. **Dropping `Database.Migrate()` became its own late stage** (it was folded into S1). Three
   integration test classes depend on that call to build their Testcontainers schema, so removing it
   in S1 made that stage non-green. It is retained through stages 02-06 and removed in stage 07
   together with the test migrator.

## Verification

Per stage, from `src/api/`:
- `dotnet build` — warning-free (nullable is enabled; fix warnings, don't suppress).
- `dotnet test` — all three test projects green.
- **GitNexus before each commit** (per the template's discipline): `gitnexus impact <symbol> --direction upstream`
  before editing `Program.cs`, `FmsDbContext`, or `AuditSaveChangesInterceptor`; report `risk`,
  and treat `risk: UNKNOWN` as unresolved rather than safe. `gitnexus detect-changes --scope all`
  before committing — `partial`/`truncated` is not a clean check.

End-to-end, after stage [08](08-retrofit-existing-tests.md):
1. `dotnet run --project Fms.Api` with Postgres up → apply the migration via
   `dotnet ef database update` (startup no longer does it) → `GET /health` returns `Healthy`
   with no DB configured too.
2. `GET /api/me` with a test token → `users` row provisioned; `auditLogs` gains an `Added` entry
   whose `EntityId` matches the new user's id (the payoff of application-assigned keys).
3. `POST /api/spaces` as admin → `GET /api/spaces` finds it → `DELETE /api/spaces/{id}` → the
   space, its forms and their submissions all vanish from listings (cascade), and each leaves a
   row in `auditLogs`.
4. `GET /openapi/v1.json` (Development) lists the controllers; `dotnet docker build` via
   `--file src/api/Dockerfile` with context `src/api`; `docker compose up --build` boots and
   `/health` responds.

## Out of scope

- `docs/features/00-mission-1-sprint.md`, `backlog.md` and feature 08 (remote-lookup-proxy) —
  untouched; this is a structural refactor, not new behaviour.
- The frontend (`src/web`) — Figma-Make generated, not test-first, and unaffected beyond the
  PRD wording.
- The template's own documented gaps (its §12: `DeleteAsync` hardcodes `LastModifiedBy = "sys"`,
  N round trips per delete, no transaction). FMS should adopt the *interceptor-maintained* audit
  columns instead, so §12's items 1 and 5 are fixed by construction here — worth noting in
  `src/api/STANDARD.md` as deliberate improvements over the template.
