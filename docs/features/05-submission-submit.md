# 05 — Submission Submit

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Decisions #17 + Submissions & export + data-model `submissions`.

## Summary

A user submits data for a form they can access. The backend validates the submitted data against
the form's JSON Schema and stores it as `jsonb` linked to the form and the submitting user.
Invalid data is rejected with field-level errors.

## Story

As a user I want to fill an accessible form and submit it, so that my answers are saved against
that form — but only if they satisfy the schema the admin defined.

## Dependencies

- [03-permission-eval](03-permission-eval.md) (the user must hold a grant for the form)
- [04-forms-crud](04-forms-crud.md) (the form and its schema exist)

## Acceptance criteria

- [ ] Only a user with a true grant for the form can submit to it (default deny otherwise → 403)
- [ ] Submitted data is validated against the form schema; invalid → 400 with per-field errors
- [ ] A valid submission is stored: `data` (jsonb), `formId`, `userId`, `createdOn`
- [ ] The response returns the created submission `id` (detail view comes in 06)

## Tests (TDD)

- Unit (`Fms.Service.Test`) **hot spot**: data-vs-schema validation (type, enum, required, format, `if`/`then`/`else`).
- Integration (`Fms.Api.Test`): submit flow — allowed user ok, denied user 403, invalid data 400.

## Notes / non-goals

- Fill-form UI + widget behaviour are Figma-authored.
- Edit/withdraw of an existing submission is not in the PRD (a submission is stored once).
