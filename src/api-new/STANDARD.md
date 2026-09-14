# Backend API Development Standard

This document is the standard for backend APIs built on this solution. It is shipped inside
the project, so it travels with the code: cite it in review, and if you find yourself working
around a rule here, change the rule in a pull request rather than in one service.

It is prescriptive. Where a rule exists because of a specific past failure, the reason is
given — not to justify the rule, but because a rule whose reasoning is missing gets
"simplified" away by the next person.

**Reading it:** sections 1–2 are the layout and the one workflow you will use most. Sections
3–9 are the rules by area. Section 10 is testing, 11 is CI, and **12 lists the places this
standard and the code do not yet agree** — read that one before trusting the rest blindly.

---

## 1. Solution layout

| Project | Responsibility | May reference |
| --- | --- | --- |
| `Fms.Model` | Entities, DTOs, constants, attributes. No behaviour. | *nothing* |
| `Fms.Interface` | The contracts: repository and service interfaces, shared abstractions. | `Fms.Model` |
| `Fms.Service` | Business logic. Implements `Fms.Interface` service contracts. | `Fms.Interface`, `Fms.Model` |
| `Fms.Repository` | Data access. Implements `Fms.Interface` repository contracts. | `Fms.Interface`, `Fms.Model` |
| `Fms.Api` | Composition root: hosting, DI wiring, controllers, middleware, configuration. | all of the above |
| `Fms.TestSupport` | Shared test doubles and harnesses. Holds no tests and is never run directly. | `Fms.Repository`, `Fms.Interface`, `Fms.Model` |
| `Fms.Service.Test` | xUnit tests for `Fms.Service`. | `Fms.Service`, `Fms.Repository`, `Fms.Interface`, `Fms.Model`, `Fms.TestSupport` |
| `Fms.Repository.Test` | xUnit tests for `Fms.Repository`. | `Fms.Repository`, `Fms.Interface`, `Fms.Model`, `Fms.TestSupport` |
| `Fms.Api.Test` | xUnit tests for `Fms.Api`. | `Fms.Api`, `Fms.Service`, `Fms.Repository`, `Fms.Interface`, `Fms.Model`, `Fms.TestSupport` |

Dependencies point **inward**. `Fms.Interface` is the pivot: both `Service` and `Repository`
depend on it because both *implement* it, and neither knows the other exists. `Fms.Api` is the
only project that knows how the parts fit together.

Tests are tiered to match the layer they cover, rather than collected in one project, so a
failing test names the layer it belongs to before you read it. A test project may reference
the layer under test and anything below it, never the layer above. `Fms.TestSupport` exists
because a double needed by two tiers would otherwise be written twice and drift; it is a
library, not a tier — `IsTestProject` is `false` so `dotnet test` does not try to run it.

```
        Fms.Api  ──────────────► everything
           │
           ├──► Fms.Service ──┐
           │                  ├──► Fms.Interface ──► Fms.Model
           └──► Fms.Repository┘
```

### Rules

- **`Fms.Model` must not gain a project reference.** It is the bottom of the graph. The
  moment it depends on something, every project depends on that something.
- **`Fms.Model` must not gain a package reference to a framework.** No EF Core, no ASP.NET
  Core. It is plain data, which is why it can be referenced from anywhere.
- **`Fms.Service` must not reference `Fms.Repository`.** A service depends on
  `Fms.Interface`, never on a concrete data-access type. If a service needs data, it asks for
  an interface.
- **`Fms.Repository` must not reference `Fms.Service`.** Same rule, other direction.
- **Only `Fms.Api` reads configuration.** See section 6.
- Place a contract in `Fms.Interface` under the folder matching its kind — `Repository/`,
  `Service/`, `Infrastructure/`. `IUserContextService` is in `Infrastructure/` and not
  `Service/` because it describes the runtime environment rather than a business capability.

### File and code conventions

These are the conventions the existing code follows; match them rather than introducing a
second style in the same solution.

- **File-scoped namespaces**, with the namespace on the first line and `using` directives
  *after* it:

  ```csharp
  namespace Fms.Service;

  using Fms.Interface.Service;

  public sealed class OrderService : IOrderService
  { }
  ```

- **Primary constructors** for dependency injection; assign to a `private readonly` field only
  when the parameter is used outside the constructor:

  ```csharp
  public sealed class OrderRepository(FmsContext context) : DatabaseRepository(context), IOrderRepository
  { }
  ```

- **`sealed`** on concrete classes that are not designed for inheritance. Everything in this
  solution is `sealed` except `EntityBase` (inherited by design) and `DatabaseRepository`
  (inherited by repositories).
