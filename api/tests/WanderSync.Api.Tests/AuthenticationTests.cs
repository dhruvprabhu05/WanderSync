using System.Net;
using System.Net.Http.Headers;

namespace WanderSync.Api.Tests;

/// <summary>
/// The API's security boundary, asserted from the outside. Each rejection case
/// is a distinct way a real attacker or a buggy client could present a token.
/// </summary>
public sealed class AuthenticationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Me_returns_the_callers_subject_for_a_valid_token()
    {
        using var response = await SendAsync("/v1/me", ApiFactory.CreateToken(subject: "user-123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"subject\":\"user-123\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Me_rejects_a_request_with_no_token()
    {
        using var response = await SendAsync("/v1/me", token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_a_token_signed_with_an_untrusted_key()
    {
        var forged = ApiFactory.CreateToken(signingKey: ApiFactory.NewKey());

        using var response = await SendAsync("/v1/me", forged);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_an_expired_token()
    {
        var expired = ApiFactory.CreateToken(expires: DateTime.UtcNow.AddHours(-1));

        using var response = await SendAsync("/v1/me", expired);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_a_token_issued_for_a_different_api()
    {
        var wrongAudience = ApiFactory.CreateToken(audience: "api://some-other-api");

        using var response = await SendAsync("/v1/me", wrongAudience);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_a_token_from_a_different_issuer()
    {
        var wrongIssuer = ApiFactory.CreateToken(issuer: "https://attacker.example/");

        using var response = await SendAsync("/v1/me", wrongIssuer);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_forbids_a_valid_token_without_a_subject()
    {
        using var response = await SendAsync("/v1/me", ApiFactory.CreateToken(subject: null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_routes_require_authentication_before_revealing_404()
    {
        // Deny-by-default fallback policy: anonymous callers cannot probe which
        // routes exist.
        using var anonymous = await SendAsync("/v1/does-not-exist", token: null);
        using var authenticated = await SendAsync("/v1/does-not-exist", ApiFactory.CreateToken());

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, authenticated.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _client.SendAsync(request);
    }
}
