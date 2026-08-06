# 04 — Database schema + migrations

Status: **Not started** (spec drafted 2026-08-06)

## Summary

EF Core model + migrations for the single PostgreSQL DB: `users`, `spaces`, `forms`, `submissions`, `permissions`. Schemas and submissions stored as `jsonb`.

## Dependencies

- 00-scaffolding.md

## User story

As a developer, I want the DB schema established and versioned so that the domain is consistent across all features.

## Acceptance criteria

- [ ] Tables: `users` (Entra id, email, role), `spaces`, `forms` (belongs to space, `schema jsonb`), `submissions` (belongs to form + user, `data jsonb`), `permissions` (resource_type, resource_id, expression)
- [ ] Migrations run at backend startup (DB created on first boot)
- [ ] Foreign keys: form→space, submission→form, submission→user

## Tasks

- [ ] Entity models + DbContext
- [ ] EF Core migrations
- [ ] Startup migration application
