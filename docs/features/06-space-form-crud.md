# 06 — Space & Form CRUD APIs

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Admin-gated REST APIs for managing spaces and forms (create/read/update/delete/list). Form definitions are standard JSON Schema (draft 2020-12); mutations are admin-only. Mutable single definition (no versions).

## Dependencies

- 04-db-schema.md
- 05-permissions-service.md

## User story

As an admin, I want to create spaces and forms so that forms are organized and configurable.

## Acceptance criteria

- [ ] Space list/create/update/delete (admin only)
- [ ] Form list/create/update/delete under a space (admin only)
- [ ] Form create/update validates the submitted schema as valid JSON Schema (draft 2020-12)
- [ ] GET endpoints for spaces/forms are permission-scoped for non-admins
- [ ] Forms exposed via generated API client

## Tasks

- [ ] Space endpoints
- [ ] Form endpoints + JSON Schema validation
- [ ] Admin role gate
- [ ] Permission-scoped GET queries
