using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WanderSync.Api.Tests;

/// <summary>
/// Boots the real application pipeline in memory and exercises it over HTTP.
/// This is the harness the authorization suite will extend in Phase 3, where
/// every trip-scoped endpoint gets a test asserting 403 for a non-member.
/// </summary>
public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_return_ok(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_reports_ok()
    {
        using var response = await _client.GetAsync("/v1/ping");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_route_returns_not_found()
    {
        using var response = await _client.GetAsync("/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
