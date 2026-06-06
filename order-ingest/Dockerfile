# order-ingest/Dockerfile
# ===========================================================================
# Lesson 2, step 3 — a MULTI-STAGE build. In lesson 1 you used one stage (the
# SDK image was also the final image — big). Here we use TWO stages: build with
# the SDK, then copy only the published output into a small runtime image. The
# final image ships without the SDK. Replace each TODO(you). Hints in LESSON.md.
# ===========================================================================

# ---- build stage (has the full SDK) ----
# TODO(you) 3.1 — start FROM the .NET 9 SDK image and name this stage `build`.
#   hint: FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
# TODO(you) 3.2 — publish a Release build into /app.
#   hint: RUN dotnet publish -c Release -o /app

# ---- runtime stage (small — ASP.NET runtime only) ----
# TODO(you) 3.3 — start a new, smaller stage FROM the ASP.NET 9 runtime image.
#   hint: FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# TODO(you) 3.4 — copy the published output from the build stage into this image.
#   hint: COPY --from=build /app .
EXPOSE 8080
# TODO(you) 3.5 — run the app when the container starts.
#   hint: ENTRYPOINT ["dotnet", "OrderIngest.dll"]
