FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy everything first (so restore can read all project references)
COPY . .

# restore solution (ensure all packages for all projects are pulled)
RUN dotnet restore services/warehouseService/warehouseService.sln

# publish API and Worker
RUN dotnet publish services/warehouseService/Warehouse.Api/Warehouse.Api.csproj -c Release -o /app/api
RUN dotnet publish services/warehouseService/Warehouse.Worker/Warehouse.Worker.csproj -c Release -o /app/worker

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api ./api
COPY --from=build /app/worker ./worker

# no ENTRYPOINT here (set command in docker-compose)
