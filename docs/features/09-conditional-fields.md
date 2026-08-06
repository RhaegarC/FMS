# 09 — Conditional fields (dynamic forms)

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Reactive conditional-field engine: JSON Schema `if`/`then`/`else` makes the form shape change as the user fills it (show/hide/require fields). The core of "dynamic form." **Test first** — the interaction with field-order layout is where bugs live.

## Dependencies

- 08-form-renderer-core.md

## User story

As a form admin, I want fields to appear/disappear based on other answers so that forms adapt to the respondent.

## Acceptance criteria

- [ ] `if`/`then`/`else` conditionals evaluated on value change
- [ ] Hidden fields collapse cleanly from layout (no gaps)
- [ ] Validation recomputed for visible fields; hidden fields excluded from required-checks
- [ ] User input on a hidden field is preserved but not submitted... (decide: values of hidden fields are dropped from the payload)
- [ ] Unit tests cover show/hide/require transitions and edge cases

## Tasks

- [ ] Conditional evaluation engine
- [ ] Recompute visible fields + validation on change
- [ ] Hidden-field value handling on submit
- [ ] Unit tests (RED-first)
