# 01 — Foundation

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — System overview + Deployment (stage 1) + Decisions #7/#8/#9/#22.

## Summary

Verify the layered backend solution and its test projects build green, stand up the stage-1
`docker-compose` (the `backend`; Postgres runs as its own container and a single `web-app` is
deferred to the frontend-integration track), and provide the config plumbing (`.env`,
CORS, EF migrations at startup, Swagger/OpenAPI) that every later feature builds on.

## Story

As a developer I want the services running locally on separate ports with a bootstrapped
database, so that each feature lands against a working environment and is verifiable end-to-end.

## Dependencies

- none

## Acceptance criteria

- [ ] `dotnet test` runs green from `src/api/` (all layers: `Fms.Repository.Test`, `Fms.Service.Test`, `Fms.Api.Test`)
- [ ] `docker-compose` starts the `backend` (Postgres is a standalone container outside compose), with no dead references to the old two-app Dockerfiles; a `web-app` service joins at frontend integration
- [ ] Backend applies EF migrations at startup against the configured standalone Postgres
- [ ] Swagger/OpenAPI is reachable (served at root — bug 01) and CORS is configured from `.env`
- [ ] `.env` keys documented: connection string, Entra tenant/clientId, `ADMIN_USER_IDS`, data-source URL policy, CORS (the `VITE_*` SPA keys apply once the web app is wired, at frontend integration)
- [ ] Data model follows [docs/backend-standard.md §6.1.1](../backend-standard.md): camelCase column names (no underscores); every data table has a `uuid` `id` primary key (a `string` in .NET/JSON, `gen_random_uuid()` DB default) plus the four audit columns `createdBy`, `createdOn`, `lastModifiedBy`, `lastModifiedOn`

## Tests (TDD)

- Integration (`Fms.Api.Test`): boot the API host; a `/health`-style endpoint returns 200.
- No behavioural logic yet — this feature is infrastructure + build verification.

## Notes / non-goals

- Real screens are Figma-authored (`src/web` is the Figma Make export); the `web-app` compose service is added at frontend integration.
- Entra token validation itself is feature 02.