- **Nullable reference types are enabled.** A build is expected to be warning-free; fix the
  warning rather than suppressing it to get a green build.

---

## 2. Adding a feature: the vertical slice

Work top-down or bottom-up, but in one pull request, and always through these layers. For an
`Order` feature:

**1. The entity** — `Fms.Model/DatabaseEntity/Order.cs`

```csharp
namespace Fms.Model.DatabaseEntity;

public sealed class Order : EntityBase
{
    public required string Reference { get; set; }

    public decimal Total { get; set; }

    public string? Notes { get; set; }
}
```

Derive from `EntityBase` unless the type is genuinely append-only and never soft-deleted
(see `AuditLog` for the one such type, and section 3 for why).

**2. The mapping** — `Fms.Repository/FmsContext.cs`

Add a `DbSet` and, if the type needs anything beyond EF's conventions, configure it in
`OnModelCreating`. Indexes and column types belong here, not in attributes on the entity.

```csharp
public DbSet<Order> Orders { get; set; }

// in OnModelCreating
modelBuilder.Entity<Order>(entity =>
{
    entity.HasIndex(order => order.Reference).IsUnique();
});
```

**3. The contract** — `Fms.Interface/Repository/IOrderRepository.cs`

```csharp
namespace Fms.Interface.Repository;

using Fms.Model.DatabaseEntity;

public interface IOrderRepository : IDbRepository;
```

Only add methods beyond `IDbRepository` when the generic operations genuinely cannot express
what you need. **Do not expose `IQueryable`** from a repository — it leaks the data layer into
its callers and makes the query impossible to reason about from the interface.

**4. The implementation** — `Fms.Repository/OrderRepository.cs`

```csharp
namespace Fms.Repository;

using Fms.Interface.Repository;
using Fms.Model.DatabaseEntity;

public sealed class OrderRepository(FmsContext context) : DatabaseRepository(context), IOrderRepository
{
    public Task<Order?> GetByReferenceAsync(string reference) =>
        GetAsync<Order>(order => order.Reference == reference);
}
```

**5. Registration** — `Fms.Repository/PersistenceExtensions.cs`

