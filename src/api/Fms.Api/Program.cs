using System.Security.Cryptography;
using Fms.Api.Auth;
using Fms.Api.ExceptionHandling;
using Fms.Interface.Service;
using Fms.Repository;
using Fms.Repository.Extensions;
using Fms.Service.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers (ASP.NET Core MVC) — the Api layer's HTTP entry points, one controller
// per resource (Controllers/). Swashbuckle reads the controller routes to emit the
// OpenAPI spec at /swagger/v1/swagger.json, consumed by openapi-typescript in
// packages/api-client.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// Current-user provider (backend-standard §6.1.1): the Repository layer's audit
// interceptor stamps createdBy/createdOn + lastModifiedBy/lastModifiedOn from the
// authenticated principal.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

// EF Core (PostgreSQL) + repositories (Repository layer, backend-standard §4.3).
// Connection string comes from Configuration: "ConnectionStrings:Postgres"
// (appsettings.Development.json locally, ConnectionStrings__Postgres env var in
// docker-compose). When absent, the context is registered without a provider so
// /health and Swagger still serve — DB-backed endpoints fail on first use.
builder.Services.AddFmsPersistence(builder.Configuration.GetConnectionString("Postgres"));

// Business logic (Service layer, backend-standard §4.3) — all by interface.
builder.Services.AddFmsServices();

// CORS (feature 01 foundation): allow the single frontend app's origin(s), read from
// configuration "CORS:AllowedOrigins" (env CORS__AllowedOrigins / appsettings.Development).
// No origins configured => the middleware is not registered (backend-only tooling).
var corsOrigins = builder.Configuration["CORS:AllowedOrigins"];
if (!string.IsNullOrWhiteSpace(corsOrigins))
{
    var corsOriginsList = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.Services.AddCors(options =>
        options.AddPolicy("WebApp", policy =>
            policy.WithOrigins(corsOriginsList).AllowAnyHeader().AllowAnyMethod()));
}

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
    // Serve the Swagger UI at the root (bug 01) so a developer hitting the base URL
    // (e.g. http://localhost:5149) lands on the interactive API docs instead of a 404.
    // With RoutePrefix = "" the endpoint must be absolute (/swagger/v1/swagger.json) —
    // a relative URL would resolve against "/" and the UI would fetch /v1/swagger.json (404).
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FMS API v1");
    });
}

// Domain exceptions → HTTP status codes (Service layer throws, Api maps).
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
// Allow the configured frontend origin(s) to call the API cross-origin (feature 01).
if (!string.IsNullOrWhiteSpace(corsOrigins))
{
    app.UseCors("WebApp");
}
app.UseAuthentication();
// Provision a `users` row for authenticated principals before authorization runs,
// so protected controllers can read the current Fms user from HttpContext.Items.
app.UseMiddleware<UserProvisioningMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the generated Program class to the test project (WebApplicationFactory).
public partial class Program; // ReSharper disable once UnusedTypeParameter
