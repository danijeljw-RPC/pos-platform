# Deployment: Daxa Local

Daxa Local is the on-premises deployment mode for Daxa POS.

---

## Overview

A Daxa Local Server runs inside the venue's own network. All POS operations are processed locally. Internet is not required for day-to-day trading.

Data may optionally sync or back up to Daxa Cloud.

---

## Server Stack (Docker Compose)

```text
Docker Compose services:
├─ daxa-api          ASP.NET Core Web API
├─ daxa-worker       Background workers
├─ db                PostgreSQL
├─ keycloak          Identity provider
└─ daxa-sync         Sync service (optional, for Hybrid)
```

See [Deployment: Docker](docker.md) for Docker Compose configuration.

---

## Hardware Requirements

Minimum and recommended hardware specifications are to be defined. See [OI-0003 — Local Server Reference Hardware](../issues/open/OI-0003-local-server-reference-hardware.md).

Initial recommendation: 8GB RAM / 4-core x86-64 / 256GB SSD NVMe minimum.

---

## Network Requirements

- Venue devices connect to the Daxa Local Server over the venue's local network.
- Static IP or DNS entry for the local server is recommended.
- Port 443 (HTTPS) for API and Keycloak.
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
- [ADR-0002 — Cloud, Local, Hybrid](../adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0012 — Docker Local Deployment Strategy](../adr/proposed/ADR-0012-docker-local-deployment-strategy.md)
- [OI-0003 — Local Server Reference Hardware](../issues/open/OI-0003-local-server-reference-hardware.md)
