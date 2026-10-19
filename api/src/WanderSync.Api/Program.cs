using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    // "live" answers: is the process up? It must not depend on anything
    // external, or a database blip will make the platform kill the container.
    // "ready" will later add dependency checks (Postgres) and is what a load
    // balancer should gate traffic on.
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

// ---------------------------------------------------------------------------
// Authentication: plain OIDC JWT bearer validation, issuer-agnostic.
//
// AddJwtBearer() binds from the "Authentication:Schemes:Bearer" config section
// (Authority, ValidAudiences, ValidIssuers). Nothing below names Entra: the
// identity provider is a configuration value, not a code dependency. See
// docs/adr/0007-identity-provider.md.
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names as the issuer sent them ("sub", not the legacy
        // http://schemas.xmlsoap.org/... URIs), so code reads the token's
        // actual contract.
        options.MapInboundClaims = false;
    });

// Fail at startup, not at the first request, if auth is not configured. A
// container that boots without an authority would otherwise pass its health
// probes and then reject every authenticated call.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.Authority) || o.TokenValidationParameters.IssuerSigningKey is not null,
        "Authentication:Schemes:Bearer:Authority is not configured.")
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.TokenValidationParameters.ValidAudience)
             || o.TokenValidationParameters.ValidAudiences?.Any() == true,
        "Authentication:Schemes:Bearer:ValidAudiences is not configured.")
    .ValidateOnStart();

// Deny by default. Every endpoint requires an authenticated caller with a
// subject claim unless it explicitly opts out with AllowAnonymous(). Forgetting
// an attribute therefore fails closed, which is the failure mode you want.
var authenticatedUser = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .RequireClaim(ApiClaims.Subject)
    .Build();

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(authenticatedUser)
    .SetFallbackPolicy(authenticatedUser);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// Deliberately no UseHttpsRedirection(). TLS terminates at the Container Apps
// ingress, which forwards plain HTTP to the container on 8080. Redirecting here
// would bounce the platform's own health probes and can produce a redirect loop.

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
}).AllowAnonymous();

app.MapHealthChecks("/health/ready").AllowAnonymous();

var v1 = app.MapGroup("/v1");

v1.MapGet("/ping", () => Results.Ok(new PingResponse("ok", DateTimeOffset.UtcNow)))
  .AllowAnonymous()
  .WithName("Ping")
  .WithSummary("Unauthenticated reachability check. Confirms the deployed build is up.");

v1.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new MeResponse(
        // The authorization policy guarantees this claim exists.
        Subject: user.FindFirstValue(ApiClaims.Subject)!,
        Name: user.FindFirstValue(ApiClaims.Name),
        Email: user.FindFirstValue(ApiClaims.Email))))
  .WithName("GetMe")
  .WithSummary("Returns the identity the API sees in the caller's access token.");

app.Run();

internal static class ApiClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
}

internal sealed record PingResponse(string Status, DateTimeOffset Utc);

internal sealed record MeResponse(string Subject, string? Name, string? Email);

// Exposed so WebApplicationFactory<Program> can boot the real pipeline in tests
// rather than a reconstruction of it. Top-level statements otherwise generate an
// internal Program class that the test project cannot reference.
public partial class Program
{
}
