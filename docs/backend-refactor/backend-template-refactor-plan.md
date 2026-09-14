# Port the FMS backend onto the `Rg.Backend.Api` template

**Status:** in progress. This file replaces an earlier plan — see [Why this is a port and not a
refactor](#why-this-is-a-port-and-not-a-refactor) — and it is the single record of the work, so
it is the file to keep true. The eight per-stage files it replaces have been deleted rather than
left to describe a plan nobody is following.

## Why this is a port and not a refactor

The first version of this plan refactored `src/api` **in place** onto the template's design, in
eight stages. That is not what is happening, and the difference is not cosmetic: it changes what
gets built, what gets tested, and what a reviewer should expect to see.

What changed: a proper solution template is installed — the NuGet package **`Rg.Backend.Api`**
(`dotnet new BackendSolution`), whose source repo is a separate clone (not part of this repository),
shipping its rules as `src/content/STANDARD.md`. FMS's `src/api` was built against `docs/backend-standard.md`,
an org-wide standard it is described as *reference-implementing*. The two disagree on real
behaviour, not just style — keys, delete semantics, audit, error contract, config keys, test-project
split — so the codebase documents one standard and implements another. Removing that divergence is
the point of this work.

Refactoring `src/api` in place would mean editing the deployed backend while it is the only backend,
and every stage would have to keep a partially-migrated system green. Generating a fresh solution
from the template and moving the domain onto it instead means **the template is correct by
construction** and the work is confined to the domain, which is the only part the template has no
opinion about.

The approach is **hybrid**: `src/api-new` is generated from the template as the build target, and
the domain and its tests are **ported** from `src/api` rather than re-derived from the feature
acceptance criteria. Re-deriving them would be a rewrite, and would put behaviour that is already
proven and already deployed back through first principles — the ports are checkable, the rewrite
would not be.

Two things this plan's predecessor assumed wrongly, recorded so they are not re-assumed:

- **`docs/features/archive/` is not the backend.** It holds only `01-foundation.md` and
  `02-entra-auth.md`. Features 03–07 are marked "Not started" in `docs/features/` yet are
  substantially implemented in `src/api` — 2,355 non-test lines and 2,151 test lines. The inventory
  for the port therefore comes from **all** of `docs/features/`, read against the code, not from the
  archive.
- **Feature 08 (remote-lookup proxy) is genuinely not implemented.** It is new behaviour, not a
  port, and is out of scope here.

## Target structure

`src/api-new`, generated from the template. Nine projects: the template's six tiers, its test
per tier, plus `Fms.TestSupport`.

```
src/api-new/
  Fms.slnx
  STANDARD.md                     <- the template's rules, with FMS's deviations recorded (see below)
  Dockerfile                      <- build context = src/api-new
  Fms.Api/                        Program.cs (composition root), ServiceExt.cs, UserContextService.cs,
                                  Controllers/
  Fms.Interface/                  Repository/  Service/  Infrastructure/
  Fms.Model/                      Constant.cs, NotAuditedAttribute.cs, DatabaseEntity/
  Fms.Repository/                 FmsDbContext.cs, DatabaseRepository.cs, PersistenceExtensions.cs,
                                  AuditSaveChangesInterceptor.cs, *Repository.cs
  Fms.Service/                    *Service.cs, PermissionExpression.cs, PermissionEvaluator.cs
  Fms.TestSupport/                AuditHarness, FakeUserContext, StubUserRepository
```

## Decisions locked

| Question | Decision |
|---|---|
| Project names | **Keep `Fms.*`** — adopt the template's structure and design, not its naming |
| Build target | **`src/api-new`**, generated from the template; `src/api` is untouched while it is deployed |
| Entity / key / audit | **Full adoption** — `EntityBase`, application-assigned keys, `IsDeleted` soft delete, `AuditLog` |
| Domain code | **Port, don't re-derive** — the tests come across with the code |
| Test projects | **One per tier** (`Fms.Api.Test`, `Fms.Repository.Test`, `Fms.Service.Test`) plus `Fms.TestSupport` |
| Staging | **Per domain area**, each its own branch and PR into `develop` |
| OpenAPI, config keys, health, identity seam, Dockerfile, soft delete | **Adopt** |

### Which standard governs — unresolved

FMS now has two, and they disagree. `docs/backend-standard.md` is the org-wide standard (the
`.claude/CLAUDE.md` link, described there as applying beyond FMS), and it is what the deployed
`src/api` follows — including the `gen_random_uuid()` default that decisions 24 and 25 remove.
`src/api-new/STANDARD.md` is the template's, which is what the port follows.

Both are currently true statements about different code. That is tolerable only while there are two
backends. Deciding which one governs FMS once the port lands — and either retiring
`docs/backend-standard.md` here or reconciling it — is part of the cutover decision below, not
something to settle by editing a link.

## Deviations from the template

The cases where the template's design does not fit FMS. Each needs a code change *and* a written
rule in `STANDARD.md`, because the next person will otherwise "simplify" it away.

This list is load-bearing, not bookkeeping: checking the retired plan's copy of it against what had
actually shipped found a live defect — the interceptor was leaving `lastModifiedOn` unset on insert,
which writes year 1 rather than failing (item 9). Keeping the list current is what made that visible.

| # | Deviation | Status |
|---|---|---|
| 1 | `IDbRepository` gains `CancellationToken` — FMS threads cancellation through every repository call; the template's generic methods take none | pending |
| 2 | `DatabaseRepository` exposes `protected Context` — `SubmissionRepository` needs `.Include(...)`, composed filters and a parameterized `data::text ILIKE` base that no `Expression<Func<T,bool>>` can express | pending |
| 3 | `uuid` columns retained, but **application-assigned**: `EntityBase.Id` generates the value, and the `gen_random_uuid()` / `now()` defaults are gone so the interceptor stays the single source of truth | **applied** |
| 4 | Auth keeps a test escape hatch — `Entra:TestSigningKey` + `Issuer` (self-issued RS256) so real token validation is testable with no tenant. `AddEntraAuthentication` gains a third branch | pending |
| 5 | `ADMIN_USER_IDS` retained, resolved at the composition root and passed *down* as a value, so the Service layer never reads configuration | pending |
| 6 | Provisioning stays in middleware — `AdminOnly` and every `FmsApiControllerBase.CurrentUser` read the provisioned row from `HttpContext.Items`, so the template's provision-on-`/me` timing is not enough | pending |
| 7 | **Errors — open.** FMS returns `ApiError{message}`; the template ships `AddProblemDetails()`/RFC 9457. The port *inverts the default*: adopting the template is now the cheap path and keeping `ApiError` is the deliberate one. The frontend consumes the current shape, so this is a contract decision, not a style one | **open** |
| 8 | One test project per tier — plus `Fms.TestSupport`, a **library, not a tier**: a double needed by two tiers would otherwise be written twice and drift. `IsTestProject` is `false` or solution-level `dotnet test` fails on it | **applied** |
| 9 | The interceptor stamps `LastModifiedOn`/`LastModifiedBy` **on insert too**. The template's `Apply` leaves them to the `Added` case; with no `now()` default (deviation 3) and a non-nullable property, the insert succeeds with `default(DateTimeOffset)` — year 1 — which Postgres accepts | **applied** |
| 10 | The interceptor stays `internal` with `InternalsVisibleTo` for the test project, so tests exercise the real redaction rules rather than a reimplementation | **applied** |
| 11 | `users.id` **is** the Entra object id — no `entraObjectId` column. Entra object ids are GUIDs, so the key still satisfies the `uuid` contract; the token's `sub` claim is not accepted as a fallback because it is pairwise and has no such guarantee | **applied** |
| 12 | Model shape is pinned without a database — `ModelShapeTests` reads the EF metadata, because every decision above fails *silently* | **applied** |
| 13 | **`AsNoTracking` on the shared read — open.** The ported `ListAllAsync` read untracked; `IDbRepository.GetListAsync` tracks. Permission rows are read on every authorised request, evaluated and discarded. The fix belongs in the shared read, not a permission-specific override | **open** |

## Picking this up on another machine

Everything needed to continue is in this repository on `develop`. Three prerequisites are not,
because none of them belongs in version control:

1. **Bootstrap the GitNexus index.** `.gitnexus/` is gitignored, so a fresh clone has neither the
   index nor `run.cjs`, and the per-commit `impact` / `detect-changes` discipline below cannot run
   without it. Bootstrap once with `npx`, `bunx` or `pnpm dlx` — e.g. `bunx gitnexus@latest analyze`
   (npm 11's `npx` crashes). Cheap to redo: a full index of this repo takes about five seconds.
2. **Docker, for the database-backed tests.** Not a local Postgres: the tests bring their own up via
   `Testcontainers.PostgreSql`. That package is already referenced by `src/api` and needs adding to
   `src/api-new`, which is part of the infrastructure slice.
3. **No template checkout is required.** The port does not regenerate anything — `src/api-new` is
   generated and committed, and later migrations are produced by `dotnet ef` inside this repo. The
   template repository is only ever a *reference* for how a mechanism was written, never a build
   input, so a machine without it can do all of the remaining work.

`dotnet ef` (10.0.11) and `Microsoft.EntityFrameworkCore.Design` are already installed/ referenced,
so the migration slice needs no setup.

## Slices

Each slice is its own branch → PR into `develop`, green on its own (`dotnet build` warning-free,
`dotnet test` passing), and ported test-first. This is a ~5,000-line backend; one PR would be
unreviewable and one bisect useless.

| Slice | Feature | Scope | Status |
|---|---|---|---|
| Foundation | — | Model on `EntityBase`, `FmsDbContext`, identity seam, `Fms.TestSupport`, `ModelShapeTests` | **PR #5** |
| Permissions | 03 | `PermissionExpression`, `PermissionEvaluator`, `IPermissionEvaluator`, `IPermissionRepository`, `PermissionRepository` | **PR #6** |
| Catalog | 04 | `CatalogService` (spaces + forms CRUD), `SpaceController`, `FormController`, `DtoMapper`, `FmsApiControllerBase`, `JsonSchemaValidator` | pending |
| Submissions | 05, 06 | `SubmissionService`, submit validation, list/detail, `SubmissionController` | pending |
| Export | 07 | `SubmissionExcelExporter`, `SubmissionFlattener` | pending |
| Session & HTTP surface | 02 | `AdminOnly` policy, provisioning middleware, `Entra:TestSigningKey`, plus the CORS / health / OpenAPI / Swagger tests. Also replaces the template's scaffolded `UserController`: the deployed contract is **`GET /api/me` returning a `MeResponse`**, while the port currently serves `GET /User/me` returning the raw entity — which leaks `IsDeleted` and the audit columns into the response body | pending |
| Schema & migration | — | `Migrations/InitialCreate` for `src/api-new` (it has none, so it cannot yet create its schema), and `DbSchemaTests` reworked onto the new model | pending |
| Cutover | — | Retire `src/api`, or keep it as a maintenance fork | **open — deferred to parity** |

The foundation slice precedes permissions because every later slice needs the model; permissions
cannot simply come first.

## Cutover

Deliberately undecided, and deferred to parity rather than settled now. The two options are deleting
`src/api` once `src/api-new` is feature-complete and verified, or keeping it as a maintenance fork
while the frontend and deployment move over. The decision needs the parity point to exist before it
can be made honestly; recording it here so it is a decision rather than a drift.

## Verification

Per slice, from `src/api-new/`:

- `dotnet build` — warning-free. Nullable is enabled; fix warnings, don't suppress them.
- `dotnet test` — all projects green, and **check the exit code**, not just the "Passed!" lines: a
  solution-level `dotnet test` reports a non-zero exit when any project fails to host tests even if
  every test passed (see deviation 8).
- **GitNexus before each commit.** `impact <symbol> --direction upstream` before editing a symbol,
  reporting `risk`; `detect-changes --scope all` before committing. Two traps, both hit in practice:
  `partial`/`truncated` is not a clean check, and an untouched-looking result is often *unseen* —
  refresh the index (`analyze --index-only`) when the tree has moved, and **stage new files** before
  running `detect-changes`, since untracked files are absent from the diff it reads.
- `risk: UNKNOWN` is unresolved, not low. `impact` cannot resolve a caller whose receiver is a
  top-level-statement builder — `RegistService` from `Program.cs` is one — so confirm those by text
  search rather than treating the empty caller set as an all-clear.

End-to-end, once the slices are in:
1. `dotnet run --project Fms.Api` with Postgres up → apply the migration via `dotnet ef database update`
   → `GET /health` returns `Healthy`, including with no DB configured.
2. `GET /api/me` with a token → a `users` row is provisioned whose **id is the token's object id**,
   and `auditLogs` gains an `Added` entry whose `EntityId` matches it — the payoff of
   application-assigned keys.
3. `POST /api/spaces` as admin → `GET /api/spaces` finds it → `DELETE /api/spaces/{id}` → the space,
   its forms and their submissions all vanish from listings (cascade), each leaving an audit row.
4. `GET /openapi/v1.json` (Development) lists the controllers; the Dockerfile builds with context
   `src/api-new`.

## Out of scope

- **Feature 08 (remote-lookup proxy)** — not implemented in `src/api`; new behaviour, not a port.
- The frontend (`src/web`) — Figma-Make generated, not test-first, unaffected beyond the PRD wording.
- Some of the template's own documented gaps are inherited rather than fixed, and now appear in
  `src/api-new/STANDARD.md` §12 rather than the template's: `DeleteAsync` hardcodes
  `LastModifiedBy = "sys"`, issues one `FindAsync` per id, and wraps the batch in no transaction.
  What FMS *does* fix by construction is the audit columns themselves — the interceptor maintains
  them on every save, so no repository method has to remember to.
