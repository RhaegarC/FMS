using Fms.Api.Data;
using Microsoft.EntityFrameworkCore;

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
// API surface to unauthenticated callers once auth lands (feature 02).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new HealthStatus("Fms.Api", "ok")))
    .WithName("GetHealth");

app.Run();

/// <summary>Payload returned by the <c>/health</c> endpoint.</summary>
record HealthStatus(string Service, string Status);

// Exposes the generated Program class to the test project (WebApplicationFactory).
public partial class Program; // ReSharper disable once UnusedTypeParameter
