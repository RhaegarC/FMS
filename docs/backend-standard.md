# ASP.NET Core Backend Implementation Standard

> This document is the **common standard for ASP.NET Core API backends**. It applies to
> any new or existing API project in this org — not just FMS. The FMS solution
> (`src/api/Fms.slnx`) is the **reference implementation**; when a rule below says "the
> solution does X", look there first.
>
> Rules marked **MUST** are mandatory. Everything else is a strong convention — deviate
> deliberately and document why.

---

## 1. Purpose & Scope

Every backend API follows the same five-project layered structure so that a developer
moving between projects finds the same seams:

- **entry point** (HTTP, auth, cross-cutting) is separated from
- **business logic** (use cases), which is separated from
- **data access** (EF Core), with **models** shared underneath and **interfaces**
  inverting dependencies between the layers.

This structure is what makes the backend testable (business logic has no EF dependency)
and replaceable (storage is behind an interface).

---

## 2. Solution Structure

A solution consists of exactly five application projects (plus tests):

| Project | Layer | Responsibility |
|---|---|---|
| `<Name>.Api` | Presentation / composition root | HTTP entry point. **MVC controllers** with attribute routing (§6.5.1), middleware, **authentication & authorization**, request validation, exception handling, and **DI registration**. |
| `<Name>.Interface` | Abstractions | **Interfaces only** — the contracts for services and repositories. No implementation, no logic. |
| `<Name>.Model` | Domain models | **Data models (entities)** as plain POCOs. The leaf — references nothing. |
| `<Name>.Service` | Business logic | Use cases / orchestration. Depends on interfaces from `Interface`; **never** on `Repository` or EF Core. |
| `<Name>.Repository` | Data access | **EF Core lives here**: `DbContext`, entity mappings, migrations, repository implementations. |

Dependency direction (see §3) forms a clean acyclic graph:

```text
        ┌──────────────┐
        │    .Api      │  entry point + composition root
        └──┬───────┬───┘
           │       │
   ┌───────▼──┐  ┌─▼───────────┐
   │ .Service │  │ .Repository │
   └───────┬──┘  └─┬───────────┘
           │       │
        ┌──▼───────▼──┐
        │ .Interface  │  abstractions
        └──────┬──────┘
               │
        ┌──────▼──────┐
        │   .Model    │  leaf — no dependencies
        └─────────────┘
```

**Reference implementation:** `src/api/` — `Fms.Api`, `Fms.Interface`, `Fms.Model`,
`Fms.Repository`, `Fms.Service` (+ per-layer test projects `Fms.Api.Test`,
`Fms.Service.Test`, `Fms.Repository.Test`).

---

## 3. Dependency & Project-Reference Rules

1. **`Model` references nothing** — it is the leaf of the graph. Entities must stay
   free of EF Core, ASP.NET, and any framework dependency.
2. **`Interface` references `Model` only** (interface signatures use entities).
3. **`Service` references `Interface` + `Model`.** It MUST NOT reference `Repository`
   or any EF Core package.
4. **`Repository` references `Interface` + `Model`.** It MUST NOT reference `Service`
   (avoids circularity).
5. **`Api` references all four** — it is the composition root that wires
   implementations to interfaces.
6. **No circular references, ever.** If a rule here would force one, the abstraction
   belongs in `Interface`.
7. **Where logic lives** — follow the layer's job, not convenience:
   - Business rules / use cases → `Service`
   - Persistence / query details → `Repository`
   - HTTP / middleware / cross-cutting → `Api`
   - A rule that touches "how" (e.g. "load this then transform it") is decided by the
     layer that owns the operation, not the layer that happens to have the data.

---

## 4. Dependency Injection — MUST

> **Rule: DI is required, and every service — and every repository class — is registered
> by interface.**

1. **DI is required.** Application services are never `new`'d by hand; everything
   resolvable is registered in the container and injected via constructors.
2. **Register by interface.** Every service **and every repository class** is
   registered as `interface → implementation`:
   ```csharp
   services.AddScoped<IUserRepository, UserRepository>();
   services.AddScoped<ICatalogService, CatalogService>();
   services.AddSingleton<IPermissionEvaluator, PermissionEvaluator>();
   ```
   A repository is never registered as its concrete type alone — `UserRepository` is
   always registered as `IUserRepository → UserRepository`. Concrete types are
   registered only when they have no meaningful interface.
3. **Registration lives in the composition root (`Api`)** — in `Program.cs` or, when a
   solution grows, per-layer extension methods that keep `Program.cs` small:
   ```csharp
   builder.Services.AddApplication();  // Service layer
   builder.Services.AddPersistence();  // Repository layer (also registers DbContext)
   ```
4. **Lifetime rules:**
   - `Scoped` — anything that touches `DbContext` (repositories) and services that
     depend on scoped services or per-request state.
   - `Singleton` — stateless pure logic (validators, permission/expression evaluators,
     exporters, config-backed helpers).
   - `Transient` — cheap stateless helpers; use sparingly.
   - Never register the same service under conflicting lifetimes.
