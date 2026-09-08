# 02 — Entra Auth

Status: **Implemented and merged** via PR #1 (2026-09-08) · [00-mission-1-sprint.md](../00-mission-1-sprint.md)
Source: [PRD](../../PRD.md) — Decisions #5/#6 + Authentication & authorization (backend side) + Deployment env.

## Summary

The backend authenticates Entra ID bearer tokens and provisions users: validate OIDC JWTs on
protected endpoints, auto-provision a `users` row on first login (`role = 'user'` by default),
and promote any email listed in `ADMIN_USER_IDS` to `admin`. Expose the caller's identity + role
so the frontend can gate routes.

## Story

As an admin or user I want to sign in with my work account once and have the backend recognise me
on every call, so I never maintain a separate credential and my role is always current.

## Dependencies

- [01-foundation](01-foundation.md) (env + running host)

## Acceptance criteria

- [x] Protected endpoints reject requests with a missing, invalid, or non-Entra bearer token (401)
- [x] Valid Entra token → caller identity resolved; first-ever login auto-provisions `users` (unique `entraObjectId`, `email`, `name`, default `role = 'user'`)
- [x] First login of a user whose Entra object ID is listed in `ADMIN_USER_IDS` seeds/provisions that user as `admin`
- [x] A `whoami`/`me` endpoint returns the caller's `id`, `email`, and `role`
- [x] No credentials are stored in the DB (Entra is the only identity source)

## Tests (TDD)

- Unit (`Fms.Service.Test`) **hot spot (security)**: token → claims mapping; invalid/expired/missing-audience → rejected.
- Integration (`Fms.Api.Test`): provision-on-first-login; `ADMIN_USER_IDS` admin seeding; role echoed by `whoami`.

### Implementation & verification

The auth slice was built under the pre-redesign feature set and already lived in `develop` —
this feature re-verifies it against the ACs above (reuse is expected, per the sprint working
model) and closes the AC-1 test gap.

- Token validation: JwtBearer (Entra OIDC discovery in prod; `Entra:TestSigningKey` escape
  hatch in tests/dev). `MapInboundClaims = false` so the provisioner reads `oid`/`email`/`name`
  directly from the token.
- Provisioning: `UserProvisioner` (Service) → `IUserRepository`; `ADMIN_USER_IDS` holds Entra
  **object IDs** (the `oid` claim — immutable, unlike email). Promotes but never demotes.
- Middleware order: `UseAuthentication` → `UserProvisioningMiddleware` → `UseAuthorization`
  (stores the provisioned `User` in `HttpContext.Items["FmsUser"]` for handlers).
- `api/me` (`MeController`, `[Authorize]`) returns `id`, `name`, `email`, `role`.
- Unique `users.entraObjectId` index and the camelCase schema are verified by `Fms.Repository.Test`.
- Added in this PR: AC-1 rejection tests (`AuthIntegrationTests`) — malformed, expired,
  wrong-audience, and wrong-issuer tokens each return 401.

Test results (from `src/api/`): `dotnet test` green — Service 70, Repository 3, Api 60.

## Notes / non-goals

- MSAL login flow + route gating are frontend (Figma + API glue), verified manually.
- Entra app registration + redirect URI are a one-time ops step (documented in 01 / PRD deployment).
- Role changes after provisioning are out of scope (no user-management UI).
