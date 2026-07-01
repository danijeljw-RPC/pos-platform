# ADR-0012 — Docker and Docker Compose Local Deployment Strategy

## Status

Accepted

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

- See [OI-0003 — Local Server Reference Hardware](../../issues/open/OI-0003-local-server-reference-hardware.md)
- What is the minimum RAM/CPU for a local Daxa server running the full Docker Compose stack?
- Should the Docker images be signed and distributed via a private registry?

## Related Documents

- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](ADR-0002-cloud-local-hybrid-deployment.md)
- [Deployment: Docker](../../deployment/docker.md)
- [Deployment: Local](../../deployment/local.md)
- [PLAN-0008 — Testing, Security, Deployment](../../plans/active/PLAN-0008-testing-security-deployment-planning.md)

---

## Acceptance Addendum

ADR-0012 is accepted.

Daxa POS will use Docker Compose as the local deployment strategy for on-prem and hybrid local server installations.

The local server hardware decision is linked to:

- OI-0003 — Local Server Reference Hardware

OI-0003 contains the current reference hardware answer and should be treated as the supporting open issue for physical hardware selection.

## Resolution of Open Questions

### What is the minimum RAM/CPU for a local Daxa server running the full Docker Compose stack?

The initial local server reference target will be a small form-factor PC supplied by a vendor, packaged as part of the Daxa POS deployment, or documented as a supported customer-provided hardware option.

The expected reference specification is:

```text
Form factor: Small form-factor PC
CPU: Intel CPU
RAM: 64 GB
Storage: 512 GB local storage
Operating system: Linux
Deployment model: Docker Compose
```

This is not yet treated as the final minimum supported specification.

The 64 GB RAM reference target is intentionally conservative while the full local stack is still being developed, tested, and certified.

The final minimum hardware requirements must be confirmed through load testing and real-world venue testing.

Testing should consider:

- Number of POS terminals.
- Number of KDS screens.
- Number of printers.
- Number of concurrent staff sessions.
- Size of catalogue.
- Order volume.
- Sync workload.
- Local database size.
- Reporting workload.
- Whether optional services such as Redis, Keycloak, background workers, or monitoring are enabled.

The system may later define multiple hardware profiles, such as:

```text
Small venue
Medium venue
Large venue
Multi-location local hub
Development/test install
```

However, the accepted deployment direction is that the production local server will be a Linux-based small form-factor PC running the Daxa Docker Compose stack.

### Should the Docker images be signed and distributed via a private registry?

Yes.

Production Docker images should be signed and distributed through a private container registry.

This is the accepted commercial deployment direction.

The exact registry provider, image signing mechanism, key management process, and release pipeline will be determined later when Daxa POS is ready for commercial deployment.

The implementation should allow for:

- Private image distribution.
- Versioned Docker images.
- Signed images.
- Controlled access to production images.
- Release channels, such as stable, preview, and internal.
- Rollback to a previous known-good image version.
- Deployment audit logging.
- Future automated update tooling.

During early development, local builds and development registries may be used.

For production, customers should not be expected to build Docker images themselves.

## Accepted Deployment Direction

The local Daxa POS deployment will be packaged as a Docker Compose stack running on Linux server hardware.

The stack may include services such as:

```text
Daxa Web App / PWA
Daxa WebAPI
PostgreSQL
Background worker services
Redis, if required
Reverse proxy
Device/printer/terminal integration services
Monitoring/logging support, where required
```

The exact service list may evolve as the product matures.

The Docker Compose deployment must remain suitable for:

- Local-only deployments.
- Hybrid deployments.
- On-prem venue servers.
- Vendor-supplied hardware.
- Customer-provided supported hardware.
- Future update tooling.

## Hardware Certification

The first supported hardware target should be treated as a reference platform, not an exclusive hardware requirement.

Daxa may later certify specific small form-factor PCs as supported hardware.

A certified local server should confirm:

- Stable Linux support.
- Reliable Docker support.
- Sufficient CPU/RAM/storage capacity.
- Suitable network interface performance.
- Good thermal behaviour.
- Reliable restart behaviour after power loss.
- Vendor availability.
- Practical replacement process.
- Remote support suitability.

## Relationship to OI-0003

OI-0003 captures the current reference hardware decision.

ADR-0012 captures the deployment strategy.

The relationship is:

```text
ADR-0012 = how the local stack is deployed
OI-0003 = what hardware is initially used to run it
```

If the hardware decision changes later, OI-0003 or a future ADR/OI should be updated without changing the core Docker Compose deployment strategy.

## Consequences

This decision keeps the local deployment simple and supportable.

Using Docker Compose allows the same stack to be packaged, tested, and deployed consistently across supported local server hardware.

Using a vendor-supplied or certified small form-factor PC reduces customer setup complexity.

Using a private registry and signed images provides a path toward secure commercial distribution when the product is ready.

The final minimum specification remains subject to testing and certification.

## Status Update

Status: **Accepted**