5. **Constructor injection only.** No service locator, no `static IServiceProvider`,
   no resolving from inside methods unless passed in by the framework (middleware
   `InvokeAsync`, controller action parameters, authorization handlers are all fine).
6. **`AddDbContext` is called only in the `Api` composition root**, with the
   connection string taken from configuration.

---

## 5. Entity Framework Core / ORM — MUST

> **Rule: EF Core is the standard ORM, and it lives inside the `Repository` project
> only.**

1. **EF Core is the ORM** for all relational persistence. There is no raw ADO.NET
   business code outside `Repository` (a repository may use parameterized SQL where EF
   can't express the query — e.g. FMS's jsonb keyword search).
2. **EF Core is confined to `Repository`.** `Api`, `Service`, and `Model` MUST NOT
   reference `Microsoft.EntityFrameworkCore*` packages. Concretely: no `DbContext`
   types, no `DbSet`, no LINQ-to-Entities projections outside `Repository`.
3. **`DbContext` lives in `Repository`**, exposing `DbSet<T>` per aggregate and
   defining mappings with the **Fluent API in `OnModelCreating`** — this keeps `Model`
   entities as pure POCOs with no EF attributes.
4. **Entities live in `Model`** (§6); the `Repository` maps them to tables.
5. **Migrations are generated into and stored in `Repository`** (a `Migrations/`
   folder) — never in `Api`.
6. **`DbContext` is registered in the `Api` composition root** (§4.6).
7. **Repositories wrap `DbContext`** and are `Scoped` (§4.4). Each repository
   implements an interface from `Interface`.

---

## 6. Layer Conventions

### 6.1 Model

- Entities are **POCOs**: public properties, no logic, no framework attributes where
  the Fluent API in `Repository` can express the mapping instead.
- One class per table / aggregate root. Navigation properties model relationships
  (e.g. `User`, `Space`, `Form`, `Submission`, `Permission` in FMS).
- `Model` MUST NOT reference EF Core, ASP.NET, or the other layers.

#### 6.1.1 Audit columns — MUST

> **Rule: every table carries the same four audit columns, and the values are set
> automatically by the Repository layer — never by handlers.**

Every entity implements `IAuditable` and therefore every table has exactly these four
columns:

| Column | Type | Constraint | Meaning |
|---|---|---|---|
| `created_by` | `text` | nullable | Entra object id (AAD GUID) of the creator — the same value stored on the user row |
| `created_on` | `timestamptz` | NOT NULL, default `now()` | When the row was created |
| `last_modified_by` | `text` | nullable | Entra object id (AAD GUID) of the last modifier |
| `last_modified_on` | `timestamptz` | NOT NULL, default `now()` | When the row was last modified |

- **Actor identity** is the Microsoft Entra ID object id (AAD GUID) — the same value
  stored on the user's own row. It is nullable `text`, **no FK**: it identifies an
  external identity provider's subject, not a row in this database.
- **Who sets them** — the **Repository `SaveChanges` interceptor**, the single source of
  truth. It reads the current actor from an `ICurrentUserProvider` (an interface in
  `Interface`, implemented in `Api` from the authenticated request) and stamps:
  `Added` → all four (`created_by ??= actor` so an already-set value wins, and system
  rows without a request actor stay null); `Modified` → `last_modified_*` only.
- **Handlers never set audit columns** — no service or controller assigns `CreatedOn` /
  `LastModifiedOn` / actor values; if a new write path forgets them, the interceptor
  still fills them.
- **DB default `now()`** keeps direct SQL inserts consistent (e.g. data seeding,
  migrations, ops fixes) without a code path.
- Mapping is centralized via a single `ConfigureAudit<TEntity>` helper in the
  `DbContext`'s `OnModelCreating`, applied to every `IAuditable` entity.
- Public API DTOs may expose timestamps under friendlier names — e.g. FMS exposes
  `last_modified_on` as `FormDto.UpdatedAt` and `created_on` as
  `SubmissionDto.CreatedAt` — the API contract is independent of column names.

### 6.2 Interface

- Contains **service interfaces** (`ISomethingService`) and **repository interfaces**
  (`ISomethingRepository`), organized into `Service/` and `Repository/` folders.
- Interface signatures use `Model` entities and/or primitive types. No HTTP types.
- No implementation classes, no logic — contracts only.

### 6.3 Service

- Holds **business logic / use cases**: validation policies, permission/expression
  evaluation, export/formatting, provisioning rules.
- Depends on **repository interfaces** (from `Interface`), never on `DbContext` or EF
  directly.
- Orchestrates repositories; returns results the `Api` can map to responses. Keeps
  `Api` thin.

### 6.4 Repository

- Owns **EF Core data access**: the `DbContext`, mappings, migrations, and the concrete
  repository classes implementing `Interface` contracts.
- **Follows the same DI rule as services** (§4): each repository is registered by its
  interface (`IUserRepository → UserRepository`) and is `Scoped` because it wraps the
  `DbContext`.
- Query composition (filters, includes, keyword search) lives here so `Service`/`Api`
  never write EF queries.
