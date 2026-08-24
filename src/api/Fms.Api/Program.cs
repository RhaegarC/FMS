using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fms.Api.Auth;
using Fms.Api.Data;
using Fms.Api.Data.Entities;
using Fms.Api.Export;
using Fms.Api.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI (Swashbuckle): the backend is the single source of truth for the API
// contract. Swagger UI is served at /swagger and the spec at
// /swagger/v1/swagger.json — consumed by openapi-typescript in packages/api-client.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core (PostgreSQL). Connection string comes from Configuration:
// "ConnectionStrings:Postgres" (appsettings.Development.json locally,
// ConnectionStrings__Postgres env var in docker-compose).
builder.Services.AddDbContext<FmsDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("Postgres");
    if (!string.IsNullOrEmpty(conn))
    {
        options.UseNpgsql(conn).UseSnakeCaseNamingConvention();
    }
});

// Entra ID bearer authentication (feature 03). Two configuration paths:
//  - Production: Entra:TenantId + Entra:ClientId — OIDC metadata discovery
//    validates issuer/audience/signing keys automatically.
//  - Test/dev escape hatch: Entra:TestSigningKey (base64 RSA private key) with
//    Entra:Issuer + Entra:ClientId — lets AuthIntegrationTests validate self-issued
//    tokens and local dev run before the Entra app registration exists.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep Entra's short claim names (oid, email, name) instead of mapping them
        // to long URI claim types — the provisioner and handlers read them directly.
        options.MapInboundClaims = false;

        var testSigningKey = builder.Configuration["Entra:TestSigningKey"];
        if (!string.IsNullOrEmpty(testSigningKey))
        {
            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(testSigningKey), out _);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["Entra:Issuer"],
                ValidAudience = builder.Configuration["Entra:ClientId"],
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
            };
        }
        else
        {
            var tenantId = builder.Configuration["Entra:TenantId"];
            var clientId = builder.Configuration["Entra:ClientId"];
            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId))
            {
                options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                options.Audience = clientId;
            }
        }
    });
builder.Services.AddScoped<UserProvisioner>();
builder.Services.AddScoped<PermissionEvaluator>();
builder.Services.AddScoped<JsonSchemaValidator>();
builder.Services.AddScoped<SubmissionExcelExporter>();

builder.Services.AddAuthorization(options =>
{
    // Admin-only catalog mutations (feature 06). The Fms role is DB-stored — the
    // handler reads it from the provisioned user (HttpContext.Items), not a claim.
    options.AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new AdminOnlyRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, AdminOnlyHandler>();

var app = builder.Build();

// Apply EF migrations at startup (feature 02) so the schema exists on first boot.
// If Postgres is unreachable, log and continue: /health still serves for
// orchestration, and DB-backed endpoints surface errors until the DB is up.
using (var scope = app.Services.CreateScope())
{
    try
    {
        scope.ServiceProvider.GetRequiredService<FmsDbContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex,
            "Database migration skipped at startup (is Postgres reachable?): {Message}",
            ex.Message);
    }
}

// Swagger UI + spec are dev-only: openapi-typescript (packages/api-client) pulls the
// spec from the local dev server. Exposing them in prod would surface an interactive
// API surface to unauthenticated callers now that auth has landed (feature 03).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
// Provision a `users` row for authenticated principals before authorization runs,
// so protected handlers can read the current Fms user from HttpContext.Items.
app.UseMiddleware<UserProvisioningMiddleware>();
app.UseAuthorization();

// Health stays anonymous (orchestration probes it before auth is up).
app.MapGet("/health", () => Results.Ok(new HealthStatus("Fms.Api", "ok")))
    .WithName("GetHealth");

// The current user's own profile — protected, drives the admin/user portals.
app.MapGet("/api/me", (HttpContext http) =>
{
    var user = (User)http.Items[UserProvisioningMiddleware.FmsUserKey]!;
    return Results.Ok(new MeResponse(user.Id, user.Name, user.Email, user.Role));
}).RequireAuthorization().WithName("GetMe");

// --- Space catalog (feature 06) ------------------------------------------
// Mutations are admin-only (AdminOnly policy reads the DB-stored role). Reads are
// permission-scoped for non-admins via the feature-05 PermissionEvaluator: a space
// grant covers every form under that space; form access is the union of form-level
// and space-level grants; default deny when no grant matches.

app.MapGet("/api/spaces", async (HttpContext http, FmsDbContext db, PermissionEvaluator evaluator) =>
{
    var user = (User)http.Items[UserProvisioningMiddleware.FmsUserKey]!;
    var spaces = await db.Spaces.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
    if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
    {
        var permissions = await db.Permissions.AsNoTracking().ToListAsync();
        var subject = new PermissionSubject(user.Id, user.Email, user.Role);
        spaces = spaces.Where(s => evaluator.CanAccessSpace(subject, permissions, s.Id)).ToList();
    }

    return Results.Ok(spaces.Select(s => new SpaceDto(s.Id, s.Name)));
}).RequireAuthorization().WithName("ListSpaces");

app.MapPost("/api/spaces", async (CreateSpaceRequest request, FmsDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError("Space name is required."));
    }

    var space = new Space { Name = request.Name.Trim() };
    db.Spaces.Add(space);
    await db.SaveChangesAsync();
    return Results.Created($"/api/spaces/{space.Id}", new SpaceDto(space.Id, space.Name));
}).RequireAuthorization("AdminOnly").WithName("CreateSpace");

