using Microsoft.AspNetCore.Builder;
using Shared.Logging.Correlation;

namespace Shared.Logging.Extensions;

public static class CorrelationIdApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
