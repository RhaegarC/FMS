# 14 — User portal: dashboard

Status: **Not started** (spec drafted 2026-08-06)

## Summary

User portal for common users: dashboard of granted (visible) forms, fill-and-submit flow, own-submissions list with search + Excel/JSON export, and read-only submission detail.

## Dependencies

- 04-msal-login.md
- 05-permissions-service.md (granted-forms visibility)
- 07-submission-apis.md
- 08-form-renderer-core.md
- 11-readonly-render.md

## User story

As a common user, I want to see only the forms I'm granted, fill them, and manage my own submissions so that I can participate in data collection.

## Acceptance criteria

- [ ] Dashboard lists only forms the caller can access (permission-scoped)
- [ ] Fill a form with the shared renderer; validation enforced; submit persists
- [ ] After submit, the submission appears in "my submissions"
- [ ] Own-submissions list: keyword search + form/date filters
- [ ] Excel + JSON export of own submissions
- [ ] Read-only detail view of a submission
- [ ] Submissions from other users are never visible

## Tasks

- [ ] Granted-form list (permission-scoped API call)
- [ ] Fill/submit flow
- [ ] Own-submissions list + search + filters + export
- [ ] Read-only detail view
