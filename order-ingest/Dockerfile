# order-ingest/Dockerfile
# Solution to lesson 2, step 3 — a multi-stage build: build with the SDK, ship
# only the published output in the smaller ASP.NET runtime image.

# ---- build stage (has the full SDK) ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# ---- runtime stage (small — ASP.NET runtime only) ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderIngest.dll"]
