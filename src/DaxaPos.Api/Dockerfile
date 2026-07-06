# Local-dev-only image. Not tuned for production (no non-root user, no layer-cached restore,
# no trimming) — see PLAN-0010. Build context is the repo root (all projects are copied in
# because DaxaPos.Api references Application/Infrastructure/Persistence, which reference Domain).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/DaxaPos.Api/DaxaPos.Api.csproj
RUN dotnet publish src/DaxaPos.Api/DaxaPos.Api.csproj -c Release -o /app/publish --no-restore

# Runs `dotnet ef database update` as a one-shot Compose service (see compose.yaml's
# `migrations` service). Shares the `build` stage's already-restored source tree instead of
# restoring twice.
FROM build AS migrator
WORKDIR /src
RUN dotnet tool install --global dotnet-ef --version 10.0.9
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", \
    "--project", "src/DaxaPos.Persistence/DaxaPos.Persistence.csproj", \
    "--startup-project", "src/DaxaPos.Api/DaxaPos.Api.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DaxaPos.Api.dll"]
