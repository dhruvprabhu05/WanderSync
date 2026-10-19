using System.Net;

namespace WanderSync.Api.Tests;

/// <summary>
/// Boots the real application pipeline in memory and exercises it over HTTP.
/// The anonymous surface is deliberately tiny: health probes and ping.
/// </summary>
public sealed class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
}
