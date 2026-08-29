using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Fms.Api.Test;

/// <summary>
/// RED for feature 07 (submission APIs + export). Submissions are schema-validated
/// on submit, stored with form + submitting user, listed own-vs-all by role
/// (permission-scoped for non-admins via the feature-05 evaluator), searchable by
/// keyword/form/date, and exportable as Excel (.xlsx) + JSON with nested-data
/// flattening. Same self-issued RS256 JWT harness as SpaceFormCrudTests.
/// </summary>
public class SubmissionApiTests : IAsyncLifetime
{
    private const string Issuer = "https://fms.test";
    private const string Audience = "test-audience";
    private const string AdminUserIds = "admin-oid-123";
    private const string UserOid = "user-oid-456";
    private const string UserEmail = "alice@example.com";
    private const string OtherUserOid = "user-oid-789";
    private const string OtherUserEmail = "charlie@example.com";

    private const string ExpenseSchema =
        """{"type":"object","properties":{"amount":{"type":"number"},"vendor":{"type":"string"},"note":{"type":"string"}},"required":["amount","vendor"]}""";

    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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
    private HttpClient OtherUserClient() => ClientWithToken(CreateToken(OtherUserOid, OtherUserEmail));

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

    /// <summary>Inserts a grant expression directly (feature 05 has no web UI).</summary>
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

