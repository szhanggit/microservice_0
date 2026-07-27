# syntax=docker/dockerfile:1

# Build context is the repository root (see docker-compose.yml), because this service
# depends on the libs/ shared projects and the root Directory.Build.props / Directory.Packages.props.

# This stage is used when running from VS in fast mode (Default for Debug configuration).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# curl is used only by the HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# This stage is used to build the service project.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./

# Copy only project files first so `dotnet restore` is cached until a .csproj actually changes.
COPY libs/Shared.Contracts/Shared.Contracts.csproj libs/Shared.Contracts/
COPY libs/Shared.Common/Shared.Common.csproj libs/Shared.Common/
COPY libs/Shared.Logging/Shared.Logging.csproj libs/Shared.Logging/
COPY libs/Shared.Protos/Shared.Protos.csproj libs/Shared.Protos/
COPY components/UserManagementGateway/src/private/UserManagementGateway/UserManagementGateway.csproj components/UserManagementGateway/src/private/UserManagementGateway/
RUN dotnet restore components/UserManagementGateway/src/private/UserManagementGateway/UserManagementGateway.csproj

COPY libs/ libs/
COPY components/UserManagementGateway/src/private/ components/UserManagementGateway/src/private/

# This stage is used to publish the service project to be copied to the final stage.
FROM build AS publish
RUN dotnet publish components/UserManagementGateway/src/private/UserManagementGateway/UserManagementGateway.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=5 \
    CMD curl --fail http://localhost:8080/health || exit 1

USER app
ENTRYPOINT ["dotnet", "UserManagementGateway.dll"]
