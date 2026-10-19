using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace WanderSync.Api.Tests;

/// <summary>
/// Boots the real pipeline with one substitution: instead of fetching signing
/// keys from an OIDC authority, the bearer handler trusts a key generated per
/// test run. Every other part of token validation (issuer, audience, lifetime,
/// signature) runs exactly as it does in production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://issuer.wandersync.test/";
    public const string Audience = "api://wandersync-tests";

    private static readonly SymmetricSecurityKey SigningKey = NewKey();

    public static SymmetricSecurityKey NewKey() => new(RandomNumberGenerator.GetBytes(32));

    public static string CreateToken(
        string? subject = "user-123",
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null,
        SecurityKey? signingKey = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(10);

        var claims = new Dictionary<string, object> { ["name"] = "Test User" };
        if (subject is not null)
        {
            claims["sub"] = subject;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = expiry.AddMinutes(-15),
            NotBefore = expiry.AddMinutes(-15),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = SigningKey,
                };
            }));
    }
}
