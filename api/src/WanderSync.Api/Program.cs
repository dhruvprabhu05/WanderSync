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

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Deliberately no UseHttpsRedirection(). TLS terminates at the Container Apps
// ingress, which forwards plain HTTP to the container on 8080. Redirecting here
// would bounce the platform's own health probes and can produce a redirect loop.

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready");

app.MapGet("/v1/ping", () => Results.Ok(new PingResponse("ok", DateTimeOffset.UtcNow)))
   .WithName("Ping")
   .WithSummary("Liveness probe for humans. Confirms the deployed build is reachable.");

app.Run();

internal sealed record PingResponse(string Status, DateTimeOffset Utc);

// Exposed so WebApplicationFactory<Program> can boot the real pipeline in tests
// rather than a reconstruction of it. Top-level statements otherwise generate an
// internal Program class that the test project cannot reference.
public partial class Program
{
}
