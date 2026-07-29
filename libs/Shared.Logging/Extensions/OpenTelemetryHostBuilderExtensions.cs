using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Logging.Extensions;

public static class OpenTelemetryHostBuilderExtensions
{
    public static WebApplicationBuilder AddSharedOpenTelemetryTracing(
        this WebApplicationBuilder builder,
        string serviceName,
        Action<TracerProviderBuilder>? configureAdditionalInstrumentation = null)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                configureAdditionalInstrumentation?.Invoke(t);
                t.AddOtlpExporter();
            });

        return builder;
    }
}
