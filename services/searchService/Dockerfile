# -----------------------------------------
# BUILD STAGE
# -----------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY searchService.sln ./

COPY Search.Api/Search.Api.csproj Search.Api/
COPY Search.Worker/Search.Worker.csproj Search.Worker/
COPY Search.Infrastructure/Search.Infrastructure.csproj Search.Infrastructure/
COPY Search.Application/Search.Application.csproj Search.Application/
COPY Search.Models/Search.Models.csproj Search.Models/

RUN dotnet restore searchService.sln

# Copy full source
COPY . .

# Publish API
RUN dotnet publish Search.Api/Search.Api.csproj -c Release -o /app/api \
    /p:UseAppHost=false

# Publish Worker
RUN dotnet publish Search.Worker/Search.Worker.csproj -c Release -o /app/worker \
    /p:UseAppHost=false

# -----------------------------------------
# API RUNTIME IMAGE
# -----------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS search-api
WORKDIR /app
COPY --from=build /app/api ./
ENTRYPOINT ["dotnet", "Search.Api.dll"]

# -----------------------------------------
# WORKER RUNTIME IMAGE (use ASP.NET runtime so Microsoft.AspNetCore.App is present)
# -----------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS search-worker
WORKDIR /app

# Copy worker publish output into this image
COPY --from=build /app/worker ./

ENV METRICS_PORT=9184

ENTRYPOINT ["dotnet", "Search.Worker.dll"]