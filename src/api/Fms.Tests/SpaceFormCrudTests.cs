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

namespace Fms.Tests;

/// <summary>
/// RED for feature 06 (space/form CRUD). Spaces and forms are managed by admins
/// (the role is DB-stored, so gating reads the provisioned Fms user, not a claim);
/// form schemas must be valid JSON Schema (draft 2020-12); reads are permission-
/// scoped for non-admins via the feature-05 evaluator. Same self-issued RS256 JWT
/// harness as AuthIntegrationTests.
/// </summary>
public class SpaceFormCrudTests : IAsyncLifetime
{
    private const string Issuer = "https://fms.test";
    private const string Audience = "test-audience";
    private const string AdminUserIds = "admin-oid-123";
    private const string UserOid = "user-oid-456";
    private const string UserEmail = "alice@example.com";

    private PostgreSqlContainer _pg = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private RSA _rsa = null!;
    private string _signingKeyB64 = null!;

    public async Task InitializeAsync()
    {
        _rsa = RSA.Create(2048);
        _signingKeyB64 = Convert.ToBase64String(_rsa.ExportRSAPrivateKey());

        _pg = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("fms")
            .WithUsername("fms")
            .WithPassword("fms")
            .Build();
        await _pg.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Postgres", _pg.GetConnectionString());
                builder.UseSetting("Entra:ClientId", Audience);
                builder.UseSetting("Entra:Issuer", Issuer);
                builder.UseSetting("Entra:TestSigningKey", _signingKeyB64);
                builder.UseSetting("ADMIN_USER_IDS", AdminUserIds);
            });

        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _pg.DisposeAsync();
        _rsa.Dispose();
    }

    private string CreateToken(string oid, string email)
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
                ["name"] = email.Split('@')[0],
            },
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256),
        };
        return handler.CreateToken(descriptor);
    }

    private HttpClient AdminClient() => ClientWithToken(CreateToken(AdminUserIds, "bob@example.com"));
    private HttpClient UserClient() => ClientWithToken(CreateToken(UserOid, UserEmail));

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

    /// <summary>Inserts a grant expression directly — feature 05 has no web UI for
    /// permissions, and admin edits them via SQL in production too. Parameterized
    /// because expressions contain quoted literals (e.g. <c>user.email = '…'</c>).</summary>
    private async Task GrantAsync(string resourceType, int resourceId, string expression)
    {
        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO permissions (resource_type, resource_id, expression) VALUES (@type, @id, @expr)",
            conn);
        cmd.Parameters.AddWithValue("type", resourceType);
        cmd.Parameters.AddWithValue("id", resourceId.ToString());
        cmd.Parameters.AddWithValue("expr", expression);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Tests in a class share one container; wipe catalog state so each
    /// permission test starts from a clean slate regardless of execution order.</summary>
    private async Task ResetCatalogAsync()
    {
        await QueryAsync("DELETE FROM permissions");
        await QueryAsync("DELETE FROM spaces"); // cascades to forms + submissions
    }

    private async Task<SpaceDto> CreateSpaceAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/spaces", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SpaceDto>())!;
    }

    private async Task<FormDto> CreateFormAsync(
        HttpClient client, int spaceId, string name, string schema = """{"type":"object"}""")
    {
        var response = await client.PostAsJsonAsync($"/api/spaces/{spaceId}/forms", new { name, schema });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FormDto>())!;
    }

    private sealed record SpaceDto(int Id, string Name);
    private sealed record FormDto(int Id, int SpaceId, string Name, string Schema, DateTimeOffset UpdatedAt);

    // --- Spaces: admin-only CRUD -----------------------------------------

    [Fact]
    public async Task Anonymous_GetSpaces_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/spaces");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_CreateSpace_ReturnsForbidden()
    {
        using var user = UserClient();
        var response = await user.PostAsJsonAsync("/api/spaces", new { name = "Nope" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreateSpace_ReturnsCreatedAndPersists()
    {
        using var admin = AdminClient();
        var response = await admin.PostAsJsonAsync("/api/spaces", new { name = "Finance" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var space = (await response.Content.ReadFromJsonAsync<SpaceDto>())!;
        Assert.True(space.Id > 0);
        Assert.Equal("Finance", space.Name);

        var rows = await QueryAsync($"SELECT COUNT(*) FROM spaces WHERE id = {space.Id}");
        Assert.Equal(1L, (long)rows.Rows[0][0]!);
    }

    [Fact]
    public async Task Admin_CreateSpace_EmptyName_ReturnsBadRequest()
    {
        using var admin = AdminClient();
        var response = await admin.PostAsJsonAsync("/api/spaces", new { name = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_GetSpaces_ListsAllSpaces()
    {
        using var admin = AdminClient();
        var a = await CreateSpaceAsync(admin, "Finance");
        var b = await CreateSpaceAsync(admin, "HR");

        var spaces = await admin.GetFromJsonAsync<List<SpaceDto>>("/api/spaces");
        Assert.Contains(spaces!, s => s.Id == a.Id && s.Name == "Finance");
        Assert.Contains(spaces!, s => s.Id == b.Id && s.Name == "HR");
    }

    [Fact]
    public async Task Admin_UpdateSpace_ChangesName()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Old Name");

        var response = await admin.PutAsJsonAsync($"/api/spaces/{space.Id}", new { name = "New Name" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = (await response.Content.ReadFromJsonAsync<SpaceDto>())!;
        Assert.Equal("New Name", updated.Name);

        var row = await QueryAsync($"SELECT name FROM spaces WHERE id = {space.Id}");
        Assert.Equal("New Name", row.Rows[0][0]!.ToString());
    }

    [Fact]
    public async Task Admin_UpdateMissingSpace_ReturnsNotFound()
    {
        using var admin = AdminClient();
        var response = await admin.PutAsJsonAsync("/api/spaces/999999", new { name = "X" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_DeleteSpace_RemovesIt()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Temp");

        var response = await admin.DeleteAsync($"/api/spaces/{space.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var rows = await QueryAsync($"SELECT COUNT(*) FROM spaces WHERE id = {space.Id}");
        Assert.Equal(0L, (long)rows.Rows[0][0]!);
    }

    // --- Forms: admin-only CRUD under a space -----------------------------

    [Fact]
    public async Task Admin_CreateForm_WithValidSchema_ReturnsCreated()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");

        var response = await admin.PostAsJsonAsync($"/api/spaces/{space.Id}/forms",
            new { name = "Expense Report", schema = """{"type":"object","properties":{"amount":{"type":"number"}}}""" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var form = (await response.Content.ReadFromJsonAsync<FormDto>())!;
        Assert.True(form.Id > 0);
        Assert.Equal(space.Id, form.SpaceId);
        Assert.Equal("Expense Report", form.Name);
    }

    [Fact]
    public async Task Admin_CreateForm_InvalidSchema_ReturnsBadRequest()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");

        var response = await admin.PostAsJsonAsync($"/api/spaces/{space.Id}/forms",
            new { name = "Broken", schema = """{"type":"not-a-real-type"}""" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreateForm_MalformedJson_ReturnsBadRequest()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");

        var response = await admin.PostAsJsonAsync($"/api/spaces/{space.Id}/forms",
            new { name = "Broken", schema = "{not json" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_CreateForm_ReturnsForbidden()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");

        using var user = UserClient();
        var response = await user.PostAsJsonAsync($"/api/spaces/{space.Id}/forms",
            new { name = "Nope", schema = "{}" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_UpdateForm_UpdatesNameAndSchema()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Old Name");

        var response = await admin.PutAsJsonAsync($"/api/forms/{form.Id}",
            new { name = "New Name", schema = """{"type":"object","properties":{"amount":{"type":"number"}}}""" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = (await response.Content.ReadFromJsonAsync<FormDto>())!;
        Assert.Equal("New Name", updated.Name);
        Assert.Contains("amount", updated.Schema);
    }

    [Fact]
    public async Task Admin_UpdateForm_InvalidSchema_ReturnsBadRequest()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Fine");

        var response = await admin.PutAsJsonAsync($"/api/forms/{form.Id}",
            new { name = "Broken", schema = """{"type":"not-a-real-type"}""" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_DeleteForm_RemovesIt()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Temp");

        var response = await admin.DeleteAsync($"/api/forms/{form.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var rows = await QueryAsync($"SELECT COUNT(*) FROM forms WHERE id = {form.Id}");
        Assert.Equal(0L, (long)rows.Rows[0][0]!);
    }

    [Fact]
    public async Task Admin_ListFormsInSpace_ListsForms()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var a = await CreateFormAsync(admin, space.Id, "Form A");
        var b = await CreateFormAsync(admin, space.Id, "Form B");

        var forms = await admin.GetFromJsonAsync<List<FormDto>>($"/api/spaces/{space.Id}/forms");
        Assert.Contains(forms!, f => f.Id == a.Id);
        Assert.Contains(forms!, f => f.Id == b.Id);
    }

    // --- Permission-scoped reads for non-admins ---------------------------

    [Fact]
    public async Task NonAdmin_GetSpaces_NoGrants_ReturnsEmptyList()
    {
        await ResetCatalogAsync();
        using var admin = AdminClient();
        await CreateSpaceAsync(admin, "Secret Space");

        using var user = UserClient();
        var spaces = await user.GetFromJsonAsync<List<SpaceDto>>("/api/spaces");
        Assert.Empty(spaces!);
    }

    [Fact]
    public async Task NonAdmin_GetSpaces_SpaceGrant_SeesOnlyGrantedSpace()
    {
        await ResetCatalogAsync();
        using var admin = AdminClient();
        var visible = await CreateSpaceAsync(admin, "Visible");
        var hidden = await CreateSpaceAsync(admin, "Hidden");
        await GrantAsync("space", visible.Id, $"user.email = '{UserEmail}'");

        using var user = UserClient();
        var spaces = await user.GetFromJsonAsync<List<SpaceDto>>("/api/spaces");
        Assert.Equal(new[] { visible.Id }, spaces!.Select(s => s.Id).ToArray());
        Assert.DoesNotContain(spaces!, s => s.Id == hidden.Id);
    }

    [Fact]
    public async Task NonAdmin_GetFormsInSpace_SpaceGrant_SeesAllForms()
    {
        await ResetCatalogAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        await CreateFormAsync(admin, space.Id, "A");
        await CreateFormAsync(admin, space.Id, "B");
        await GrantAsync("space", space.Id, $"user.email = '{UserEmail}'");

        using var user = UserClient();
        var forms = await user.GetFromJsonAsync<List<FormDto>>($"/api/spaces/{space.Id}/forms");
        Assert.Equal(2, forms!.Count);
    }

    [Fact]
    public async Task NonAdmin_GetFormsInSpace_FormGrant_SeesOnlyThatForm()
    {
        await ResetCatalogAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var a = await CreateFormAsync(admin, space.Id, "A");
        await CreateFormAsync(admin, space.Id, "B");
        await GrantAsync("form", a.Id, $"user.email = '{UserEmail}'");

        using var user = UserClient();
        var forms = await user.GetFromJsonAsync<List<FormDto>>($"/api/spaces/{space.Id}/forms");
        Assert.Equal(new[] { a.Id }, forms!.Select(f => f.Id).ToArray());
    }
}
