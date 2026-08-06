# 10 — Remote-lookup dropdown

Status: **Not started** (spec drafted 2026-08-06)

## Summary

Custom `x-dataSource` keyword adds a dropdown whose options are fetched from a data-source API via a **backend proxy** (avoids CORS, holds credentials server-side, normalizes to `[{value,label}]`). `{fieldName}` placeholders enable cascading.

## Dependencies

- 08-form-renderer-core.md
- 07-submission-apis.md (or a dedicated lookup endpoint)

## User story

As a form admin, I want dropdowns backed by live data sources so that options stay current and can depend on other answers.

## Acceptance criteria

- [ ] Schema extension: `"x-dataSource": { url, valueField, labelField }` on a `string` field (standard validators ignore it)
- [ ] Backend proxy endpoint: fetches the URL server-side, maps response to normalized `[{value, label}]`
- [ ] Backend restricts fetchable URLs (https only; allowlist optional) — SSRF mitigation
- [ ] `{fieldName}` placeholders substituted with current field values; refetch when a referenced field changes
- [ ] Ordering rule: conditions compute visibility first, cascades fire for visible fields second
- [ ] Renderer widget renders the normalized options

## Tasks

- [ ] Backend proxy endpoint (fetch, normalize, placeholder substitution, URL policy)
- [ ] Renderer lookup widget (fetch, loading, error, refetch-on-change)
- [ ] Cascade ordering integration
