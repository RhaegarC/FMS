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
| Web app (single portal) | React (Vite), pnpm | One role-gated app: **admin** — form config (JSON editor + live preview), view all submissions (search + export); **user** — dashboard of own submissions (search + export), fill & submit forms, read-only submission detail |
| Backend | ASP.NET Core 10 | REST + OAuth 2.0 bearer + JSON; forms/submissions CRUD; remote-lookup proxy; authn/authz |
| Database | PostgreSQL | One DB; `jsonb` for form schemas and submissions |

Deployment (stage 1): **local Docker**, separate published ports, no reverse proxy — Postgres runs as its own container; `docker-compose` runs the `backend` now (a `web-app` joins at frontend integration).

## Frontend build

UI screens are authored in **Figma Make**, which exports a React project used from `src/web/`;
Figma Make produces the screens. The only hand-written frontend work is **integrating the exported
app with the backend API** — auth (MSAL), API calls (via the generated `api-client` package), wiring
responses into the exported components, and role gating. The export is a single flat Vite app under
`src/web/` (no monorepo); the integration glue — auth, generated `api-client` — is added inside that
app. Both roles (admin/user) live in this single app.

## Decisions log

Every requirement decision from the grilling session, in order:

| # | Decision | Resolution |
|---|---|---|
| 1 | Configuration model | **Schema-driven**: forms defined as JSON schema stored in DB |
| 2 | System scope | **Single frontend app** (admin config + user submit in one app), one backend, one DB |
| 3 | Frontend structure | **One React app**; admin vs user is a role, not a separate app |
| 4 | Code sharing | **One flat Vite app** — the Figma Make export at `src/web/` is a single package; no monorepo/workspace |
| 5 | Authentication | **Microsoft Entra ID** (OIDC); no credentials stored in DB |
| 6 | Role assignment | Auto-provision user on first login; admin via `ADMIN_USER_IDS` seed; no user-management UI |
| 7 | Backend stack | ASP.NET Core 10 (latest), containerized |
| 8 | Database | PostgreSQL with `jsonb` |
| 9 | Frontend↔backend contract | REST + OAuth 2.0 + JSON; **OpenAPI codegen** (Swashbuckle → client generated into the `src/web` app) |
| 10 | Schema format | **Standard JSON Schema only** (draft 2020-12); layout inferred by renderer |
| 11 | Conditional/dynamic fields | **In first milestone**: `if`/`then`/`else`, reactive renderer |
| 12 | Versioning | **Mutable single definition** — no versions |
| 13 | Admin authoring | JSON editor + **live preview** using the shared renderer |
| 14 | Field types | Scalar set + remote-lookup dropdown (see below) |
| 15 | Remote lookup | Custom `x-dataSource` keyword; **backend proxy**; normalized `[{value,label}]` |
| 16 | Cascading | `{fieldName}` placeholders in data-source URL; refetch on change |
| 17 | Submission visibility | Stored as `jsonb`; admin sees all; user sees own only |
| 18 | Export | **Excel (.xlsx) + JSON** |
| 19 | User flow (single app) | Form list → fill → submit → dashboard; read-only submission detail |
| 20 | Permission model | Single `permissions` table with `expression` column; SQL-editable; no web UI |
| 21 | Permission attributes | `user.id`, `user.email`, `user.role` only — no Entra group claims |
| 22 | Deployment | All services + DB in local Docker (stage 1), separate ports |
| 23 | Frontend authoring | **Figma Make exports the React UI** into `src/web`; the only hand-written frontend code is integrating the exported app with the backend API (auth + `api-client` calls) |

## Data model

Hierarchy: `Space → Form (a.k.a. Dataset) → Submission`

| Entity | Table | Key fields |
|---|---|---|
| User | `users` | `id` (PK, uuid), `entraObjectId` (unique), `email`, `name`, `role` (`admin` \| `user`) |
| Space | `spaces` | `id` (PK, uuid), `name` |
| Form (Dataset) | `forms` | `id` (PK, uuid), `spaceId` (FK → spaces), `name`, `schema` (jsonb), `lastModifiedOn` |
| Submission | `submissions` | `id` (PK, uuid), `formId` (FK → forms), `userId` (FK → users), `data` (jsonb), `createdOn` |
| Permission | `permissions` | `id` (PK, uuid), `resourceType` (`space` \| `form`), `resourceId` (`*` or the target's `id` as text), `expression` (text) |

**Access semantics**: a space grant = access to all forms under that space (e.g. `spaceA.*`); a form grant = that single form; effective access = union of space + form grants. Default deny.

**Column conventions**: column names are camelCase throughout (no underscores); primary and foreign keys are Postgres `uuid` columns surfaced in .NET and JSON as strings, generated via `gen_random_uuid()` — see docs/backend-standard.md §6.1.1.

**Schema-change discipline**: when a form schema evolves, update this table in the same PR.

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

- **Authentication**: Entra ID (OIDC). The single React app uses MSAL; backend validates bearer tokens.
- **Roles**: `admin` (config + submit) and `user` (submit only). Stored on `users`, provisioned on first login, admin seeded via `ADMIN_USER_IDS` env var.
- **Authorization for data access**: single `permissions` table:
  ```
  id, resourceType ('space' | 'form'), resourceId (uuid | '*'), expression (text)
  ```
  - `expression` is a SQL predicate evaluated against the requesting user's attributes (`user.id`, `user.email`, `user.role`) at request time. Admin-written/trusted input, like a SQL view.
  - Access granted iff a matching permission row's expression is true for the caller. **Default deny.**
  - No web UI — admin edits permission rows directly via SQL.
- Role gates the config capability; expressions gate which spaces/forms a user can view and fill.

## Submissions & export

- Stored as `jsonb`, referencing form + submitting user.
- **User**: dashboard of the user's own submissions, with keyword search over content + form/date filters, and Excel + JSON export.
- **Admin**: submissions view across all users, same search + export.
- Read-only submission detail rendered with the shared renderer in read-only mode.

## Deployment (stage 1)

- `docker-compose` runs the `backend` (ASP.NET Core 10); Postgres runs as its own standalone Docker container (not in compose); a single `web-app` (nginx-served React build of the Figma export) is added at frontend integration.
- Separate published localhost ports; no reverse proxy yet.
- EF Core migrations run at backend startup (DB created on first boot).
- Entra app registration with `http://localhost:<port>` redirect URI required (one-time setup).
- Config via `.env`: connection string, Entra tenant/clientId, `ADMIN_USER_IDS`, data-source URL policy, CORS.
- SPA auth config: the app reads `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID` — and optionally `VITE_ENTRA_REDIRECT_URI` / `VITE_ENTRA_SCOPE` — from its `.env`.

## Out of scope / deferred

- File-upload fields
- Array/repeatable fields
- User-management UI
- Draft → publish preview (definitions are mutable single-version)
- Entra group claims
- Reverse proxy / non-local deployment
- Finer-grained (per-form) access control beyond the expression model
