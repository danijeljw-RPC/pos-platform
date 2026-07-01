# Deployment: Docker and Docker Compose

Daxa POS uses Docker and Docker Compose for local development and on-premises (Daxa Local) deployment.

See [ADR-0012](../adr/accepted/ADR-0012-docker-local-deployment-strategy.md) for the decision record.

For the full detailed deployment reference, see [docker-deployment.md](docker-deployment.md) — this file contains the comprehensive Docker deployment guide including all environment variables, volumes, ports, health checks, backup procedures, and worker configuration.

---

## Quick Start (Development)

```bash
cd deploy
cp .env.example .env
# Edit .env with your local values
docker compose up --build
```

Health check:

```bash
curl http://localhost/api/health
```

---

## Core Docker Compose Services

| Service | Purpose |
|---------|---------|
| `db` | PostgreSQL database |
| `api` | ASP.NET Core Web API (DaxaPos.Api) |
| `worker` | Background workers (DaxaPos.Workers) |
| `keycloak` | Identity provider |
| `proxy` | Reverse proxy / TLS termination |

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
