# 00 — Scaffolding (monorepo + docker-compose)

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Stand up the pnpm workspace monorepo (two React/Vite apps + shared packages), an ASP.NET Core 10 WebAPI skeleton, and a local Docker compose stack with all four services. Establish the shape every later feature builds on.

## Dependencies

- None (base feature)

## User story

As a developer, I want a bootable monorepo and Docker stack so that every subsequent feature can be developed and run against a known foundation.

## Acceptance criteria

- [ ] `pnpm-workspace.yaml` defines `apps/*` and `packages/*`
- [ ] `apps/admin` and `apps/user` are Vite + React apps that build
- [ ] `packages/form-renderer`, `packages/api-client`, `packages/ui`, `packages/types` exist
- [ ] ASP.NET Core 10 WebAPI boots with a `/health` endpoint
- [ ] `docker-compose.yml` defines `postgres`, `backend`, `admin-app`, `user-app` on separate published ports
- [ ] `.env` supplies config (DB connection, Entra tenant/clientId, `ADMIN_USER_IDS`, CORS)

## Tasks

- [ ] pnpm workspace + package scaffold
- [ ] Vite + React setup for both apps
- [ ] Backend skeleton: ASP.NET Core 10 WebAPI, EF Core + Npgsql, health endpoint, containerized
- [ ] docker-compose skeleton + `.env`
