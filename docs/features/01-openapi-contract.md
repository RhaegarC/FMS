# 01 — OpenAPI codegen pipeline

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Generate TypeScript API clients for both React apps from the backend's OpenAPI spec (Swashbuckle → `openapi-typescript`), landed in `packages/api-client`. The backend is the single source of truth for DTOs.

## Dependencies

- 00-scaffolding.md

## User story

As a frontend developer, I want typed API clients generated from the backend spec so that frontend and backend DTOs cannot drift.

## Acceptance criteria

- [ ] Swashbuckle emits an OpenAPI spec from the backend
- [ ] `openapi-typescript` generates typed models + client into `packages/api-client`
- [ ] Regeneration is a single command
- [ ] Both apps import the generated client

## Tasks

- [ ] Swashbuckle configuration
- [ ] Codegen script (spec → generated client)
- [ ] Wire generated client into both apps
