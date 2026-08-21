using System.Security.Cryptography;
using Fms.Api.Auth;
using Fms.Api.Data;
using Fms.Api.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.Services.AddAuthorization();
builder.Services.AddScoped<UserProvisioner>();

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

app.Run();

/// <summary>Payload returned by the <c>/health</c> endpoint.</summary>
record HealthStatus(string Service, string Status);

/// <summary>Payload returned by <c>/api/me</c> (the authenticated user's profile).</summary>
record MeResponse(int Id, string Name, string Email, string Role);

// Exposes the generated Program class to the test project (WebApplicationFactory).
public partial class Program; // ReSharper disable once UnusedTypeParameter
