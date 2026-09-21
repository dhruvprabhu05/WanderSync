# syntax=docker/dockerfile:1

# Build context is api/. Built by .github/workflows/deploy.yml and pushed to
# ghcr.io -- see docs/adr/0005-ghcr-instead-of-acr.md for why not ACR.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore before copying sources so a code-only change reuses the cached
# restore layer instead of re-downloading every package.
COPY Directory.Build.props ./
COPY src/WanderSync.Api/WanderSync.Api.csproj src/WanderSync.Api/
RUN dotnet restore src/WanderSync.Api/WanderSync.Api.csproj

COPY src/ src/
RUN dotnet publish src/WanderSync.Api/WanderSync.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# 8080 matches containerPort in infra/modules/api.bicep. Changing one without
# the other breaks the ingress and the health probes.
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

# Non-root. APP_UID is defined by the aspnet base image.
USER $APP_UID

ENTRYPOINT ["dotnet", "WanderSync.Api.dll"]
