using Microsoft.AspNetCore.Builder;

namespace Shared.Logging.Extensions;

public static class XRayApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSharedXRayTracing(this IApplicationBuilder app, string serviceName) =>
        app.UseXRay(serviceName);
}
