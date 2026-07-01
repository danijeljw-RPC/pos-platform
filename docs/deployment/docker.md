# Deployment: Docker and Docker Compose

Daxa POS uses Docker and Docker Compose for local development and on-premises (Daxa Local) deployment.

See [ADR-0012](../adr/accepted/ADR-0012-docker-local-deployment-strategy.md) for the decision record.

For the full detailed deployment reference, see [docker-deployment.md](docker-deployment.md) — this file contains the comprehensive Docker deployment guide including all environment variables, volumes, ports, health checks, backup procedures, and worker configuration.

---

## Current Skeleton Status (PLAN-0002)

As of PLAN-0002 (platform skeleton), `deploy/docker-compose.yml` runs **only** `db` (PostgreSQL) and `keycloak`. There is no `api`, `worker`, or `proxy` service yet, and no `Dockerfile` for `DaxaPos.Api` — the API runs directly on the host via `dotnet run --project src/DaxaPos.Api` against the Compose-provided `db`. Keycloak is present but not wired into any application code yet (see ADR-0013 and PLAN-0003); the API starts and reports healthy whether or not the `keycloak` container is running.

Quick start for this stage:

```bash
cd deploy
cp .env.example .env
# Edit .env with your local values
docker compose up -d db          # keycloak is optional: add "keycloak" to also start it
cd ..
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet run --project src/DaxaPos.Api
```

Health check:

```bash
curl http://localhost:5299/health
```

(Port depends on the `ASPNETCORE_URLS` the API binds to when run directly; there is no reverse proxy in front of it yet.)

The `api`/`worker`/`proxy` containerised stack described below and in [docker-deployment.md](docker-deployment.md) is the target design for later plans, not what exists today.

---

## Target Docker Compose Services (future plans)

| Service | Purpose | Status |
|---------|---------|--------|
| `db` | PostgreSQL database | Implemented (PLAN-0002) |
| `keycloak` | Identity provider (cloud/admin/back-office only, ADR-0013) | Implemented (PLAN-0002), not yet used by application code |
| `api` | ASP.NET Core Web API (DaxaPos.Api) | Not yet containerised — runs via `dotnet run` |
| `worker` | Background workers (DaxaPos.Workers) | Not yet created |
| `proxy` | Reverse proxy / TLS termination | Not yet created |

---

## Deployment Mode

Set the deployment mode via environment variable:

```
DAXA_DEPLOYMENT_MODE=Cloud
DAXA_DEPLOYMENT_MODE=Local
DAXA_DEPLOYMENT_MODE=Hybrid
```

---

## Related Documents

- [docker-deployment.md](docker-deployment.md) — full reference
- [Deployment: Local](local.md)
- [Deployment: Hybrid](hybrid.md)
- [ADR-0012 — Docker Local Deployment Strategy](../adr/accepted/ADR-0012-docker-local-deployment-strategy.md)
- [PLAN-0002 — Platform Skeleton](../plans/active/PLAN-0002-platform-skeleton.md)
