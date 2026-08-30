# Bug 01 — Swagger page not reachable at `http://localhost:5149`

Status: **Reported** (2026-08-30) — captured, **not yet fixed** (bug-fix workflow applies).

## Triage

| Field | Value |
|---|---|
| Severity | Low — local dev-only DX; production is unaffected (Swagger is dev-only) |
| Component | `Fms.Api` — `Program.cs` Swagger pipeline / root routing |
| Environment | Local dev, `ASPNETCORE_ENVIRONMENT=Development`, `http` launch profile (`http://localhost:5149`) |
| Version | Current `develop` (also reproduces on the `refactor` branch — identical `Program.cs`) |
| Azure DevOps work item | [Bug #1](https://dev.azure.com/rhaegarc/APJ/_workitems/edit/1) |

## Summary

Starting the backend locally and navigating to the base URL `http://localhost:5149`
returns **HTTP 404** instead of the Swagger UI. Swagger does work, but only at the
non-obvious path `http://localhost:5149/swagger`.

## Reproduction

1. Run the backend with the `http` launch profile:
   `dotnet run` (or F5 with the `http` profile) from `src/api/Fms.Api`.
2. Open a browser and navigate to `http://localhost:5149`.
3. **Actual:** HTTP 404 — no page, no redirect.
4. Navigate to `http://localhost:5149/swagger` instead.
5. **Actual:** the Swagger UI renders as expected.

## Expected vs Actual

- **Expected:** `http://localhost:5149` serves the Swagger UI (either directly via
  `UseSwaggerUI` with `RoutePrefix = ""`, or by redirecting `/` to `/swagger`), so a
  developer starting the API lands on the interactive API docs.
- **Actual:** `GET /` has no route and no redirect. `Program.cs` enables
  `UseSwaggerUI()` with Swashbuckle's default `/swagger` route prefix, and no
  controller maps `/`. Only `/swagger` (and `/health`) respond.

## Root cause (suspected)

In [Program.cs](../../src/api/Fms.Api/Program.cs):

- The Swagger UI is enabled dev-only with the **default route prefix**:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.UseSwagger();
      app.UseSwaggerUI();
  }
  ```
- There is no root route and no `/` → `/swagger` redirect. The controllers all map
  under `/api/...` or `/health` (`SpaceController`, `FormController`,
  `SubmissionController`, `MeController`, `HealthController`).

So the base URL is simply unhandled.

## Fix (implemented — PR #128, `fix/swagger-root-404`)

Serve the UI at the root AND pin the spec endpoint to its absolute path:

```csharp
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FMS API v1");
});
```

**Gotcha caught during the fix:** `RoutePrefix = ""` alone is not enough — Swagger UI
then resolves the default relative endpoint `v1/swagger.json` against `/` and fetches
`http://localhost:5149/v1/swagger.json` (404 → "Fetch error" in the UI). The endpoint
must be the absolute `/swagger/v1/swagger.json`, where `UseSwagger()` still serves the
spec.

Regression test `SwaggerUiRootTests.GetRoot_ServesSwaggerUiInDevelopment` asserts:
`GET /` (following the `/` → `/index.html` redirect) returns 200 with "Swagger UI";
`GET /index.js` (the UI's endpoint config) references `/swagger/v1/swagger.json`; and
`GET /swagger/v1/swagger.json` returns 200.

## Close checklist

- [ ] Regression test added (RED) — `GET /` serves Swagger UI in Development.
- [ ] Fix applied (GREEN) — preferred approach above.
- [ ] `dotnet test src/api/Fms.slnx` green.
- [ ] PR to `develop` (`fix:` commit type); merge with approval.
- [ ] Move this file to `docs/bugs/archive/` after merge; close ADO Bug #1.
