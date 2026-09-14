# 04 — Identity seam: `IUserContextService` + `UserService`

Status: **Not started** · Parent: [backend-template-refactor-plan.md](backend-template-refactor-plan.md)
Depends on: [03](03-model-entitybase-and-database-entity.md)
Branch: `refactor/04-identity-seam-user-context-service`

## Summary

Adopt the template's identity seam and move configuration out of the Service layer. Also a
**prerequisite for stage 05**: the template's new `AuditSaveChangesInterceptor` distinguishes
`"anonymous"` (an unauthenticated request) from `"system"` (no request at all), which requires
`IUserContextService.HasActiveRequest` to already exist.

## Scope

- **New** `Fms.Interface/Infrastructure/IUserContextService.cs` — `EntraObjectId`,
  `HasActiveRequest`, `ActorName`, `IpAddress`, `UserAgent`, `CorrelationId`; every member get-only.
- **New** `Fms.Api/UserContextService.cs` — reads the `oid` claim, the long-form
  `…/claims/objectidentifier` claim, `name` / `preferred_username`, and the `X-Correlation-Id` header
  (falling back to `TraceIdentifier`).
- Delete `ICurrentUserProvider` and `HttpContextCurrentUserProvider`.
- Replace `IUserProvisioner` / `UserProvisioner` with `IUserService` / `UserService`
  (`GetOrCreateAsync()`), taking the admin-object-id set as a **resolved value**.
- Switch `AuditSaveChangesInterceptor` to consume `IUserContextService.EntraObjectId`.
- Keep `UserProvisioningMiddleware` as a thin Api-layer adapter calling
  `IUserService.GetOrCreateAsync()`.

## Acceptance criteria

- [ ] `IUserContextService` lives in `Fms.Interface/Infrastructure/` (not `Service/` — it describes
      the runtime environment, not a business capability)
- [ ] Its members are computed **on access**, not captured by the constructor — resolving it outside a
      request yields nulls rather than freezing whatever was current when it was built
- [ ] `EntraObjectId` returns null when the principal is not authenticated (`Identity.IsAuthenticated`),
      so an unauthenticated caller is never attributed to a claimed id
- [ ] `UserService.GetOrCreateAsync()` returns **null** when there is no object id — an unidentifiable
      caller must not create a user row
- [ ] Admin promotion from the resolved id set; an existing user is **never demoted**
- [ ] `IConfiguration` no longer appears anywhere in `Fms.Service` (deviation 5)
- [ ] `AdminOnlyHandler` and `FmsApiControllerBase.CurrentUser` behave exactly as before
- [ ] `dotnet build` warning-free; `dotnet test` green

## Tests (TDD)

**RED first** — `Fms.Service.Test/UserServiceTests.cs`, ported from the existing
`UserProvisionerTests` (which is deleted in the same change):

- a first-time caller is provisioned with role `user`
- a caller in the admin id set is provisioned/promoted to `admin`
- an existing admin is **not** demoted on a later non-admin login
- a principal carrying no object id returns null and inserts nothing
- profile fields (`Email`, `Name`) refresh from the principal on each login

Use the template's `TestSupport/FakeUserContext.cs` + `StubUserRepository.cs` pattern — stand in for
the HTTP-backed implementation and set the caller you want to test (authenticated, anonymous, or no
request at all).

## Traps

- ⚠️ **Do not drop `UserProvisioningMiddleware`.** The template provisions only inside
  `MeController`, so only that endpoint has a user — but `AdminOnlyHandler` and every
  `FmsApiControllerBase.CurrentUser` read the provisioned row from `HttpContext.Items`. Keep the
  middleware as a thin adapter around the template's *service and interface* (deviation 6). Pure
  template adoption here silently breaks admin authorization.
- ⚠️ `UserProvisioner` currently injects `IConfiguration` in the Service layer, violating the
  template's §6 ("only `Api` reads configuration"). Resolve the admin ids at the composition root and
  pass the value down; do **not** keep the `IConfiguration` dependency.
- ⚠️ `EntraObjectId` must not be read from an unauthenticated principal. Check
  `Identity.IsAuthenticated` before trusting any claim — the template's `UserContextService` does
  exactly this and the reason is written into its XML docs.

## Out of scope

- Soft delete, the query filter and `AuditLog` writes — stage [05](05-persistence-soft-delete-and-audit.md).
- The `.IgnoreQueryFilters()` guard on the provisioning lookup: it only becomes necessary once stage 05
  introduces the filter, and it is covered there.
