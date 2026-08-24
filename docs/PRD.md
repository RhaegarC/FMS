# PRD — Dynamic Form Configuration & Rendering Platform

Status: **Draft** (captured from requirements-grilling session, 2026-08-06)
Owner: TBD

## Vision

A web platform where admins configure forms as standard JSON Schema and common users
fill them out and manage their own submissions — with space/dataset-level access control.

## Stakeholders

- **Admin** — configures form definitions, manages spaces/forms, views all submissions across all spaces.
- **Common user** — views the forms they are granted access to, fills and submits them, and manages their own submissions.

## System overview

| Component | Tech | Responsibility |
|---|---|---|
| Admin portal | React (Vite), pnpm | Form config (JSON editor + live preview); view all submissions (search + export) |
| User portal | React (Vite), pnpm | Dashboard of own submissions (search + export); fill & submit forms; read-only submission detail |
| Backend | ASP.NET Core 10 | REST + OAuth 2.0 bearer + JSON; forms/submissions CRUD; remote-lookup proxy; authn/authz |
| Database | PostgreSQL | One DB; `jsonb` for form schemas and submissions |

Deployment (stage 1): all four services in **local Docker**, separate published ports, no reverse proxy.

## Decisions log

Every requirement decision from the grilling session, in order:

| # | Decision | Resolution |
|---|---|---|
| 1 | Configuration model | **Schema-driven**: forms defined as JSON schema stored in DB |
| 2 | System scope | Two portals (admin config + user submit), one backend, one DB |
| 3 | Frontend structure | Two separate React apps |
| 4 | Code sharing | Monorepo (pnpm workspace): `apps/admin` + `apps/user` + shared packages |
| 5 | Authentication | **Microsoft Entra ID** (OIDC); no credentials stored in DB |
| 6 | Role assignment | Auto-provision user on first login; admin via `ADMIN_USER_IDS` seed; no user-management UI |
| 7 | Backend stack | ASP.NET Core 10 (latest), containerized |
| 8 | Database | PostgreSQL with `jsonb` |
| 9 | Frontend↔backend contract | REST + OAuth 2.0 + JSON; **OpenAPI codegen** (Swashbuckle → `packages/api-client`) |
| 10 | Schema format | **Standard JSON Schema only** (draft 2020-12); layout inferred by renderer |
| 11 | Conditional/dynamic fields | **In first milestone**: `if`/`then`/`else`, reactive renderer |
| 12 | Versioning | **Mutable single definition** — no versions |
| 13 | Admin authoring | JSON editor + **live preview** using the shared renderer |
| 14 | Field types | Scalar set + remote-lookup dropdown (see below) |
| 15 | Remote lookup | Custom `x-dataSource` keyword; **backend proxy**; normalized `[{value,label}]` |
| 16 | Cascading | `{fieldName}` placeholders in data-source URL; refetch on change |
| 17 | Submission visibility | Stored as `jsonb`; admin sees all; user sees own only |
| 18 | Export | **Excel (.xlsx) + JSON** |
| 19 | User portal flow | Form list → fill → submit → dashboard; read-only submission detail |
| 20 | Permission model | Single `permissions` table with `expression` column; SQL-editable; no web UI |
| 21 | Permission attributes | `user.id`, `user.email`, `user.role` only — no Entra group claims |
| 22 | Deployment | All apps + DB in local Docker (stage 1), separate ports |

## Data model

Hierarchy: `Space → Form (a.k.a. Dataset) → Submission`

| Entity | Table | Key fields |
|---|---|---|
| User | `users` | `id` (PK), `entra_object_id` (unique), `email`, `name`, `role` (`admin` \| `user`) |
| Space | `spaces` | `id` (PK), `name` |
| Form (Dataset) | `forms` | `id` (PK), `space_id` (FK → spaces), `name`, `schema` (jsonb), `updated_at` |
| Submission | `submissions` | `id` (PK), `form_id` (FK → forms), `user_id` (FK → users), `data` (jsonb), `created_at` |
| Permission | `permissions` | `id` (PK), `resource_type` (`space` \| `form`), `resource_id` (`*` or the target's `id` as text), `expression` (text) |

**Access semantics**: a space grant = access to all forms under that space (e.g. `spaceA.*`); a form grant = that single form; effective access = union of space + form grants. Default deny.

**Schema-change discipline**: when a form schema evolves, update this table (and any affected feature file) in the same PR.

## Form schema model

- Standard JSON Schema, draft 2020-12.
- Renderer infers layout from schema (field order = schema order; widget chosen by type).
- Conditional fields expressed via `if`/`then`/`else`; renderer reacts to value changes.
- Field types (MVP): `string` (text), `string`+`enum` (select/radio), `string`+`format:textarea`, `string`+`format:date` (date picker), `number`/`integer`, `boolean` (checkbox/switch).
- Remote-lookup dropdown — custom keyword:
  ```json
  {
    "type": "string",
    "x-dataSource": {
      "url": "https://api.example.com/cities?country={country}",
      "valueField": "id",
      "labelField": "name"
    }
  }
  ```
  - Renderer asks the **backend proxy**; backend fetches server-side, maps `valueField`/`labelField`, returns normalized `[{value, label}]`.
  - `{fieldName}` placeholders enable cascading; refetch when a referenced field changes.
  - Renderer ordering rule: conditions compute visibility first; cascades fire for visible fields second.

## Authentication & authorization

- **Authentication**: Entra ID (OIDC). Both React apps use MSAL; backend validates bearer tokens.
- **Roles**: `admin` (config + submit) and `user` (submit only). Stored on `users`, provisioned on first login, admin seeded via `ADMIN_USER_IDS` env var.
- **Authorization for data access**: single `permissions` table:
  ```
  id, resource_type ('space' | 'form'), resource_id (uuid | '*'), expression (text)
  ```
  - `expression` is a SQL predicate evaluated against the requesting user's attributes (`user.id`, `user.email`, `user.role`) at request time. Admin-written/trusted input, like a SQL view.
  - Access granted iff a matching permission row's expression is true for the caller. **Default deny.**
  - No web UI — admin edits permission rows directly via SQL.
- Role gates the config capability; expressions gate which spaces/forms a user can view and fill.

## Submissions & export

- Stored as `jsonb`, referencing form + submitting user.
- User portal: dashboard of the user's own submissions, with keyword search over content + form/date filters, and Excel + JSON export.
- Admin portal: submissions view across all users, same search + export.
- Read-only submission detail rendered with the shared renderer in read-only mode.

## Deployment (stage 1)

- `docker-compose` with four services: `postgres`, `backend` (ASP.NET Core 10), `admin-app` (nginx-served React build), `user-app` (nginx-served React build).
- Separate published localhost ports; no reverse proxy yet.
- EF Core migrations run at backend startup (DB created on first boot).
- Entra app registration(s) with `http://localhost:<port>` redirect URIs required (one-time setup).
- Config via `.env`: connection string, Entra tenant/clientId, `ADMIN_USER_IDS`, data-source URL policy, CORS.
- SPA auth config (feature 04): each portal reads `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID` — and optionally `VITE_ENTRA_REDIRECT_URI` / `VITE_ENTRA_SCOPE` — from its own `.env` (see `src/web/apps/*/.env.example`).

## Out of scope / deferred

- File-upload fields
- Array/repeatable fields
- User-management UI
- Draft → publish preview (definitions are mutable single-version)
- Entra group claims
- Reverse proxy / non-local deployment
- Finer-grained (per-form) access control beyond the expression model
