# order-processor/Dockerfile
# Multi-stage build — the same pattern you wrote in lesson 2 (given here, since
# Dockerfiles aren't the focus of this lesson; the consumer is).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "OrderProcessor.dll"]
