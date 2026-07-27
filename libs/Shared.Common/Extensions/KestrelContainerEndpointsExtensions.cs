using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Shared.Common.Extensions;

public static class KestrelContainerEndpointsExtensions
{
    /// <summary>
    /// Kestrel can't multiplex HTTP/1.1 and HTTP/2 on one cleartext (non-TLS) port - that requires
    /// TLS + ALPN. Containers run without TLS (it terminates at the edge), so a gRPC-hosting service
    /// needs two separate ports: one HTTP/2-only for gRPC calls, one HTTP/1.1-only for health probes.
    /// Local `dotnet run` is unaffected - it uses the HTTPS launch profile, where ALPN already
    /// negotiates both protocols on a single port.
    /// </summary>
    public static WebApplicationBuilder ConfigureContainerGrpcAndHealthEndpoints(
        this WebApplicationBuilder builder, int grpcPort = 8080, int healthPort = 8081)
    {
        var isContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!isContainer)
        {
            return builder;
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(grpcPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
            options.ListenAnyIP(healthPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
        });

        return builder;
    }
}
