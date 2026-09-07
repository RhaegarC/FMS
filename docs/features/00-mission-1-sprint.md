# Mission 1 — Dynamic Form Configuration & Rendering Platform

Status: **Planning** — derived from [docs/PRD.md](../PRD.md) on 2026-09-07. The PRD stays the
living reference (decisions log, canonical data model, schema model, permission semantics).

## Goal

A web platform where admins configure forms as standard JSON Schema and common users fill them
out and manage their own submissions — with space/dataset-level access control.

## How to read these features (working model)

- **Backend** is implemented test-first (RED → GREEN → refactor; tiers in
  [docs/testing-and-tdd.md](../testing-and-tdd.md)). It lives in `src/api/` as a layered
  solution — `Fms.Model`, `Fms.Repository`, `Fms.Service`, `Fms.Interface`, `Fms.Api` — each
  layer with a sibling `*.Test` xUnit project. Test command: `dotnet test` (from `src/api/`).
- **Frontend** is authored in Figma Make and exported into `src/web/` as a **single role-gated
  React app** (admin vs user is a role, not a separate app). It is not test-first. The only
  hand-written frontend work is integrating the exported app with the backend API.
  Consequently each feature below specs the **backend/API slice** the exported UI calls and its
  acceptance criteria — it does not spec screens, widgets, or renderer behavior (Figma-authored).
- A feature that changes the data model updates the PRD data-model table in the same PR
  ([schema-change discipline](../PRD.md#data-model)).
- Status reflects the doc lifecycle (file created → in progress → archived after PR to
  `develop`). Backend code already exists under `src/api/` from the pre-redesign build; reusing
  it is expected, and a feature is done once its API slice is verified by tests and merged.

## Feature breakdown

Number = priority (lowest first = next to implement); file = `docs/features/NN-name.md`.

| # | Feature (file) | Depends on | Summary — the backend/API slice | Status |
|---|---|---|---|---|
| 01 | foundation | — | Verify the layered solution + `*.Test` projects build green; `docker-compose` (backend; Postgres standalone outside compose; `web-app` at frontend integration); EF migrations at startup; OpenAPI/Swagger + CORS + `.env` config | archived (PR #131) |
| 02 | entra-auth | 01 | Backend validates Entra ID bearer tokens; users auto-provisioned on first login; admins seeded from `ADMIN_USER_IDS`; `role` stored on `users` | not started |
| 03 | permission-eval | 02 | `permissions` table + request-time expression evaluation over `user.id`/`email`/`role`; union of space + form grants; **default deny**; enforced on every data read | not started |
| 04 | forms-crud | 02 | Admin CRUD for form definitions (space-scoped): `schema` jsonb, draft 2020-12 (incl. `if`/`then`/`else`), mutable single definition; schema validated on save | not started |
| 05 | submission-submit | 03, 04 | User fills & submits an accessible form; **submitted data validated against the schema**; stored as jsonb with form + submitting user | not started |
| 06 | submission-list-detail | 03, 05 | User lists own / admin lists all (permission-scoped) with keyword search + form/date filters; read-only submission detail | not started |
| 07 | export | 06 | Excel (.xlsx) + JSON export of the visible, filtered submission set | not started |
| 08 | remote-lookup-proxy | 02 | Backend proxy for `x-dataSource` URLs: server-side fetch, `{fieldName}` placeholder substitution (cascading), URL allowlist policy, normalized `[{value,label}]` | not started |

## Non-TDD tracks (not feature files)

- **Frontend integration** — the single Figma-exported app wired to the APIs above: MSAL
  login, `api-client` regeneration + calls, route/role gating, wiring responses into the
  exported components. Verified manually (E2E), not test-first.
- **Deployment prerequisites** mostly fold into feature 01.

## Definition of Done (checked by `/sprint-status`)

- [ ] Layered backend solution builds; `dotnet test` green from `src/api/`
- [ ] Entra auth: backend validates bearer; users auto-provisioned; admins seeded via `ADMIN_USER_IDS`
- [ ] Permission expressions enforced (default deny) on all data access
- [ ] Admin can create/update a form schema (draft 2020-12) via the API; schema validated
- [ ] User can submit an accessible form; data validated against the schema on submit
- [ ] User sees own / admin sees all submissions — keyword search, form/date filters, read-only detail
- [ ] Excel (.xlsx) + JSON export of visible results
- [ ] Remote-lookup proxy + cascading placeholders work end-to-end
- [ ] Single role-gated app (Figma-exported) integrated with the backend for the above; E2E verified
- [ ] Each backend feature merged to `develop` with its tests (RED → GREEN)
