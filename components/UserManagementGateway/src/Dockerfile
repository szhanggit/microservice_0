# syntax=docker/dockerfile:1

# Build context is the repository root (see docker-compose.yml), because this service
# depends on the libs/ shared projects and the root Directory.Build.props / Directory.Packages.props.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./

# Copy only project files first so `dotnet restore` is cached until a .csproj actually changes.
COPY libs/Shared.Contracts/Shared.Contracts.csproj libs/Shared.Contracts/
COPY libs/Shared.Common/Shared.Common.csproj libs/Shared.Common/
COPY libs/Shared.Logging/Shared.Logging.csproj libs/Shared.Logging/
COPY libs/Shared.Protos/Shared.Protos.csproj libs/Shared.Protos/
COPY components/UserManagementGateway/src/UserManagementGateway.csproj components/UserManagementGateway/src/
RUN dotnet restore components/UserManagementGateway/src/UserManagementGateway.csproj

COPY libs/ libs/
COPY components/UserManagementGateway/src/ components/UserManagementGateway/src/
RUN dotnet publish components/UserManagementGateway/src/UserManagementGateway.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is used only by the HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=5 \
    CMD curl --fail http://localhost:8080/health || exit 1

USER app
ENTRYPOINT ["dotnet", "UserManagementGateway.dll"]
