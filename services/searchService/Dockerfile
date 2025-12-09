# services/searchService/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for caching
COPY searchService.sln ./

COPY Search.Api/Search.Api.csproj Search.Api/
COPY Search.Application/Search.Application.csproj Search.Application/
COPY Search.Infrastructure/Search.Infrastructure.csproj Search.Infrastructure/
COPY Search.Worker/Search.Worker.csproj Search.Worker/
COPY Search.Models/Search.Models.csproj Search.Models/

# Restore using the solution (uses copied csproj layout)
RUN dotnet restore "searchService.sln"

# Copy the rest of the service source
COPY . .

# Publish the API project
WORKDIR /src/Search.Api
RUN dotnet publish "Search.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:TrimUnusedDependencies=true

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# optional: run as non-root (uncomment if you add chown steps)
# RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
# USER appuser

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "Search.Api.dll"]
