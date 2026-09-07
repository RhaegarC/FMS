# 07 — Export

Status: **Not started** · [00-mission-1-sprint.md](00-mission-1-sprint.md)
Source: [PRD](../PRD.md) — Decision #18 + Submissions & export.

## Summary

Export the same permission-scoped, filtered submission set the list API returns as Excel (.xlsx)
and as JSON. Nested `jsonb` data is flattened for spreadsheet rows.

## Story

As a user or admin I want to download the submissions I can see, so I can analyse them offline
without exposing anyone else's rows.

## Dependencies

- [06-submission-list-detail](06-submission-list-detail.md) (shares its scoping + filters)

## Acceptance criteria

- [ ] Export honours the identical permission scope and filters as the list API (own vs. granted/all)
- [ ] Excel (.xlsx) and JSON exports are produced from the same filtered set
- [ ] Nested `jsonb` values are flattened for the Excel layout
- [ ] An empty/denied result exports an empty-but-valid file (no leak of others' rows)

## Tests (TDD)

- Unit (`Fms.Service.Test`): flattening of nested jsonb → row cells.
- Integration (`Fms.Api.Test`): export returns a parseable file for the visible set; a denied scope yields no others' data.

## Notes / non-goals

- Excel/JSON download buttons are frontend; the file bytes come from this API.