- Prefer parameterized queries; never concatenate user input into SQL.

### 6.5 Api

- **Entry point for every request**: MVC controllers (§6.5.1), middleware,
  authentication & authorization, binding/validation of input, exception handling,
  uniform error responses.
- **Cross-cutting request concerns live here** — e.g. FMS provisions the FMS `users`
  row and resolves permissions in middleware/authorization before handlers run.
- **Thin action methods**: parse input → call a service (or repository for read-only
  endpoints when no use case exists) → map to a response DTO. No business rules inline.
- **Request/response DTOs live in `Api`** — a `Contracts/` folder of records (one file
  per resource area as the API grows). Entities are not exposed as API payloads.

#### 6.5.1 Controllers — the HTTP entry-point standard

> **Rule: HTTP endpoints are implemented as MVC controllers with attribute routing.
> Minimal APIs are not the standard — if one is added, document why.**

- **One controller per resource/aggregate**, named `<Resource>Controller`, in the `Api`
  project's `Controllers/` folder — e.g. `SpaceController`, `SubmissionController` in FMS.
- **`[ApiController]` on every controller**, with a `[Route(...)]` prefix; actions carry
  the HTTP verb attribute plus route segments/constraints:
  ```csharp
  [ApiController]
  [Route("api/spaces")]
  public class SpaceController(ICatalogService catalog) : ControllerBase
  {
      [HttpPut("{id:int}")]
      public async Task<ActionResult<SpaceDto>> Update(int id, [FromBody] UpdateSpaceRequest request) …
  }
  ```
- **Thin actions**: bind input, call exactly one service via a constructor-injected
  interface, map the result to a DTO. No business rules in the controller (§6.5).
- **Authorization is declarative**: `[Authorize]` for any authenticated user,
  `[Authorize(Policy = "…")]` for policy-gated actions (e.g. FMS's `AdminOnly`).
- **Shared per-request access lives on a base controller** (`ControllerBase` subclass),
  not duplicated static helpers — e.g. FMS exposes the provisioned current user as a
  `CurrentUser` property backed by `HttpContext.Items`.
- **Program.cs is the composition root only**: `AddControllers()` + `MapControllers()`,
  no endpoint lambdas. DTOs come from `Contracts/`; controllers return DTOs, never
  entities.

---

## 7. Naming & Structure Conventions

- Projects: `<Name>.Api`, `<Name>.Interface`, `<Name>.Model`, `<Name>.Service`,
  `<Name>.Repository`, `<Name>.Tests`. (FMS uses the `Fms.` prefix.)
- **Namespaces match project names** (`Fms.Api`, `Fms.Service`, …) so a class's origin
  is visible from its namespace.
- **Interfaces are prefixed `I`**; the implementation is the interface name minus the
  `I`, placed in the implementing project:
  ```csharp
  // Fms.Interface / Repository
  public interface IUserRepository { … }

  // Fms.Repository
  public sealed class UserRepository(FmsDbContext db) : IUserRepository { … }
  ```
- Folders inside a project mirror concerns (`Controllers/`, `Contracts/`, `Auth/`,
  `Validation/`, `Export/`, `Data/Entities/`, `Migrations/`).

---

## 8. Compliance Checklist

Use this to audit any project (including FMS) against the standard:

- [ ] Solution has the five projects; names/namespaces match §2 / §7.
- [ ] `Model` references nothing; entities are POCOs.
- [ ] `Service` has no project reference to `Repository` and no EF Core package.
- [ ] `Repository` references EF Core; `Api`/`Service`/`Model` do not.
- [ ] `DbContext` + migrations are inside `Repository`; `AddDbContext` is called only in
      `Api`.
- [ ] Every service **and every repository class** is registered by interface in the
      `Api` composition root (§4.2).
- [ ] No `new` for app services outside the composition root; constructor injection
      only (§4.5).
- [ ] Lifetimes follow §4.4 (DB-backed `Scoped`, stateless `Singleton`).
- [ ] No circular project references.
- [ ] HTTP endpoints are MVC controllers with attribute routing (`[ApiController]`,
      `[Route]`, verb attributes); Program.cs has no endpoint lambdas (§6.5.1).
- [ ] Controller actions in `Api` are thin; business logic is in `Service`; queries in
      `Repository`.
- [ ] Every table has the four audit columns (`created_by`, `created_on`,
      `last_modified_by`, `last_modified_on`); every entity implements `IAuditable`
      (§6.1.1).
- [ ] Audit columns are set by the Repository `SaveChanges` interceptor via
      `ICurrentUserProvider` — no service or controller assigns them (§6.1.1).
- [ ] No entity is returned directly as an HTTP payload; DTOs are used (§6.5).
- [ ] `dotnet build` clean; `dotnet test` green.

---

## 9. Related Documents

- FMS product requirements — [docs/PRD.md](PRD.md)
- TDD strategy & test tiers — [docs/testing-and-tdd.md](testing-and-tdd.md)
- Reference implementation — `src/api/Fms.slnx`
