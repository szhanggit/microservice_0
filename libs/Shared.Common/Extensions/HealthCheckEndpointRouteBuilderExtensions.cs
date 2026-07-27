using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Shared.Common.Extensions;

public static class HealthCheckEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the conventional /health, /ready, and /live probe endpoints. Checks tagged "ready"
    /// (e.g. database connectivity) gate readiness; liveness never runs a check - the process
    /// responding at all is proof enough it is alive.
    /// </summary>
    public static IEndpointRouteBuilder MapSharedHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        endpoints.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        endpoints.MapHealthChecks("/live", new HealthCheckOptions { Predicate = _ => false });

        return endpoints;
    }
}
