# Local-dev-only image — see PLAN-0010. DaxaPos.Web is a standalone Blazor WebAssembly PWA: this
# builds the static output and serves it via nginx. Unlike src/DaxaPos.Api/Dockerfile and
# src/DaxaPos.Workers/Dockerfile, this container's config is NOT reached over the Docker network —
# it ships static files to the developer's browser, which then calls the API directly at whatever
# host-published address deploy/web/appsettings.docker.json points at (never the Compose service
# name `api`, which the browser cannot resolve).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/DaxaPos.Web/DaxaPos.Web.csproj
RUN dotnet publish src/DaxaPos.Web/DaxaPos.Web.csproj -c Release -o /app/publish --no-restore

FROM nginx:1.27-alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY deploy/web/appsettings.docker.json /usr/share/nginx/html/appsettings.json
COPY deploy/web/nginx.conf /etc/nginx/conf.d/default.conf
