# 01 — Foundation

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — System overview + Deployment (stage 1) + Decisions #7/#8/#9/#22.

## Summary

Verify the layered backend solution and its test projects build green, stand up the stage-1
`docker-compose` (`postgres`, `backend`, `web-app`), and provide the config plumbing (`.env`,
CORS, EF migrations at startup, Swagger/OpenAPI) that every later feature builds on.

## Story

As a developer I want the three services running locally on separate ports with a bootstrapped
database, so that each feature lands against a working environment and is verifiable end-to-end.

## Dependencies

- none

## Acceptance criteria

- [ ] `dotnet test` runs green from `src/api/` (all layers: `Fms.Repository.Test`, `Fms.Service.Test`, `Fms.Api.Test`)
- [ ] `docker-compose` starts `postgres`, `backend`, and `web-app` (placeholder page until the Figma-exported app lands) on separate local ports
- [ ] Backend runs EF migrations at startup; the DB is created on first boot
- [ ] Swagger/OpenAPI is reachable (served at root — bug 01) and CORS is configured from `.env`
- [ ] `.env` keys documented: connection string, Entra tenant/clientId, `ADMIN_USER_IDS`, data-source URL policy, CORS, `VITE_*` SPA auth vars

## Tests (TDD)

- Integration (`Fms.Api.Test`): boot the API host; a `/health`-style endpoint returns 200.
- No behavioural logic yet — this feature is infrastructure + build verification.

## Notes / non-goals

- Real screens are Figma-authored; `web-app` is a placeholder until then.
- Entra token validation itself is feature 02.
