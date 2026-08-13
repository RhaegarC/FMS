var builder = WebApplication.CreateBuilder(args);

// OpenAPI (Swashbuckle): the backend is the single source of truth for the API
// contract. Swagger UI is served at /swagger and the spec at
// /swagger/v1/swagger.json — consumed by openapi-typescript in packages/api-client.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core + Npgsql packages are referenced here (see Fms.Api.csproj).
// The DbContext and schema are introduced in feature 04 (db-schema).
// Connection string comes from Configuration: "ConnectionStrings:Postgres".

var app = builder.Build();

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