    /// <summary>Tests share one container; wipe submission/permission/catalog state
    /// so each test starts clean regardless of execution order.</summary>
    private async Task ResetDataAsync()
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
        HttpClient client, int spaceId, string name, string schema = ExpenseSchema)
    {
        var response = await client.PostAsJsonAsync($"/api/spaces/{spaceId}/forms", new { name, schema });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FormDto>())!;
    }

    private async Task<HttpResponseMessage> SubmitAsync(HttpClient client, int formId, string dataJson)
    {
        var payload = new StringContent($$"""{"data": {{dataJson}}}""", Encoding.UTF8, "application/json");
        return await client.PostAsync($"/api/forms/{formId}/submissions", payload);
    }

    private async Task<int> UserIdAsync(string oid)
    {
        var rows = await QueryAsync($"SELECT id FROM users WHERE entra_object_id = '{oid}'");
        Assert.Single(rows.Rows);
        return (int)rows.Rows[0][0]!;
    }

    /// <summary>Grants a form to the primary test user and returns it.</summary>
    private async Task<FormDto> CreateGrantedFormAsync()
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Expense Report");
        await GrantAsync("form", form.Id, $"user.email = '{UserEmail}'");
        return form;
    }

    /// <summary>Grants a form to several users via an IN expression and returns it.</summary>
    private async Task<FormDto> CreateFormGrantedToUsersAsync(params string[] emails)
    {
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Expense Report");
        var inList = string.Join(", ", emails.Select(e => $"'{e}'"));
        await GrantAsync("form", form.Id, $"user.email IN ({inList})");
        return form;
    }

    private sealed record SpaceDto(int Id, string Name);
    private sealed record FormDto(int Id, int SpaceId, string Name, string Schema, DateTimeOffset UpdatedAt);
    private sealed record SubmissionDto(int Id, int FormId, int UserId, string UserEmail, string Data, DateTimeOffset CreatedAt);

    // --- Submit: auth, permission, schema validation ------------------------

    [Fact]
    public async Task Anonymous_Submit_ReturnsUnauthorized()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var anon = _factory.CreateClient();
        var response = await SubmitAsync(anon, form.Id, """{"amount":10,"vendor":"Acme"}""");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_Submit_NoGrant_ReturnsForbidden()
    {
        await ResetDataAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Expense Report");

        using var user = UserClient();
        var response = await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Acme"}""");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_Submit_MissingForm_ReturnsNotFound()
    {
        await ResetDataAsync();
        using var user = UserClient();
        var response = await SubmitAsync(user, 999999, """{"amount":10,"vendor":"Acme"}""");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task User_Submit_GrantedForm_ReturnsCreatedAndPersists()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        var response = await SubmitAsync(user, form.Id, """{"amount":42.5,"vendor":"Acme","note":"lunch"}""");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var submission = (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
        Assert.True(submission.Id > 0);
        Assert.Equal(form.Id, submission.FormId);
        Assert.Equal(UserEmail, submission.UserEmail);

        var userId = await UserIdAsync(UserOid);
        var rows = await QueryAsync(
            $"SELECT form_id, user_id, data, created_on FROM submissions WHERE id = {submission.Id}");
        Assert.Single(rows.Rows);
        Assert.Equal(form.Id, (int)rows.Rows[0][0]!);
        Assert.Equal(userId, (int)rows.Rows[0][1]!);
        Assert.Contains("Acme", rows.Rows[0][2]!.ToString());
        Assert.NotEqual(DBNull.Value, rows.Rows[0][3]);
    }

    [Fact]
    public async Task User_Submit_InvalidData_ReturnsBadRequest()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        var response = await SubmitAsync(user, form.Id, """{"amount":"lots","vendor":"Acme"}""");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_Submit_MissingRequiredField_ReturnsBadRequest()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        var response = await SubmitAsync(user, form.Id, """{"amount":10}"""); // vendor required
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_Submit_NonObjectData_ReturnsBadRequest()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        var response = await SubmitAsync(user, form.Id, """[1,2,3]""");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Submit_Works_NoGrantNeeded()
    {
        await ResetDataAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var form = await CreateFormAsync(admin, space.Id, "Expense Report");

        var response = await SubmitAsync(admin, form.Id, """{"amount":10,"vendor":"Acme"}""");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- List: own (user) vs all (admin), permission-scoped -----------------

    [Fact]
    public async Task NonAdmin_GetAllSubmissions_ReturnsForbidden()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();
        using var user = UserClient();
        await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Acme"}""");

        var response = await user.GetAsync("/api/submissions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_GetMeSubmissions_NoGrant_ReturnsEmpty()
    {
        await ResetDataAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        await CreateFormAsync(admin, space.Id, "Secret Form");

        using var user = UserClient();
        var list = await user.GetFromJsonAsync<List<SubmissionDto>>("/api/me/submissions");
        Assert.Empty(list!);
    }

    [Fact]
    public async Task User_GetMeSubmissions_ReturnsOnlyOwnOnGrantedForms()
    {
        await ResetDataAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var granted = await CreateFormAsync(admin, space.Id, "Granted");
        var ungranted = await CreateFormAsync(admin, space.Id, "Ungranted");
        await GrantAsync("form", granted.Id, $"user.email = '{UserEmail}'");

        // User submits to the granted form.
        using var user = UserClient();
        var userSubmit = await SubmitAsync(user, granted.Id, """{"amount":10,"vendor":"Alice Shop"}""");
        Assert.Equal(HttpStatusCode.Created, userSubmit.StatusCode);

        // Admin submits to both forms (these belong to the admin, not the user).
        await SubmitAsync(admin, granted.Id, """{"amount":20,"vendor":"Admin Shop"}""");
        await SubmitAsync(admin, ungranted.Id, """{"amount":99,"vendor":"Hidden"}""");

        var list = await user.GetFromJsonAsync<List<SubmissionDto>>("/api/me/submissions");
        var userOnly = list!.Single();
        Assert.Equal(granted.Id, userOnly.FormId);
        Assert.Equal(UserEmail, userOnly.UserEmail);
        Assert.Contains("Alice Shop", userOnly.Data);
    }

    [Fact]
    public async Task Admin_GetAllSubmissions_ReturnsAllAcrossUsersAndForms()
    {
        await ResetDataAsync();
        var form = await CreateFormGrantedToUsersAsync(UserEmail, OtherUserEmail);

        using var user = UserClient();
        using var other = OtherUserClient();
        var userSubmit = await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Acme"}""");
        var otherSubmit = await SubmitAsync(other, form.Id, """{"amount":20,"vendor":"Charlie"}""");
        Assert.Equal(HttpStatusCode.Created, userSubmit.StatusCode);
        Assert.Equal(HttpStatusCode.Created, otherSubmit.StatusCode);

        using var admin = AdminClient();
        var list = await admin.GetFromJsonAsync<List<SubmissionDto>>("/api/submissions");
        Assert.Equal(2, list!.Count);
        Assert.Contains(list, s => s.UserEmail == UserEmail);
        Assert.Contains(list, s => s.UserEmail == OtherUserEmail);
    }

    // --- Search: keyword + form + date filters ------------------------------

    [Fact]
    public async Task User_GetMeSubmissions_KeywordSearch_Filters()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        await SubmitAsync(user, form.Id, """{"amount":500,"vendor":"Acme","note":"travel to Tokyo"}""");
        await SubmitAsync(user, form.Id, """{"amount":50,"vendor":"Acme","note":"local coffee"}""");

        var match = await user.GetFromJsonAsync<List<SubmissionDto>>("/api/me/submissions?q=tokyo");
        var only = match!.Single();
        Assert.Contains("travel to Tokyo", only.Data);

        var none = await user.GetFromJsonAsync<List<SubmissionDto>>("/api/me/submissions?q=zzznotfound");
        Assert.Empty(none!);
    }

    [Fact]
    public async Task User_GetMeSubmissions_FormFilter_Filters()
    {
        await ResetDataAsync();
        using var admin = AdminClient();
        var space = await CreateSpaceAsync(admin, "Finance");
        var formA = await CreateFormAsync(admin, space.Id, "Form A");
        var formB = await CreateFormAsync(admin, space.Id, "Form B");
        await GrantAsync("form", formA.Id, $"user.email = '{UserEmail}'");
        await GrantAsync("form", formB.Id, $"user.email = '{UserEmail}'");

        using var user = UserClient();
        await SubmitAsync(user, formA.Id, """{"amount":1,"vendor":"Acme"}""");
        await SubmitAsync(user, formB.Id, """{"amount":2,"vendor":"Acme"}""");

        var a = await user.GetFromJsonAsync<List<SubmissionDto>>($"/api/me/submissions?formId={formA.Id}");
        var onlyA = a!.Single();
        Assert.Equal(formA.Id, onlyA.FormId);
    }

    [Fact]
    public async Task User_GetMeSubmissions_DateRange_Filters()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        var response = await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Acme"}""");
        var submission = (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
        // Pin the stored timestamp so the range query is deterministic.
        await QueryAsync($"UPDATE submissions SET created_on = '2026-01-15 00:00:00+00' WHERE id = {submission.Id}");

        var inside = await user.GetFromJsonAsync<List<SubmissionDto>>(
            "/api/me/submissions?from=2026-01-01&to=2026-01-31");
        Assert.Equal(submission.Id, inside!.Single().Id);

        var outside = await user.GetFromJsonAsync<List<SubmissionDto>>(
            "/api/me/submissions?from=2026-02-01");
        Assert.Empty(outside!);
    }

    // --- Export: Excel (.xlsx) + JSON ---------------------------------------

    [Fact]
    public async Task Admin_ExportJson_ReturnsAllAsJson()
    {
        await ResetDataAsync();
        var form = await CreateFormGrantedToUsersAsync(UserEmail, OtherUserEmail);

        using var user = UserClient();
        await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Acme"}""");
        using var other = OtherUserClient();
        await SubmitAsync(other, form.Id, """{"amount":20,"vendor":"Charlie"}""");

        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/submissions/export?format=json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);

        var list = await response.Content.ReadFromJsonAsync<List<SubmissionDto>>();
        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task Admin_ExportXlsx_ReturnsWorkbook()
    {
        await ResetDataAsync();
        var form = await CreateGrantedFormAsync();

        using var user = UserClient();
        await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Acme"}""");

        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/submissions/export?format=xlsx");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(XlsxContentType, response.Content.Headers.ContentType!.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        // xlsx is a zip: magic bytes "PK".
        Assert.Equal('P', (char)bytes[0]);
        Assert.Equal('K', (char)bytes[1]);
    }

    [Fact]
    public async Task Admin_Export_InvalidFormat_ReturnsBadRequest()
    {
        await ResetDataAsync();
        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/submissions/export?format=csv");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_ExportAll_ReturnsForbidden()
    {
        await ResetDataAsync();
        using var user = UserClient();
        var response = await user.GetAsync("/api/submissions/export?format=json");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_ExportJson_ScopedToOwnOnGrantedForms()
    {
        await ResetDataAsync();
        // Both users can submit, so the user export returning exactly one row proves
        // the scope is the caller's own submissions, not the whole form's.
        var form = await CreateFormGrantedToUsersAsync(UserEmail, OtherUserEmail);

        using var user = UserClient();
        var userSubmit = await SubmitAsync(user, form.Id, """{"amount":10,"vendor":"Alice Shop"}""");
        Assert.Equal(HttpStatusCode.Created, userSubmit.StatusCode);
        using var other = OtherUserClient();
        var otherSubmit = await SubmitAsync(other, form.Id, """{"amount":99,"vendor":"Charlie"}""");
        Assert.Equal(HttpStatusCode.Created, otherSubmit.StatusCode);

        var response = await user.GetAsync("/api/me/submissions/export?format=json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<SubmissionDto>>();
        var only = list!.Single();
        Assert.Equal(UserEmail, only.UserEmail);
        Assert.Contains("Alice Shop", only.Data);
    }
}
