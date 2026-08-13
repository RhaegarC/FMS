# 03 — Entra ID authentication (backend)

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Backend authenticates via Microsoft Entra ID: validate bearer tokens, auto-provision a `users` row on first login, and seed admin users from `ADMIN_USER_IDS`. No credentials stored in the DB.

## Dependencies

- 01-openapi-contract.md
- 02-db-schema.md (users table)

## User story

As a user, I want to sign in with my organizational account so that I don't manage another set of credentials.

## Acceptance criteria

- [ ] Entra app registration(s) exist with `http://localhost:<port>` redirect URIs
- [ ] Backend validates Entra-issued JWTs (issuer, audience, signing keys)
- [ ] First authenticated request creates the `users` row with role `user`
- [ ] Users listed in `ADMIN_USER_IDS` are provisioned/seeded with role `admin`
- [ ] No passwords stored anywhere

## Tasks

- [ ] Entra app registration(s) + redirect URIs (operational, one-time)
- [ ] JWT bearer authentication middleware (Entra authority/audience)
- [ ] First-login user provisioning service
- [ ] `ADMIN_USER_IDS` seeding