app.MapPut("/api/spaces/{id:int}", async (int id, UpdateSpaceRequest request, FmsDbContext db) =>
{
    var space = await db.Spaces.FindAsync(id);
    if (space is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError("Space name is required."));
    }

    space.Name = request.Name.Trim();
    await db.SaveChangesAsync();
    return Results.Ok(new SpaceDto(space.Id, space.Name));
}).RequireAuthorization("AdminOnly").WithName("UpdateSpace");

app.MapDelete("/api/spaces/{id:int}", async (int id, FmsDbContext db) =>
{
    var space = await db.Spaces.FindAsync(id);
    if (space is null)
    {
        return Results.NotFound();
    }

    db.Spaces.Remove(space); // cascades to forms (and their submissions)
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("AdminOnly").WithName("DeleteSpace");

// --- Form catalog (feature 06) -------------------------------------------
// Forms live under a space. Create/update validate the submitted schema as valid
// JSON Schema (draft 2020-12); invalid schemas are rejected with 400 so a broken
// definition can never be stored. Reads are permission-scoped for non-admins.

app.MapGet("/api/spaces/{spaceId:int}/forms", async (int spaceId, HttpContext http, FmsDbContext db, PermissionEvaluator evaluator) =>
{
    var user = (User)http.Items[UserProvisioningMiddleware.FmsUserKey]!;
    var spaceExists = await db.Spaces.AsNoTracking().AnyAsync(s => s.Id == spaceId);
    if (!spaceExists)
    {
        return Results.NotFound();
    }

    var forms = await db.Forms.AsNoTracking()
        .Where(f => f.SpaceId == spaceId)
        .OrderBy(f => f.Name)
        .ToListAsync();
    if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
    {
        var permissions = await db.Permissions.AsNoTracking().ToListAsync();
        var subject = new PermissionSubject(user.Id, user.Email, user.Role);
        forms = forms.Where(f => evaluator.CanAccessForm(subject, permissions, f.Id, f.SpaceId)).ToList();
    }

    return Results.Ok(forms.Select(f => new FormDto(f.Id, f.SpaceId, f.Name, f.Schema, f.UpdatedAt)));
}).RequireAuthorization().WithName("ListFormsInSpace");

app.MapPost("/api/spaces/{spaceId:int}/forms", async (int spaceId, CreateFormRequest request, FmsDbContext db, JsonSchemaValidator validator) =>
{
    var space = await db.Spaces.FindAsync(spaceId);
    if (space is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError("Form name is required."));
    }

    if (!validator.IsValid(request.Schema, out var schemaError))
    {
        return Results.BadRequest(new ApiError($"Invalid JSON Schema (draft 2020-12): {schemaError}"));
    }

    var form = new Form
    {
        SpaceId = spaceId,
        Name = request.Name.Trim(),
        Schema = request.Schema,
        UpdatedAt = DateTimeOffset.UtcNow,
    };
    db.Forms.Add(form);
    await db.SaveChangesAsync();
    return Results.Created($"/api/forms/{form.Id}", new FormDto(form.Id, form.SpaceId, form.Name, form.Schema, form.UpdatedAt));
}).RequireAuthorization("AdminOnly").WithName("CreateForm");

app.MapPut("/api/forms/{id:int}", async (int id, UpdateFormRequest request, FmsDbContext db, JsonSchemaValidator validator) =>
{
    var form = await db.Forms.FindAsync(id);
    if (form is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new ApiError("Form name is required."));
    }

    if (!validator.IsValid(request.Schema, out var schemaError))
    {
        return Results.BadRequest(new ApiError($"Invalid JSON Schema (draft 2020-12): {schemaError}"));
    }

    form.Name = request.Name.Trim();
    form.Schema = request.Schema;
    form.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new FormDto(form.Id, form.SpaceId, form.Name, form.Schema, form.UpdatedAt));
}).RequireAuthorization("AdminOnly").WithName("UpdateForm");

