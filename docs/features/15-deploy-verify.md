# 15 — Docker build + end-to-end verification

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Final packaging and verification: Docker builds for all four services, an end-to-end walkthrough (admin config → user fill → submission → admin view → export), and setup docs.

## Dependencies

- All features 00–14

## User story

As a developer, I want a one-command local environment that proves the whole flow works.

## Acceptance criteria

- [ ] `docker compose up` boots all four services
- [ ] E2E flow verified: admin creates space + form → user fills/submits → admin sees + exports submission
- [ ] README documents setup, Entra registration steps, and `.env`
- [ ] Known deferred items documented (file upload, arrays, user management, groups, reverse proxy)

## Tasks

- [ ] Dockerfiles for backend and both apps (nginx-served static builds)
- [ ] Compose wiring + `.env.example`
- [ ] E2E verification script/walkthrough
- [ ] README / setup docs
