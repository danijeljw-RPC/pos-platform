# Local-dev-only image — see PLAN-0010. Build context is the repo root (same reason as
# src/DaxaPos.Api/Dockerfile: DaxaPos.Workers references Application/Infrastructure/Persistence).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/DaxaPos.Workers/DaxaPos.Workers.csproj
RUN dotnet publish src/DaxaPos.Workers/DaxaPos.Workers.csproj -c Release -o /app/publish --no-restore

# DaxaPos.Infrastructure carries an explicit FrameworkReference to Microsoft.AspNetCore.App
# (see src/DaxaPos.Infrastructure/DaxaPos.Infrastructure.csproj), so Workers needs the ASP.NET
# Core shared runtime, not just the base .NET runtime, even though it hosts no HTTP endpoints.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DaxaPos.Workers.dll"]