app.MapDelete("/api/forms/{id:int}", async (int id, FmsDbContext db) =>
{
    var form = await db.Forms.FindAsync(id);
    if (form is null)
    {
        return Results.NotFound();
    }

    db.Forms.Remove(form); // cascades to its submissions
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("AdminOnly").WithName("DeleteForm");

// --- Submission APIs (feature 07) --------------------------------------
// Submissions are schema-validated against the form's definition before storage.
// Listing and export are scoped by role: admins see every submission; non-admins
// see only their own submissions on forms they can access (feature-05 evaluator,
// default deny). Keyword search matches the jsonb text representation; form/date
// filters narrow the set. Excel export flattens nested data into a shared table.

app.MapPost("/api/forms/{formId:int}/submissions", async (
    int formId, SubmitSubmissionRequest request, HttpContext http,
    FmsDbContext db, PermissionEvaluator evaluator, JsonSchemaValidator validator) =>
{
    var user = (User)http.Items[UserProvisioningMiddleware.FmsUserKey]!;

    var form = await db.Forms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == formId);
    if (form is null)
    {
        return Results.NotFound();
    }

    // Access-control boundary: admins bypass grants; everyone else needs a
    // space/form grant covering this form (default deny).
    if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
    {
        var permissions = await db.Permissions.AsNoTracking().ToListAsync();
        var subject = new PermissionSubject(user.Id, user.Email, user.Role);
        if (!evaluator.CanAccessForm(subject, permissions, form.Id, form.SpaceId))
        {
            return Results.Forbid();
        }
    }

    if (request.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
    {
        return Results.BadRequest(new ApiError("Submission data is required."));
    }

    var data = request.Data.GetRawText();
    if (!validator.ValidateInstance(form.Schema, data, out var validationError))
    {
        return Results.BadRequest(new ApiError(validationError ?? "Submission does not match the form schema."));
    }

    var submission = new Submission
    {
        FormId = form.Id,
        UserId = user.Id,
        Data = data,
        CreatedAt = DateTimeOffset.UtcNow,
    };
    db.Submissions.Add(submission);
    await db.SaveChangesAsync();

    var dto = new SubmissionDto(submission.Id, submission.FormId, submission.UserId,
        user.Email, submission.Data, submission.CreatedAt);
    return Results.Created($"/api/forms/{form.Id}/submissions/{submission.Id}", dto);
}).RequireAuthorization().WithName("SubmitSubmission");

app.MapGet("/api/me/submissions", async (
    HttpContext http, FmsDbContext db, PermissionEvaluator evaluator,
    int? formId, string? from, string? to, string? q) =>
{
    var user = (User)http.Items[UserProvisioningMiddleware.FmsUserKey]!;

    // The caller's own submissions; non-admins are further limited to forms they
    // can access (union of space + form grants, default deny).
    IQueryable<Submission> query = BuildSubmissionQuery(db, formId, from, to, q)
        .Include(s => s.User)
        .Where(s => s.UserId == user.Id);
    if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
    {
        var accessible = await AccessibleFormIdsAsync(user, db, evaluator);
        query = query.Where(s => accessible.Contains(s.FormId));
    }

    var submissions = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    return Results.Ok(submissions.Select(ToDto));
}).RequireAuthorization().WithName("ListMySubmissions");

app.MapGet("/api/submissions", async (
    FmsDbContext db, int? formId, string? from, string? to, string? q) =>
{
    var submissions = await BuildSubmissionQuery(db, formId, from, to, q)
        .Include(s => s.User)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
    return Results.Ok(submissions.Select(ToDto));
}).RequireAuthorization("AdminOnly").WithName("ListAllSubmissions");

app.MapGet("/api/submissions/export", async (
    FmsDbContext db, SubmissionExcelExporter exporter,
    string format, int? formId, string? from, string? to, string? q) =>
{
    var submissions = await BuildSubmissionQuery(db, formId, from, to, q)
        .Include(s => s.User)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
    return ExportSubmissions(submissions, format, exporter);
}).RequireAuthorization("AdminOnly").WithName("ExportAllSubmissions");

app.MapGet("/api/me/submissions/export", async (
    HttpContext http, FmsDbContext db, PermissionEvaluator evaluator, SubmissionExcelExporter exporter,
    string format, int? formId, string? from, string? to, string? q) =>
{
    var user = (User)http.Items[UserProvisioningMiddleware.FmsUserKey]!;

    IQueryable<Submission> query = BuildSubmissionQuery(db, formId, from, to, q)
        .Include(s => s.User)
        .Where(s => s.UserId == user.Id);
    if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
    {
        var accessible = await AccessibleFormIdsAsync(user, db, evaluator);
        query = query.Where(s => accessible.Contains(s.FormId));
    }

    var submissions = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    return ExportSubmissions(submissions, format, exporter);
}).RequireAuthorization().WithName("ExportMySubmissions");

// --- Shared submission helpers (feature 07) ----------------------------

// Ids of forms the caller can access. Permission evaluation is in-memory (the
// feature-05 expression grammar is the security boundary), so the candidate set
// is loaded then filtered — same approach as the space/form list endpoints.
async Task<HashSet<int>> AccessibleFormIdsAsync(User user, FmsDbContext db, PermissionEvaluator evaluator)
{
    var permissions = await db.Permissions.AsNoTracking().ToListAsync();
    var subject = new PermissionSubject(user.Id, user.Email, user.Role);
    var forms = await db.Forms.AsNoTracking().ToListAsync();
    return forms
        .Where(f => evaluator.CanAccessForm(subject, permissions, f.Id, f.SpaceId))
        .Select(f => f.Id)
        .ToHashSet();
}

// Builds the filtered submission query. Keyword search runs against the jsonb
// text representation — jsonb has no LIKE/ILIKE operator, so the search uses a
// parameterized raw-SQL base with an explicit data::text cast. The remaining
// filters (form id, date range) compose on top as ordinary LINQ.
IQueryable<Submission> BuildSubmissionQuery(
    FmsDbContext db, int? formId, string? from, string? to, string? keyword)
{
    IQueryable<Submission> query;
    if (string.IsNullOrWhiteSpace(keyword))
    {
        query = db.Submissions.AsNoTracking();
    }
    else
    {
        var pattern = $"%{keyword}%";
        query = db.Submissions.FromSqlInterpolated(
                $"SELECT * FROM submissions WHERE data::text ILIKE {pattern}")
            .AsNoTracking();
    }

    if (formId is not null)
    {
        query = query.Where(s => s.FormId == formId);
    }

    var fromDate = ParseDateFilter(from, inclusiveEndOfDay: false);
    if (fromDate is not null)
    {
        query = query.Where(s => s.CreatedAt >= fromDate);
    }

    var toDate = ParseDateFilter(to, inclusiveEndOfDay: true);
    if (toDate is not null)
    {
        query = query.Where(s => s.CreatedAt < toDate);
    }

    return query;
}

// Parses a from/to filter as UTC. A date-only `to` (yyyy-MM-dd) means "through
// the end of that day", so it becomes an exclusive bound at the next midnight.
DateTimeOffset? ParseDateFilter(string? value, bool inclusiveEndOfDay)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var parsed))
    {
        return null;
    }

    if (inclusiveEndOfDay && DateTime.TryParseExact(value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
    {
        parsed = parsed.AddDays(1);
    }

    return parsed;
}

// Formats an export response: JSON array download, Excel workbook download, or a
// 400 for any other format value.
IResult ExportSubmissions(List<Submission> submissions, string format, SubmissionExcelExporter exporter)
{
    if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
    {
        var json = JsonSerializer.Serialize(submissions.Select(ToDto));
        return Results.File(Encoding.UTF8.GetBytes(json), "application/json", "submissions.json");
    }

    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        return Results.File(exporter.BuildWorkbook(submissions),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "submissions.xlsx");
    }

    return Results.BadRequest(new ApiError("Export format must be 'xlsx' or 'json'."));
}

