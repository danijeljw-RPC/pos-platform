# PLAN-0010 — Docker Compose Local Dev Stack

## Status

Complete.

## Goal

Let a developer run `docker compose up -d --build` from the repo root and get a fully
containerized local stack (db, keycloak, migrations, api, worker, web) for manual testing,
without changing production behavior or adding product features.

This is the infra follow-up PLAN-0002 explicitly deferred: "`DaxaPos.Workers`, reverse proxy,
API Dockerfile/container — later infra plan, once something needs them." PLAN-0006 Milestone A
adding `DaxaPos.Web` is what now needs it (manual end-to-end testing of the PWA against a
containerized API without hand-seeding ports/CORS every time).

## Scope

- Dockerfile for `DaxaPos.Api` (multi-stage: build, `migrator` target, runtime).
- Dockerfile for `DaxaPos.Workers`.
- Root `compose.yaml` running `db`, `keycloak`, `migrations`, `api`, `worker`, `web`.
- Root `.env.example` (mirrors `deploy/.env.example`, dev-only defaults).
- `.dockerignore` so the build context doesn't ship `.git`/`bin`/`obj`.
- Docker-specific Blazor `appsettings.json` override + nginx conf for serving the published
  Blazor WebAssembly output.
- Docs: `docker.md` / `docker-deployment.md` updated to reflect the new root stack.

## Non-goals

- PLAN-0006 Milestone B (Back Office, device PIN generation UI).
- Any new UI, sales/payment/receipt/KDS/customer-display work.
- PLAN-0009 (Stripe Terminal).
- OI-0018 (production printer routing).
- New EF Core migrations.
- Reverse proxy / TLS (out of scope per the "local-dev only" instruction — HTTP only, matching
  the existing `dotnet run` dev workflow's plain-HTTP CORS path).
- Changing `deploy/docker-compose.yml` (the existing PLAN-0002 infra-only db+keycloak file) —
  left as-is; the new root `compose.yaml` is the fuller app-layer stack.

## Context Read

- `CLAUDE.md`
- `docs/deployment/docker.md`, `docs/deployment/docker-deployment.md`
- `docs/plans/active/PLAN-0002-platform-skeleton.md` (source of the deferred-work note above)
- `docs/plans/active/PLAN-0006-worker-notes.md` (Milestone A report — API ports/CORS/appsettings
  assumptions the container stack must not silently break)
- `deploy/docker-compose.yml`, `deploy/.env.example`

## Files Likely To Change

```
.dockerignore
compose.yaml
.env.example
src/DaxaPos.Api/Dockerfile
src/DaxaPos.Workers/Dockerfile
deploy/web/nginx.conf
deploy/web/appsettings.docker.json
docs/deployment/docker.md
docs/deployment/docker-deployment.md
```

## Architecture Assumptions

- Server-side services (`api`, `worker`, `migrations`) use the Docker service name `db` in their
  connection string — never `localhost` — since they run inside the Compose network.
- The Blazor WebAssembly `web` container is different: it ships static files to the *browser*,
  which runs on the host, not inside the Docker network. Its `ApiBaseUrl` must be a
  **host-published** address (`http://localhost:5118/`), never the internal service name `api` —
  the browser cannot resolve Compose service names.
- A distinct `ASPNETCORE_ENVIRONMENT=Docker` (not `Development`) is used for `api`/`worker` in
  Compose, so `appsettings.Development.json` (tuned for the unrelated `dotnet run` dev-port CORS
  origins, 5013/7025) doesn't leak into the container config. All container-specific config
  (connection string, CORS origin) is supplied explicitly via Compose environment variables.
- Migrations run as a one-shot `migrations` service (same `Dockerfile` as `api`, different build
  `target`), gated with `depends_on: db (healthy)`; `api`/`worker` in turn `depends_on: migrations
  (completed successfully)`.
- No reverse proxy / TLS — everything is plain HTTP on host-published ports, matching the
  project's documented "local-dev only" instruction for this task.

## Domain Assumptions

None — no business logic touched.

## Risks

- Docker Compose service-name-vs-localhost confusion is the single most likely thing to break
  silently (a working `api`/`worker` but a `web` that can't reach it, or vice versa) — mitigated
  by the explicit split above and by an end-to-end `docker compose up -d --build` + curl +
  container-to-container smoke test before reporting done.
- `dotnet-ef` isn't installed in the base SDK image; the `migrator` stage installs it explicitly.

## Tests To Run

- `docker compose config` (validates the compose file parses).
- `docker compose up -d --build`, then `docker compose ps` — all services healthy/exited(0).
- `docker compose logs migrations` — confirms `dotnet ef database update` ran and exited 0.
- `curl http://localhost:5118/health` — `Healthy`.
- `curl http://localhost:8080/` — Blazor `index.html` served.
- `docker compose down -v --remove-orphans` then repeat `up -d --build` — confirms fresh-start
  path works, not just an already-migrated volume.
- `dotnet build DaxaPos.sln` / `dotnet test DaxaPos.sln` unaffected (no source code changed).

## Documentation To Update

- `docs/deployment/docker.md`
- `docs/deployment/docker-deployment.md`

## Commit Sequence

```
infra: add API and Workers Dockerfiles for local Docker Compose stack
infra: add root compose.yaml running db/keycloak/migrations/api/worker/web
docs: document the root Docker Compose local dev stack
```

## Handoff Notes

The root `compose.yaml` is the full app-layer local dev stack; `deploy/docker-compose.yml`
remains the older infra-only (db+keycloak) file from PLAN-0002 and was not touched. If a future
plan wants to consolidate these into one file/workflow, do that as its own scoped change (with an
ADR if it changes the documented quick-start commands), not as a silent side effect of another
plan.

Deliberately deferred (do not fold into a "small follow-up" without a plan refresh):

- Reverse proxy / TLS termination for the Docker stack.
- Any seeding beyond the existing `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`_PASSWORD` bootstrap-admin path —
  there is still no way to reach the PWA's Staff PIN login without manually calling the admin API
  to create a location/device-registration-PIN/staff-member (see the reply to the previous
  "inspect Docker Compose" prompt in this session for the exact curl sequence). Automating that
  (e.g. a `seed-dev-data` Compose service) is feature-shaped work, not infra wiring, and is out of
  scope here.