```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

**6. The service contract and implementation** — under `Fms.Interface/Service/` and
`Fms.Service/`. Controllers never see a repository; they see a service.

**7. The controller** — `Fms.Api/Controllers/OrderController.cs`

```csharp
namespace Fms.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fms.Interface.Service;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var order = await orderService.GetAsync(id);
        return order is null ? NotFound() : Ok(order);
    }
}
```

Inherit `ControllerBase`, not `Controller` — `Controller` pulls in view support this project
never uses. **Add `[Authorize]` deliberately**; see section 7, endpoints are anonymous by
default.

**8. Tests** — see section 10. A feature is not finished without one.

---

## 3. Data model rules

### Keys are application-assigned

Every `EntityBase` gets a `string` primary key at construction, in the initializer. This is
not a style preference:

- The row is **identifiable before it is saved**. An insert can be audited with its real id
  (section 5); a caller can return the id it will use without waiting for the database.
- A database-generated key exists only after the save, so any code that needs it — audit,
  logging, an idempotency response — has to run a second query to find out what it wrote.

**Never** reconfigure a key to `ValueGeneratedOnAdd`, and never switch to `int` identity.
Doing so silently breaks audit `EntityId` on insert, and the failure is invisible until
someone needs the history.

Set the id explicitly only when importing or replaying known data; otherwise let the
constructor assign it.

### Soft delete

Deleting sets `IsDeleted`; the row stays. A global query filter, applied **by convention** to
every `EntityBase` type in `FmsContext.ApplySoftDeleteFilter`, hides those rows from ordinary
reads. Because it is applied by convention rather than entity by entity, a type you add
inherits it automatically — you do not register it, and you cannot forget it.

- Use `IDbRepository.DeleteAsync`, which soft-deletes. Do not call `Remove` on an
  `EntityBase` — that is a hard delete, and the row is gone.
- To read deleted rows, use `.IgnoreQueryFilters()` explicitly. It is deliberately visible at
  the call site: reading history is a decision, not a default.
- A type that should **not** be filtered must not derive from `EntityBase`. `AuditLog` is the
  example: history is append-only, so it has no deleted state to hide.

### Timestamps

`AuditLog.Timestamp` is a `DateTimeOffset`. The audit columns on `EntityBase`
(`CreatedOn`, `LastModifiedOn`) are `DateTime` and are **not yet maintained** — see section
12. Record UTC, and prefer `DateTimeOffset` for anything new.

---

## 4. Persistence rules

- **All mapping configuration lives in `OnModelCreating`.** Indexes, column types, unique
  constraints. Not data annotations on the entity — the entity is a plain model and should not
  carry storage decisions.
- **String columns are `text` by default.** Anything holding JSON must be configured as
  `jsonb` explicitly, as `AuditLog.OldValues` and `NewValues` are. A JSON document stored in
  `text` is a write-only blob; `jsonb` stays queryable.
- **Index what you query by.** Audit history is read by "what happened to this row" and "what
  happened around then", which is why `AuditLog` carries an index on
  `(TableName, EntityId)` and one on `Timestamp`. Add the index when you add the query, not
  after the table is large.
- **The `DbContext` is scoped and is not thread-safe.** Never share one across concurrent
  tasks, and never start a task inside a loop that touches it. See section 12 for a
  batch-write bug of exactly this shape found in the repository.
- **Never put a raw `IQueryable` on a repository interface.**

---

## 5. Auditing

Auditing is automatic. `AuditSaveChangesInterceptor` runs on **both** `SaveChanges()` and
`SaveChangesAsync()` and records every added, modified or deleted entity into `AuditLogs` on
the same context, so history is written in the same transaction as the change it describes.

**You do not write to `AuditLogs` yourself**, and you do not call an audit method. Save the
entity.

### What is recorded

| Field | Value |
| --- | --- |
| `TableName` | The mapped table name. |
| `EntityId` | The primary key, populated for inserts as well as updates (section 3). |
| `Action` | The EF `EntityState`: `"Added"`, `"Modified"`, `"Deleted"`. |
| `OldValues` / `NewValues` | JSON snapshots. `OldValues` is null for an insert, `NewValues` is null for a delete. |
| `ChangedColumns` | Which columns changed, for an update. |
| `Actor` | See below. Never null. |
| `ActorName`, `IpAddress`, `UserAgent`, `CorrelationId` | From the request in flight, when there is one. |

### `Actor` — three distinct values, not one "unknown"

`Actor` is never null, and the fallback is chosen to be informative rather than uniform:

- The Entra **Object ID** when the request is authenticated.
- `"anonymous"` when a request arrived **without** an authenticated user.
- `"system"` when there is **no request at all** — startup, a background job.

A missing token and a background job are different problems. Do not collapse them.

### Opting a property out

Annotate a property with `[NotAudited]` to keep it out of `OldValues` / `NewValues`:

```csharp
[NotAudited]
public string? PasswordHash { get; set; }
```

Audit rows typically have looser access control than the tables they describe, so a value
copied into one becomes readable by people who cannot read the source table — and it stays
readable for as long as the history is kept. **Annotate a property the moment it becomes
sensitive** — a credential, a token, a government identifier, personal data — rather than
waiting for a leak to point it out. Nothing in this solution is annotated yet, because the
only entity holds no secret.

---

## 6. Configuration

Configuration is `IConfiguration`: `appsettings.json`, then `appsettings.{Environment}.json`,
then user-secrets, then environment variables, then command line. Later sources win.

**Rules:**

- **Read configuration only at the composition root** (`Program.cs`) and pass *resolved
  values* down. Library layers take a `string? connectionString`, not an `IConfiguration`.
  This is what keeps `Fms.Repository` config-agnostic and testable.
- **No static configuration class.** An earlier design read the process environment in a
  static constructor and threw from it, which coupled unrelated settings: a missing `TenantId`
  broke code that only wanted the connection string. Do not reintroduce it.
- **Keys are flat and unprefixed**, so the same name works as an environment variable:
  `DbConnection=...` on the command line, `DbConnection` in `appsettings.json`.
- **Every key is declared in `appsettings.json` with an empty default.** An empty value means
  "not configured", which is a defined state, not an error. Add the key name to
  `Constant.ConfigKey` so it is never spelled as a literal in two places.
- **Credentials never go in the repository.** Not in `appsettings.json`, and never in
  `Properties/launchSettings.json` — that file is version-controlled, so anything in it is
  handed to every clone. Use user-secrets:

  ```bash
  dotnet user-secrets set "DbConnection" "Host=localhost;Database=Orders;Username=<user>;Password=<password>"
  ```

- **A missing required setting fails at startup with a message naming the setting** — not with
  a null reference when the first request arrives, and not with an exception naming a local
  variable.

---

## 7. Authentication and authorization

Authentication is Entra ID bearer tokens, wired in `ServiceExt.AddEntraAuthentication`. It
activates only when **both** `TenantId` and `Audience` are configured:

- Both present — JWT bearer is the default scheme, validating issuer, audience, lifetime and
  signing key against `https://login.microsoftonline.com/{tenantId}/v2.0`.
