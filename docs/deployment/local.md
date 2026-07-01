# Deployment: Daxa Local

Daxa Local is the on-premises deployment mode for Daxa POS.

---

## Overview

A Daxa Local Server runs inside the venue's own network. All POS operations are processed locally. Internet is not required for day-to-day trading.

Data may optionally sync or back up to Daxa Cloud.

> **Current implementation status (PLAN-0002, 2026-07-01):** the server stack below is the target production design. Today only the developer-facing skeleton exists — `deploy/docker-compose.yml` running `db` and `keycloak`, with the API run directly via `dotnet run`. No `daxa-worker` or `daxa-sync` service exists yet. See [docker.md](docker.md) § Current Skeleton Status.

---

## Server Stack (Docker Compose)

```text
Docker Compose services:
├─ daxa-api          ASP.NET Core Web API
├─ daxa-worker       Background workers
├─ db                PostgreSQL
└─ daxa-sync         Sync service (optional, for Hybrid)
```

Keycloak is not included in the default Daxa Local stack. Local POS authentication is handled by the Daxa WebAPI using staff ID/PIN and local username/password. See [ADR-0013](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md).

See [Deployment: Docker](docker.md) for Docker Compose configuration.

### Bootstrap admin (dev/local only)

Since there is no self-serve tenant/organisation provisioning yet, the first `SystemAdmin` user for a fresh local install is created by an idempotent startup routine (`BootstrapAdminSeeder`, PLAN-0003 Milestone C) that reads `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`DAXA_BOOTSTRAP_ADMIN_PASSWORD` from `deploy/.env` (see `deploy/.env.example`). If either is unset, seeding is skipped and the app still starts normally — there is no fallback/guessable credential in source. If a user with that email already exists, seeding is skipped without resetting its password. Rotate or replace this credential before any real use; this mechanism is not a production tenant-onboarding flow.

---

## Hardware Requirements

See [OI-0003 — Local Server Reference Hardware](../issues/closed/OI-0003-local-server-reference-hardware.md) for the full decision.

**Minimum:** 8GB RAM / 4-core x86-64 / 256GB NVMe SSD / wired Ethernet.

**Recommended:** 16GB RAM / 4-core x86-64 / 512GB NVMe SSD / wired Ethernet / business mini PC or small form-factor PC suitable for always-on venue operation.

Raspberry Pi / ARM hardware is not supported for production Daxa Local deployments. Linux + Docker Compose on x86-64 is the production target.

---

## Network Requirements

- Venue devices connect to the Daxa Local Server over the venue's local network.
- Static IP or DNS entry for the local server is recommended.
- Port 443 (HTTPS) for the Daxa API.
- Port 5432 (PostgreSQL) — internal Docker network only, not exposed externally.

---

## Backup

Daxa Local supports:
- Automated daily backup to a local storage path.
- Optional sync to Daxa Cloud (Hybrid mode).
- Optional export to S3-compatible storage.

---

## Related Documents

- [Architecture: Deployment Modes](../architecture/deployment-modes.md)
- [Deployment: Docker](docker.md)
- [Deployment: Hybrid](hybrid.md)
- [ADR-0002 — Cloud, Local, Hybrid](../adr/accepted/ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0012 — Docker Local Deployment Strategy](../adr/accepted/ADR-0012-docker-local-deployment-strategy.md)
- [OI-0003 — Local Server Reference Hardware](../issues/closed/OI-0003-local-server-reference-hardware.md)
