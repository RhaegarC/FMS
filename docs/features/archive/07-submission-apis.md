# 07 — Submission APIs + export

Status: **Implemented and merged** via MR #9 (2026-08-24)

## Summary

Backend endpoints for submitting form data, listing/searching submissions (permission-scoped; own vs all), and exporting to Excel (.xlsx) + JSON. Submissions stored as `jsonb` referencing form + user.

## Dependencies

- 06-space-form-crud.md

## User story

As a user, I want to submit forms and retrieve my submissions; as an admin, I want to retrieve all submissions and export them.

## Acceptance criteria

- [ ] Submit endpoint validates payload against the form's schema before storing
- [ ] Submission stored with form id + submitting user id
- [ ] User-scoped list returns only the caller's submissions on granted forms
- [ ] Admin-scoped list returns all submissions
- [ ] Keyword search over submission content + filters (form, date range)
- [ ] Export endpoints return Excel (.xlsx) and JSON
- [ ] Non-granted users cannot submit to or read a form's submissions (deny)

## Tasks

- [ ] Submit endpoint + schema validation
- [ ] List/search endpoints (own vs all, permission-scoped)
- [ ] Export: Excel writer (ClosedXML/EPPlus) + JSON serializer with nested-data flattening for Excel
- [ ] Tests for the access-control boundary