- Either missing — **no authentication scheme is registered**, and the host logs a warning at
  startup saying every request is anonymous. The application still starts, so `/health` and
  OpenAPI work on a project that has not been pointed at a tenant yet.

There is **no client secret**. Validating an inbound token needs only the public signing keys
that the authority serves. Do not add one.

### The rule that matters

> **The scheme validates tokens. It does not protect endpoints.**

**Every endpoint is anonymous until you put `[Authorize]` on it** — including with `TenantId`
and `Audience` both set. Put it on the controller by default and open individual actions
deliberately with `[AllowAnonymous]`, so the exception is visible in the diff:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpGet("health")]      // the one endpoint that is deliberately public
    [AllowAnonymous]
    public IActionResult Health() => Ok();
}
```

Read the authenticated caller from `IUserContextService` (section 8), never by parsing the
token again in your own code.

---

## 8. Knowing who is acting

Inject `IUserContextService` (`Fms.Interface/Infrastructure/`). It exposes `EntraObjectId`,
`HasActiveRequest`, `ActorName`, `IpAddress`, `UserAgent` and `CorrelationId`, all read-only.

Every member is get-only by design: the values describe the request in flight, and nothing
outside the abstraction should be able to rewrite them. Treat `null` as a real answer — there
may be no request at all — and branch on `HasActiveRequest` when the difference matters, as
`Actor` resolution does in section 5.

---

## 9. HTTP behaviour

### Errors

`AddProblemDetails()` and `UseExceptionHandler()` are registered, so an unhandled exception
becomes an RFC 9457 `application/problem+json` response with a traceable id. **Let exceptions
propagate** — do not add a catch-all that rewrites every failure into a `500` or, worse, a
`401`. Throw a specific exception, return a correct status code for expected outcomes
(`NotFound()`, `BadRequest()`), and let the handler deal with the unexpected.

Do not put stack traces or exception messages in a response body outside Development.

### Health and API description

- `GET /health` — liveness. It must stay reachable with no database configured.
- `/openapi/v1.json` — the OpenAPI document, Development only.

### CORS

`AllowedOrigins` is a **comma**-separated list; `ServiceExt.AllowCORS` splits on `,` and
trims. A semicolon produces one origin that matches nothing, and the failure appears in a
browser, far from the setting. When it is empty the application **fails at startup** with a
message naming `AllowedOrigins` — a silent misconfiguration here is a production incident.

Development defaults are set in `appsettings.Development.json` and must stay in step with the
`applicationUrl` values in `launchSettings.json`.

### Pipeline order

`Program.cs` registers middleware in a deliberate order, and it matters:

```
UseExceptionHandler      wraps everything downstream
  MapOpenApi             Development only
  UseHttpsRedirection
  UseCors                before authentication: preflight requests are unauthenticated
  UseAuthentication      must precede UseAuthorization
  UseAuthorization
  MapControllers
  UseHealthChecks
