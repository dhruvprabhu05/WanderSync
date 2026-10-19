using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WanderSync.Api.Tests;

/// <summary>
/// The other suites swap the identity provider out for a test key. This one
/// boots the app with its real committed configuration (exactly what the
/// container starts with) and checks that validation is fully switched on.
/// It also proves the app actually starts under that configuration.
/// </summary>
public sealed class AuthConfigurationTests
{
    [Fact]
    public void Committed_configuration_validates_issuer_audience_lifetime_and_signature()
    {
        using var factory = new WebApplicationFactory<Program>();

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var tvp = options.TokenValidationParameters;

        Assert.StartsWith("https://", options.Authority, StringComparison.Ordinal);
        Assert.True(tvp.ValidateIssuer);
        Assert.NotEmpty(tvp.ValidIssuers!);
        Assert.True(tvp.ValidateAudience);
        Assert.NotEmpty(tvp.ValidAudiences!);
        Assert.True(tvp.ValidateLifetime);
        Assert.True(tvp.ValidateIssuerSigningKey);
    }
}