SubmissionDto ToDto(Submission s) =>
    new(s.Id, s.FormId, s.UserId, s.User.Email, s.Data, s.CreatedAt);

app.Run();

/// <summary>Payload returned by the <c>/health</c> endpoint.</summary>
record HealthStatus(string Service, string Status);

/// <summary>Payload returned by <c>/api/me</c> (the authenticated user's profile).</summary>
record MeResponse(int Id, string Name, string Email, string Role);

// --- Space / form catalog payloads (feature 06) --------------------------

/// <summary>Space as exposed by the catalog API.</summary>
record SpaceDto(int Id, string Name);

/// <summary>Form definition as exposed by the catalog API.</summary>
record FormDto(int Id, int SpaceId, string Name, string Schema, DateTimeOffset UpdatedAt);

record CreateSpaceRequest(string Name);
record UpdateSpaceRequest(string Name);
record CreateFormRequest(string Name, string Schema);
record UpdateFormRequest(string Name, string Schema);

// --- Submission payloads (feature 07) ----------------------------------

/// <summary>Submit body: the form's field values as a JSON object.</summary>
record SubmitSubmissionRequest(JsonElement Data);

/// <summary>A submission as exposed by the list/export APIs.</summary>
record SubmissionDto(int Id, int FormId, int UserId, string UserEmail, string Data, DateTimeOffset CreatedAt);

/// <summary>Uniform error payload (validation failures, etc.).</summary>
record ApiError(string Message);

// Exposes the generated Program class to the test project (WebApplicationFactory).
public partial class Program; // ReSharper disable once UnusedTypeParameter
