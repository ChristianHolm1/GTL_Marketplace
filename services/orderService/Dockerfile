FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy everything first (so restore can read all project references)
COPY . .

# restore solution (ensure all packages for all projects are pulled)
RUN dotnet restore services/orderService/orderService.sln

# publish API
RUN dotnet publish services/orderService/Order.Api/Order.Api.csproj -c Release -o /app/api

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api ./api

# no ENTRYPOINT here (set command in docker-compose)
