using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Middleware;

namespace Shared.Common.Extensions;

public static class ExceptionHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddSharedExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
