using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fms.Api.Test;

/// <summary>
/// RED regression test for bug 01 (swagger-root-404): navigating to the API's base
/// URL (<c>http://localhost:5149</c>) must serve the Swagger UI instead of a 404.
/// Swagger is dev-only, so the WebApplicationFactory runs in the Development
/// environment (the MvcTesting default) exactly like SwaggerSpecTests.
/// </summary>
public class SwaggerUiRootTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetRoot_ServesSwaggerUiInDevelopment()
    {
        var client = _factory.CreateClient();

        // The UI serves at the root (CreateClient follows the / -> /index.html redirect,
        // exactly like a browser), so the original 404 at / is caught here.
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", body);

        // The UI loads its endpoint config from index.js. With RoutePrefix = "" the config
        // must reference the absolute /swagger/v1/swagger.json — a relative "v1/swagger.json"
        // would make the browser fetch /v1/swagger.json, which 404s (the fetch error seen in
        // the follow-up report of bug 01).
        var config = await client.GetStringAsync("/index.js");
        Assert.Contains("/swagger/v1/swagger.json", config);

        // And the spec must actually serve at that canonical path.
        var spec = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, spec.StatusCode);
    }
}
