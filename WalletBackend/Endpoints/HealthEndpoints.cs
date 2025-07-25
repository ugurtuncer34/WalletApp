using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace WalletBackend.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/alive", () => Results.Ok("I’m alive")).AllowAnonymous(); // liveness

        app.MapHealthChecks("/health", new HealthCheckOptions //readiness
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        app.MapHealthChecksUI(options => // dashboard
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-json";
        });
    }
}