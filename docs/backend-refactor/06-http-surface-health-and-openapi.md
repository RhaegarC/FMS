# 06 — HTTP surface: health checks, OpenAPI

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [02](02-foundation-config-and-composition.md)
Branch: `refactor/06-http-surface-health-and-openapi`

## Summary

Replace FMS's controller-based health endpoint and its Swashbuckle/Swagger UI surface with the
template's: `UseHealthChecks` plus the built-in `Microsoft.AspNetCore.OpenApi` document.

## Scope

- Delete `Fms.Api/Controllers/HealthController.cs`; register `app.UseHealthChecks(Constant.App.HealthCheckUrl)`.
- `AddOpenApi()` + `MapOpenApi()` (Development only) → `/openapi/v1.json`. Drop the
  `Swashbuckle.AspNetCore` package.
- Update `HealthEndpointTests` and `SwaggerSpecTests`; delete `SwaggerUiRootTests`.
- Record bug 01's supersession in [docs/bugs/00-bug-log.md](../bugs/00-bug-log.md).
- Update the `Swashbuckle → packages/api-client` line in [docs/PRD.md](../PRD.md) and
  `src/web/src/imports/PRD.md`; check [Fms.Api.http](../../src/api/Fms.Api/Fms.Api.http) for stale requests
  (the template's own §12 flags a `/weatherforecast/` request that no longer exists).

## Acceptance criteria

- [ ] `GET /health` returns `Healthy` and **stays reachable with no database configured** — it is
      liveness, and orchestration checks it before auth or the DB are up
- [ ] `/openapi/v1.json` serves the document in Development only
- [ ] Swagger UI and the root-URL route are gone; the Swashbuckle package is removed
- [ ] `HealthController` and `SwaggerUiRootTests` are deleted
- [ ] `Fms.Api.http` contains no request to a non-existent route
- [ ] `dotnet build` warning-free; `dotnet test` green

## Tests (TDD)

**RED first** — rewrite `SwaggerSpecTests` to fetch `/openapi/v1.json` and assert the controllers are
described; rewrite `HealthEndpointTests` for the plain-text `Healthy` body. Both fail against today's
Swashbuckle surface.

## Traps

- ⚠️ **`/health` leaves the OpenAPI document.** It is no longer a controller action, so the old
  assertion "spec should describe the `/health` path" must be **removed**, not left failing. If any
  consumer still needs health in the spec, that is a design conversation — say so before proceeding.
- ⚠️ **Deleting `SwaggerUiRootTests` loses real regression coverage.** It exists because of bug 01
  (root URL 404'd instead of serving the UI). Its subject no longer exists, so the test cannot be
  preserved — but the bug log must record *why* it went, so this does not read later as someone
  deleting a failing test.
- ⚠️ Developers lose the root-URL interactive UI. Accept this consciously (decided); the replacement is
  the raw document at `/openapi/v1.json`.
- ⚠️ Confirm the frontend is genuinely unaffected before removing Swagger: it calls only
  `GET /api/me`, and **no `packages/api-client` exists** despite the PRD mentioning it. If a codegen
  consumer appears, this decision must be revisited.

## Out of scope

- The error contract — `ExceptionHandlingMiddleware` + `ApiError{message}` are **kept** (decided), not
  replaced with `AddProblemDetails` / RFC 9457.
- Removing `Database.Migrate()` — stage [07](07-drop-startup-migration.md).
