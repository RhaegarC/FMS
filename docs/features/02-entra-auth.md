# 02 — Entra Auth

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Decisions #5/#6 + Authentication & authorization (backend side) + Deployment env.

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

- [ ] Protected endpoints reject requests with a missing, invalid, or non-Entra bearer token (401)
- [ ] Valid Entra token → caller identity resolved; first-ever login auto-provisions `users` (unique `entraObjectId`, `email`, `name`, default `role = 'user'`)
- [ ] First login of an email listed in `ADMIN_USER_IDS` seeds/provisions that user as `admin`
- [ ] A `whoami`/`me` endpoint returns the caller's `id`, `email`, and `role`
- [ ] No credentials are stored in the DB (Entra is the only identity source)

## Tests (TDD)

- Unit (`Fms.Service.Test`) **hot spot (security)**: token → claims mapping; invalid/expired/missing-audience → rejected.
- Integration (`Fms.Api.Test`): provision-on-first-login; `ADMIN_USER_IDS` admin seeding; role echoed by `whoami`.

## Notes / non-goals

- MSAL login flow + route gating are frontend (Figma + API glue), verified manually.
- Entra app registration + redirect URI are a one-time ops step (documented in 01 / PRD deployment).
- Role changes after provisioning are out of scope (no user-management UI).
