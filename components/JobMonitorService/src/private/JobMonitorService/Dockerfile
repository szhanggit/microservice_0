# syntax=docker/dockerfile:1

# Build context is the repository root (see docker-compose.yml pattern used by
# the other components), because this service depends on the root
# Directory.Build.props / Directory.Packages.props. Unlike the other three
# services, it has no libs/ dependency - it's a standalone batch job, not part
# of the gRPC service mesh.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./

COPY components/JobMonitorService/src/private/JobMonitorService/JobMonitorService.csproj components/JobMonitorService/src/private/JobMonitorService/
RUN dotnet restore components/JobMonitorService/src/private/JobMonitorService/JobMonitorService.csproj

COPY components/JobMonitorService/src/private/ components/JobMonitorService/src/private/

FROM build AS publish
RUN dotnet publish components/JobMonitorService/src/private/JobMonitorService/JobMonitorService.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime-only base image (no ASP.NET Core needed) - this container has no
# HTTP/gRPC endpoints and no HEALTHCHECK, since it's a Kubernetes CronJob that
# simply runs to completion and exits rather than a long-running process.
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER app
ENTRYPOINT ["dotnet", "JobMonitorService.dll"]
