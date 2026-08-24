# 05 — Permission evaluation service

Status: **Implemented and merged** via MR #7 (2026-08-24)

## Summary

Authorization for data access. Evaluates the `permissions` table's `expression` column (SQL predicate over `user.id`/`email`/`role`) at request time, with wildcard (`*`) resource matching and default deny. Admin edits expressions via SQL — no web UI. **Security boundary — test first.**

## Dependencies

- 02-db-schema.md

## User story

As an admin, I want data access controlled by SQL-editable expressions so that users only see and fill spaces/forms they are granted.

## Acceptance criteria

- [ ] Access to a space granted iff a matching `permissions` row's expression is true (space grant = subtree)
- [ ] Access to a form granted iff a matching form-level or space-level row's expression is true (union)
- [ ] `resource_id = '*'` wildcard supported
- [ ] Default deny when no matching row is true
- [ ] Expression references only `user.id`, `user.email`, `user.role`
- [ ] Unit tests cover allow/deny/wildcard/subtree cases

## Tasks

- [ ] Expression predicate parser/evaluator over user attributes
- [ ] Resource matching incl. wildcards
- [ ] Effective-access computation (union of space + form grants)
- [ ] Unit tests (RED-first)
