using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Fms.Api.Test;

/// <summary>
/// Integration tests for feature 03 (Entra auth): the backend validates bearer JWTs
/// and auto-provisions a <c>users</c> row on first login. Tokens are self-issued
/// RS256 (issuer <c>https://fms.test</c>, audience <c>test-audience</c>) and validated
/// against a test signing key supplied via config — no real Entra tenant needed.
/// </summary>
public class AuthIntegrationTests : IAsyncLifetime
{
    private const string Issuer = "https://fms.test";
    private const string Audience = "test-audience";
    private const string AdminUserIds = "admin-oid-123";

    private PostgreSqlContainer _pg = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private RSA _rsa = null!;
    private string _signingKeyB64 = null!;

    public async Task InitializeAsync()
    {
        // Generate the test signing key once; Program.cs validates tokens against it.
        _rsa = RSA.Create(2048);
        _signingKeyB64 = Convert.ToBase64String(_rsa.ExportRSAPrivateKey());

        _pg = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("fms")
            .WithUsername("fms")
            .WithPassword("fms")
            .Build();
        await _pg.StartAsync();

        _factory = CreateFactory(AdminUserIds);

        using var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(string adminUserIds) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Postgres", _pg.GetConnectionString());
                builder.UseSetting("Entra:ClientId", Audience);
                builder.UseSetting("Entra:Issuer", Issuer);
                builder.UseSetting("Entra:TestSigningKey", _signingKeyB64);
                builder.UseSetting("ADMIN_USER_IDS", adminUserIds);
            });

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _pg.DisposeAsync();
        _rsa.Dispose();
    }

    /// <summary>Serializes a self-issued RS256 JWT for the given principal claims.</summary>
    private string CreateToken(string oid, string email, string name, RSA? signingKey = null)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = new Dictionary<string, object>
            {
                ["oid"] = oid,
                ["email"] = email,
                ["name"] = name,
            },
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signingKey ?? _rsa), SecurityAlgorithms.RsaSha256),
        };
        return handler.CreateToken(descriptor);
    }

    private HttpClient ClientWithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
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

    private async Task<long> UserCountAsync(string oid)
    {
        var rows = await QueryAsync(
            $"SELECT COUNT(*) FROM users WHERE entra_object_id = '{oid}'");
        return (long)rows.Rows[0][0]!;
    }

    private async Task<string> UserRoleAsync(string oid)
    {
        var rows = await QueryAsync(
            $"SELECT role FROM users WHERE entra_object_id = '{oid}'");
        return rows.Rows[0][0]!.ToString()!;
    }

    private sealed record MeDto(int Id, string Name, string Email, string Role);

    [Fact]
    public async Task Anonymous_GetApiMe_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_FirstRequest_ProvisionsUserWithRoleUser()
    {
        using var client = ClientWithToken(
            CreateToken("user-oid-456", "alice@example.com", "Alice"));

        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<MeDto>();
        Assert.Equal("user", me!.Role);
        Assert.Equal("Alice", me.Name);
        Assert.Equal("alice@example.com", me.Email);

        Assert.Equal(1, await UserCountAsync("user-oid-456"));
        Assert.Equal("user", await UserRoleAsync("user-oid-456"));
    }

    [Fact]
    public async Task ValidToken_UserInAdminUserIds_ProvisionsWithRoleAdmin()
    {
        using var client = ClientWithToken(
            CreateToken("admin-oid-123", "bob@example.com", "Bob"));

        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<MeDto>();
        Assert.Equal("admin", me!.Role);

        Assert.Equal(1, await UserCountAsync("admin-oid-123"));
        Assert.Equal("admin", await UserRoleAsync("admin-oid-123"));
    }

    [Fact]
    public async Task TokenSignedByUnknownKey_ReturnsUnauthorized()
    {
        using var rogue = RSA.Create(2048);
        var token = CreateToken("user-oid-456", "mallory@example.com", "Mallory", rogue);

        using var client = ClientWithToken(token);
        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RepeatedRequests_DoNotDuplicateUserRow()
    {
        using var client = ClientWithToken(
            CreateToken("user-oid-789", "carol@example.com", "Carol"));

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/api/me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Equal(1, await UserCountAsync("user-oid-789"));
    }

    [Fact]
    public async Task ExistingUserAddedToAdminList_IsPromotedToAdmin()
    {
        // Provision as a regular user first (not in the admin list).
        using (var client = ClientWithToken(
                   CreateToken("late-admin-oid", "dave@example.com", "Dave")))
        {
            var response = await client.GetAsync("/api/me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        Assert.Equal("user", await UserRoleAsync("late-admin-oid"));

        // A second app instance now includes the user in ADMIN_USER_IDS: promotion.
        using var adminFactory = CreateFactory($"{AdminUserIds},late-admin-oid");
        using (var client = adminFactory.CreateClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", CreateToken("late-admin-oid", "dave@example.com", "Dave"));
            var response = await client.GetAsync("/api/me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Equal("admin", await UserRoleAsync("late-admin-oid"));
        Assert.Equal(1, await UserCountAsync("late-admin-oid"));
    }

    [Fact]
    public async Task UsersTable_StoresNoPasswordColumn()
    {
        var cols = await QueryAsync(
            @"SELECT column_name FROM information_schema.columns
              WHERE table_schema = 'public' AND table_name = 'users'");

        var names = cols.Rows.Cast<DataRow>()
            .Select(r => r[0]!.ToString()!.ToLowerInvariant())
            .ToList();

        Assert.DoesNotContain(names, c => c.Contains("password"));
    }
}
