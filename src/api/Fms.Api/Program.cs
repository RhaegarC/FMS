using System.Security.Cryptography;
using Fms.Api.Auth;
using Fms.Api.Data;
using Fms.Api.Data.Entities;
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

/// <summary>Uniform error payload (validation failures, etc.).</summary>
record ApiError(string Message);

// Exposes the generated Program class to the test project (WebApplicationFactory).
public partial class Program; // ReSharper disable once UnusedTypeParameter
