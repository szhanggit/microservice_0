using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserRepositoryService.Persistence;

namespace UserRepositoryService.HealthChecks;

public sealed class DatabaseHealthCheck(UserRepositoryDbContext dbContext) : IHealthCheck
{
    public static readonly string[] ReadyTags = ["ready"];

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
}
