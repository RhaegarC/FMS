using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fms.Api.Test;

/// <summary>
/// Feature 01 (foundation): CORS must be wired from configuration
/// ("CORS:AllowedOrigins" — fed by the CORS__AllowedOrigins env var in compose /
/// the appsettings.Development.json default) so the single Figma app's origin
/// (http://localhost:8443) can call the API. The dev default lives in
/// appsettings.Development.json; compose overrides it for other origins.
/// </summary>
public class CorsEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetHealth_FromConfiguredOrigin_ReturnsAllowOriginHeader()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:8443");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Access-Control-Allow-Origin"), v => v == "http://localhost:8443");
    }

    [Fact]
    public async Task GetHealth_FromDisallowedOrigin_OmitsAllowOriginHeader()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("Origin", "http://evil.example");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
