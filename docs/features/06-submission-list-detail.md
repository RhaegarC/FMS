# 06 — Submission List & Detail

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Submissions & export + Decisions #17/#18.

## Summary

Read APIs over stored submissions, permission-scoped: a user lists/reads only their own
submissions; an admin lists/reads all. Listing supports keyword search over content plus
form/date filters. Detail returns the full data and the form schema for read-only rendering.

## Story

As a user or admin I want to find and open past submissions, so I can review answers without
ever seeing rows I am not allowed to.

## Dependencies

- [03-permission-eval](03-permission-eval.md) (scoping to own vs. granted forms)
- [05-submission-submit](05-submission-submit.md) (submissions exist)

## Acceptance criteria

- [ ] User list returns only the caller's own submissions; admin list returns all — enforced via permission evaluation
- [ ] Both lists filter by form and by date, and keyword-search over submission content (jsonb)
- [ ] Detail returns the submission `data` + the form schema (input for read-only rendering)
- [ ] Reading a submission the caller cannot see → 403/404 (default deny)

## Tests (TDD)

- Unit (`Fms.Service.Test`): visibility-scoping rules (own vs. granted) as pure logic.
- Integration (`Fms.Repository.Test` + `Fms.Api.Test`): seeded data → correct rows for user vs admin; search/filter behaviour.

## Notes / non-goals

- Dashboard/search UI and read-only detail rendering are Figma-authored.
- Export of these results is feature 07.