```

**Do not insert middleware without deciding where it belongs in this list.** Anything
registered after `MapControllers()` still runs before the endpoints, because `UseRouting` and
`UseEndpoints` are positioned automatically — so "it looked like it went last" is not a reason
to put it there.

---

## 10. Testing

Tests live in the three tiered projects (`Fms.Service.Test`, `Fms.Repository.Test`,
`Fms.Api.Test`) and run with `dotnet test`. **A behaviour change without a test is
not finished**; the tests in this solution exist because every one of them catches a specific
regression that had already happened once.

### The pattern: no database required

EF Core runs save interception **before it opens a connection**. Point the context at a port
nothing listens on, and the audit entries are already staged in the change tracker by the time
the save fails — so audit behaviour is testable with no database at all. `Fms.TestSupport/AuditHarness.cs`
wraps this, and wires the context through `AddRepositoryPersistence` rather than by hand, so
tests exercise the same composition the application uses.

```csharp
[Fact]
public void An_insert_is_recorded_with_its_key_and_table()
{
    using var harness = new AuditHarness();
    var order = new Order { Reference = "A-1" };
    harness.Context.Orders.Add(order);

    harness.Save();                       // fails on connection, after interception

    var log = harness.SingleEntry();
    Assert.Equal("Orders", log.TableName);
    Assert.Equal(order.Id, log.EntityId);
}
```

`FakeUserContext` stands in for the HTTP-backed implementation; set the caller you want to
test — authenticated, anonymous, or no request at all.

Queries can be inspected without executing them using `ToQueryString()`, which builds the SQL
and stops.

### Rules

- **Test behaviour, not implementation.** Assert on what a caller observes.
- **A test that cannot fail is not a test.** For anything asserting on generated SQL or on
  absence, comment out the thing under test and confirm the test goes red before you trust it.
  A substring check on a column name passes whether or not the filter is applied; assert on
  the `WHERE` clause, or pair it with a control test that asserts the opposite.
- **Name the condition and the expected result**: `An_unauthenticated_request_is_recorded_as_anonymous`,
  not `TestActor2`.
- **One behaviour per test.** Use `[Theory]` for the same behaviour across inputs.

---

## 11. Continuous integration

**FMS has no CI.** There is no `.github/workflows`, so nothing runs on push or pull request and
the checks below are manual. Written down so that adding a workflow is transcription rather than
decision.

A job needs to do two things, from `src/api-new/`:

- `dotnet build` — must be warning-free. Nullable is enabled; fix warnings rather than suppress them.
- `dotnet test` — must pass, **and the exit code must be checked**, not just the "Passed!" lines.
  A solution-level run reports a non-zero exit when any project fails to host tests even when every
  test passed. That is not hypothetical: adding `Fms.TestSupport` did it, because `dotnet test`
  probes every project in the solution for a test host. The fix is `IsTestProject=false` on the
  library — see section 1.

The template repository runs a second job that regenerates from the package and builds the result.
**That job has no counterpart here**: FMS consumes the template, it does not ship it. Template rot
is the template's problem, and the fix belongs in a release of `Rg.Backend.Api` rather than in a
workflow in this repository.

---

## 12. Where the code and this standard do not yet agree

Listed so you are not surprised by them, and so fixing one is an obvious pull request.

1. **`DatabaseRepository.DeleteAsync` hardcodes `LastModifiedBy = "sys"`.** It runs at the wrong
   time to know who is acting, and it attributes a real user's action to the system. In practice
   the interceptor overwrites it during the save, so the recorded value is right — but the line is
   a lie that becomes one the moment the interceptor is not registered, and `DeleteAsync` should
   read `IUserContextService` instead.
2. **`DeleteAsync` issues one `FindAsync` per id** and wraps the batch in no transaction, so a
   large delete is N round trips and can partially apply.
3. **No EF Core migrations are present.** `src/api-new` therefore cannot create its own schema.
   This is the largest open gap: decide the migration strategy before the first real deployment.
   The deployed `src/api` carries an `InitialCreate`, which is the shape to follow.
4. **`UserController` is still template scaffolding.** It inherits `Controller` rather than
   `ControllerBase`, routes on `[controller]` rather than `api/[controller]` as section 2 shows,
   and carries a placeholder `Index` action. Worse than untidy: its `GET /User/me` is **not** the
   deployed contract, which is `GET /api/me` returning a `MeResponse`. The ported endpoint
   currently returns the raw `User` entity, which leaks `IsDeleted` and the audit columns into the
   response body. Replacing it is the Session & HTTP surface slice.
5. **A soft delete is recorded as `"Modified"`,** not as a distinct `"Deleted"`, because
   `Action` holds the EF `EntityState`. A soft delete is therefore indistinguishable in the
   history from an ordinary update.
6. **Timestamps are inconsistent.** `EntityBase` uses `DateTimeOffset` while `DatabaseRepository`
   assigns `DateTime.UtcNow`, which converts implicitly to a zero offset. Consistent in effect,
   inconsistent in kind.
7. **`Fms.Api.http` requests `/weatherforecast/`,** which does not exist in this solution.

---

## 13. Pull request checklist

- [ ] New code respects the dependency direction in section 1.
- [ ] New entities derive from `EntityBase`, or the reason they do not is stated.
- [ ] Keys are application-assigned; no key was reconfigured to be database-generated.
- [ ] Anything soft-deleted goes through `DeleteAsync`, not `Remove`.
- [ ] Sensitive properties carry `[NotAudited]`.
- [ ] New configuration keys are flat, declared empty in `appsettings.json`, and named via
      `Constant.ConfigKey`. No credential is committed.
- [ ] New endpoints carry `[Authorize]`, or the reason they do not is stated.
- [ ] Tests cover the behaviour, and any test asserting on generated SQL was confirmed to fail
      when the thing under test is removed.
- [ ] `dotnet build` and `dotnet test` pass, and CI's template job stays green.
- [ ] Any rule above that had to be worked around is updated in this document, in the same
      pull request.
