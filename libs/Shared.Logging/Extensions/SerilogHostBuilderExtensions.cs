using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Shared.Logging.Enrichers;

namespace Shared.Logging.Extensions;

public static class SerilogHostBuilderExtensions
{
    public static WebApplicationBuilder AddSharedSerilogLogging(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.With<ActivityEnricher>()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({Service}) {CorrelationId} {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture));

        return builder;
    }
}
