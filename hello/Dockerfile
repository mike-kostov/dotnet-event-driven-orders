# hello/Dockerfile
# Solution to lesson 1, step 4. Builds the tiny app into an image.

# 4.1 — base image with the .NET 10 SDK (gives us the `dotnet` tooling)
FROM mcr.microsoft.com/dotnet/sdk:10.0

# 4.2 — work inside /app
WORKDIR /app

# 4.3 — copy the source in and publish a Release build to /out
COPY . .
RUN dotnet publish -c Release -o /out

# 4.4 — run the published app when the container starts
ENTRYPOINT ["dotnet", "/out/hello.dll"]
