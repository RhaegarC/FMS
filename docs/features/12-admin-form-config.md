# 12 — Admin portal: form configuration

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Admin portal form-config UI: form list per space, Monaco JSON editor with schema validation, **live preview** using the shared renderer, and save.

## Dependencies

- 03-msal-login.md
- 06-space-form-crud.md
- 08-form-renderer-core.md

## User story

As an admin, I want to author a form's JSON schema and preview it live so that I can validate the form before users see it.

## Acceptance criteria

- [ ] Form list grouped by space
- [ ] Open a form in the Monaco editor; invalid JSON / invalid schema flagged
- [ ] Live preview pane renders the current schema via the shared renderer
- [ ] Save persists the schema via the CRUD API
- [ ] Create/delete forms; create spaces

## Tasks

- [ ] Form list by space
- [ ] Monaco editor + schema validation
- [ ] Live preview pane
- [ ] Save / create / delete wiring
