# 02 — Foundation: `Constant`, config keys, `ServiceExt`, Dockerfile

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [01](01-capture-spec-and-standard.md)
Branch: `refactor/02-foundation-config-and-composition`

## Summary

Adopt the template's composition-root shape and its flat configuration keys. This is the plumbing
stage: **no domain behaviour changes**, but it is the widest-blast-radius stage in the sequence
because [Fms.Api/Program.cs](../../src/api/Fms.Api/Program.cs) and every deployment config move at once.

## Scope

- **New** `Fms.Model/Constant.cs` — `App.CORSPolicyName`, `App.HealthCheckUrl`,
  `Message.NoAllowedOrigins`, and `ConfigKey` with `DbConnection`, `TenantId`, `Audience`,
  `AllowedOrigins`, `TestSigningKey`, `Issuer`, `AdminUserIds`.
- **New** `Fms.Api/ServiceExt.cs` — `RegistService` / `AddEntraAuthentication` / `AllowCORS`, ported
  from the template and extended for deviations 4 (test signing key) and 5 (admin ids).
- Rewrite [Program.cs](../../src/api/Fms.Api/Program.cs) down to the template's shape.
- Move repository registration: `Fms.Repository/Extensions/FmsPersistenceExtensions.cs` →
  `Fms.Repository/PersistenceExtensions.cs`. Delete `Fms.Service/Extensions/FmsServiceExtensions.cs`
  (registration moves into `Fms.Api/ServiceExt.cs`).
- Flat key rename across [appsettings.json](../../src/api/Fms.Api/appsettings.json),
  `appsettings.Development.json`, [docker-compose.yml](../../docker-compose.yml),
  [config/.env.example](../../config/.env.example), and the `UseSetting` calls in the three
  integration test classes.
- Dockerfile → `src/api/Dockerfile` (template form: `COPY . .`, no hand-maintained csproj list);
  update [.gitlab-ci.yml](../../.gitlab-ci.yml) (`--file src/api/Dockerfile`, context `src/api`) and
  [docker-compose.yml](../../docker-compose.yml) (`context: src/api`).
- Drop now-redundant packages: `Microsoft.Extensions.Configuration.Abstractions` +
  `DependencyInjection.Abstractions` from `Fms.Service`; `DependencyInjection.Abstractions` from
  `Fms.Repository` (EF Core brings DI transitively, as in the template).

## Acceptance criteria

- [ ] `Constant.ConfigKey` names every key; no config key is spelled as a literal in two places
- [ ] `appsettings.json` declares every key with an empty default (an empty value is a defined state)
- [ ] `AddEntraAuthentication` has three branches: `TestSigningKey` → RS256 test scheme;
      `TenantId` + `Audience` → Entra; neither → no scheme **and a startup warning naming both keys**
- [ ] `AllowCORS` splits `AllowedOrigins` on `,`, trims, uses `Constant.App.CORSPolicyName`, and
      **throws at startup naming the setting** when empty
- [ ] `IUserRepository` and `ICatalogService` resolve by interface from the container
- [ ] `docker build --file src/api/Dockerfile src/api` succeeds; `docker compose up --build` boots
- [ ] No credentials in `appsettings*.json` or `Properties/launchSettings.json`
- [ ] `Database.Migrate()` is **still present** in `Program.cs` — see the trap below
- [ ] `dotnet build` warning-free; `dotnet test` green (all three test projects)

## Tests (TDD)

**RED first** — `Fms.Api.Test/StartupWiringTests.cs` (in the template's spirit):
- the host builds and `IUserRepository` / `ICatalogService` resolve by interface
- startup logs a warning when `TenantId`/`Audience` are absent
- `AllowCORS` throws with a message naming `AllowedOrigins` when it is empty

These fail against today's `Program.cs` (no `ServiceExt`, nested keys) and pass when the stage is done.

## Traps

- ⚠️ **`Database.Migrate()` must stay in `Program.cs` during this stage.** Three integration test
  classes ([AuthIntegrationTests](../../src/api/Fms.Api.Test/AuthIntegrationTests.cs),
  [SpaceFormCrudTests](../../src/api/Fms.Api.Test/SpaceFormCrudTests.cs),
  [SubmissionApiTests](../../src/api/Fms.Api.Test/SubmissionApiTests.cs)) rely on it to build their
  Testcontainers schema and assert `/health` in `InitializeAsync`. Removing it here turns the suite
  red. It is removed in [07](07-drop-startup-migration.md), which also gives those classes their own
  migration step.
- ⚠️ The three classes set `UseSetting("ConnectionStrings:Postgres", …)`, `Entra:ClientId`,
  `Entra:Issuer`, `Entra:TestSigningKey`. All must be renamed to the flat keys **in this stage**, or
  the suite breaks even though the app builds.
- ⚠️ `AllowCORS` throwing on empty is only safe because `appsettings.Development.json` carries an
  `AllowedOrigins` default and `WebApplicationFactory` runs in Development. Confirm the no-DB test
  classes (`HealthEndpointTests`, `CorsEndpointTests`, `SwaggerSpecTests`) still boot — they use a
  bare `WebApplicationFactory<Program>`.
- ⚠️ GitNexus before editing `Program.cs`: `gitnexus impact Program --direction upstream`. Treat
  `risk: UNKNOWN` as unresolved, not as safe.

## Out of scope

- `EntityBase`, soft delete, `AuditLog`, the repository base class — stages [03](03-model-entitybase-and-database-entity.md) and [05](05-persistence-soft-delete-and-audit.md).
- Health-check and OpenAPI changes — stage [06](06-http-surface-health-and-openapi.md). `/health` must keep
  returning its current JSON body through this stage so its tests stay green.
