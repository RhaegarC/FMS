using System.Data;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Fms.Tests;

/// <summary>
/// RED for feature 02 (db-schema): the backend must create the FMS schema on
/// startup via EF Core migrations — tables, jsonb columns, and foreign keys.
/// Runs against a real PostgreSQL (Testcontainers) because migrations, FK
/// constraints, and jsonb types cannot be validated by the InMemory provider.
/// </summary>
public class DbSchemaTests : IAsyncLifetime
{
    private PostgreSqlContainer _pg = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _pg = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("fms")
            .WithUsername("fms")
            .WithPassword("fms")
            .Build();
        await _pg.StartAsync();

        // Boot the app pointing at the ephemeral Postgres so startup migrations run.
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("ConnectionStrings:Postgres", _pg.GetConnectionString()));

        using var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _pg.DisposeAsync();
    }

    private async Task<DataTable> QueryAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    [Fact]
    public async Task Startup_AppliesMigrations_AndCreatesAllTables()
    {
        var tables = await QueryAsync(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'");

        var names = tables.Rows.Cast<DataRow>().Select(r => r[0]!.ToString()!).ToHashSet();

        Assert.True(
            names.IsSupersetOf(new[] { "users", "spaces", "forms", "submissions", "permissions" }),
            $"Missing table(s). Found: {string.Join(", ", names.Order())}");
    }

    [Fact]
    public async Task Schema_DefinesExpectedColumns_AndJsonbTypes()
    {
        var cols = await QueryAsync(
            @"SELECT table_name, column_name, udt_name
              FROM information_schema.columns
              WHERE table_schema = 'public'");

        var map = cols.Rows.Cast<DataRow>().ToDictionary(
            r => $"{r["table_name"]}.{r["column_name"]}",
            r => r["udt_name"]!.ToString()!);

        // jsonb columns for form schema and submission data
        Assert.Equal("jsonb", map["forms.schema"]);
        Assert.Equal("jsonb", map["submissions.data"]);

        // users key fields (PRD data model)
        foreach (var col in new[] { "entra_object_id", "email", "name", "role" })
        {
            Assert.True(map.ContainsKey($"users.{col}"), $"users.{col} column missing");
        }

        // relational columns
        foreach (var col in new[] { "spaces.name", "forms.space_id", "forms.name",
                                    "submissions.form_id", "submissions.user_id",
                                    "permissions.resource_type", "permissions.resource_id",
                                    "permissions.expression" })
        {
            Assert.True(map.ContainsKey(col), $"{col} column missing");
        }
    }

    [Fact]
    public async Task Schema_DefinesExpectedForeignKeys()
    {
        var fks = await QueryAsync(
            @"SELECT c.conrelid::regclass::text AS child,
                      c.confrelid::regclass::text AS parent
              FROM pg_constraint c
              WHERE c.contype = 'f' AND c.connamespace = 'public'::regnamespace");

        var pairs = fks.Rows.Cast<DataRow>()
            .Select(r => $"{r["child"]}->{r["parent"]}")
            .ToHashSet();

        Assert.Contains("forms->spaces", pairs);
        Assert.Contains("submissions->forms", pairs);
        Assert.Contains("submissions->users", pairs);
    }
}
