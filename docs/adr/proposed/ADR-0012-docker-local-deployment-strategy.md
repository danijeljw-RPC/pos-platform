# ADR-0012 — Docker and Docker Compose Local Deployment Strategy

## Status

Proposed

## Context

Daxa POS backend services (API, database, background workers, sync service, identity provider) need to be deployable in development environments and on local (on-premises) hardware. A containerised approach simplifies setup, reduces environment inconsistency, and makes local server deployment reproducible.

Docker and Docker Compose are the de facto standard for containerised local development and small-scale production deployment. They work across Windows, Linux, and macOS.

## Decision

Daxa POS uses **Docker and Docker Compose** for:

1. **Local development** — All developers run the backend stack via `docker compose up`.
2. **Daxa Local server deployment** — Venues running on-premises use a Docker Compose stack on a local server.
3. **CI/CD** — Integration test pipelines use Docker to spin up real PostgreSQL, Keycloak, and service dependencies.

The Compose stack will include:

```yaml
services:
  api:          # DaxaPos.Api
  worker:       # DaxaPos.Workers
  db:           # PostgreSQL
  keycloak:     # Identity provider
  sync:         # Daxa Sync service (Hybrid/Local)
```

Secrets are managed via environment variables and Docker secrets. Provider credentials are never committed to the repository.

Cloud deployment strategy (Kubernetes, Azure Container Apps, AWS ECS, etc.) is a separate decision to be addressed in a later ADR.

## Consequences

**Positive:**
- Reproducible local development environment.
- Local server deployment is consistent with development environment.
- Integration tests run against real services.
- Onboarding new developers is faster.

**Negative:**
- Local server hardware must support Docker (Linux preferred; Windows containers are less common in hospitality).
- Keycloak in Docker can be resource-intensive on small hardware.
- Persistent volume management must be planned for on-premises deployments.

## Alternatives Considered

1. **Native installation (no Docker)** — Rejected. Difficult to reproduce consistently across environments.
2. **Kubernetes locally (minikube/kind)** — Considered for development but too complex for venue-managed local servers.
3. **Pre-built VM image** — A future option for venue deployment; does not replace Docker Compose as the composition tool.

## Open Questions

- See [OI-0003 — Local Server Reference Hardware](../issues/open/OI-0003-local-server-reference-hardware.md)
- What is the minimum RAM/CPU for a local Daxa server running the full Docker Compose stack?
- Should the Docker images be signed and distributed via a private registry?

## Related Documents

- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](ADR-0002-cloud-local-hybrid-deployment.md)
- [Deployment: Docker](../deployment/docker.md)
- [Deployment: Local](../deployment/local.md)
- [PLAN-0008 — Testing, Security, Deployment](../plans/active/PLAN-0008-testing-security-deployment-planning.md)
