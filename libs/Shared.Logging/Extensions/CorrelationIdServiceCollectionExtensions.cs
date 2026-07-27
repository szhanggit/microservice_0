using Microsoft.Extensions.DependencyInjection;
using Shared.Logging.Correlation;

namespace Shared.Logging.Extensions;

public static class CorrelationIdServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelationId(this IServiceCollection services) =>
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
}
