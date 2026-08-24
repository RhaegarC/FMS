using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fms.Tests;

/// <summary>
/// RED for feature 01 (openapi-contract): the backend must serve an OpenAPI spec
/// that both React apps generate their typed API client from.
/// </summary>
public class SwaggerSpecTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task SwaggerSpec_EmitsOpenApiDocument()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerSpec_DescribesHealthPath()
    {
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("openapi", out _), "spec should declare an 'openapi' version");
        Assert.True(
            root.GetProperty("paths").TryGetProperty("/health", out _),
            "spec should describe the /health path");
    }

    [Fact]
    public async Task SwaggerSpec_DescribesSpaceAndFormCatalogPaths()
    {
        // Feature 06: the space/form catalog CRUD endpoints must be part of the
        // contract so openapi-typescript can regenerate the typed client.
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var doc = JsonDocument.Parse(body);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in new[]
                 {
                     "/api/spaces",
                     "/api/spaces/{id}",
                     "/api/spaces/{spaceId}/forms",
                     "/api/forms/{id}",
                 })
        {
            Assert.True(paths.TryGetProperty(path, out _), $"spec should describe the {path} path");
        }
    }
}
