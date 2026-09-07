using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fms.Api.Test;

public class HealthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsServiceName()
    {
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/health");

        Assert.Contains("Fms.Api", body);
    }
}
