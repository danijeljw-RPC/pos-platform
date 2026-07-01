# Deployment: Daxa Cloud

Daxa Cloud is the fully cloud-hosted deployment mode for Daxa POS.

---

## Overview

In Daxa Cloud, all Daxa POS services run in Daxa-managed cloud infrastructure. Venue devices connect to the cloud API over the internet for all operations.

Internet connectivity is required for trading in this mode.

---

## Components

```text
Daxa Cloud Infrastructure
├─ API (ASP.NET Core)
├─ PostgreSQL (managed)
├─ Identity (Keycloak)
├─ Workers (sync, reporting, jobs)
├─ Admin portal (PWA)
└─ Reporting service

Venue (connects over internet)
├─ Daxa Terminal (MAUI)
├─ Daxa Display (MAUI)
├─ Daxa KDS (PWA)
├─ Receipt printers
└─ Payment terminals
```

---

## Cloud Region

Cloud region is to be decided. See [OI-0008 — Cloud Data Region Strategy](../issues/closed/OI-0008-cloud-data-region-strategy.md).

For AU/NZ launch, Sydney region (AWS ap-southeast-2 or Azure Australia East) is the initial recommendation.

---

## Configuration

Environment variables required:

```
DAXA_DB_CONNECTION_STRING=
DAXA_KEYCLOAK_URL=
DAXA_KEYCLOAK_REALM=
DAXA_KEYCLOAK_CLIENT_ID=
DAXA_KEYCLOAK_CLIENT_SECRET=
DAXA_PAYMENT_PROVIDER_*=
```

Secrets must not be committed to the repository.

---

## TODO

- Cloud provider selection (see OI-0008).
- CI/CD pipeline for cloud deployment.
- Blue/green or rolling deployment strategy.
- Backup and DR configuration.
- Monitoring and alerting setup.

---

## Related Documents

- [Architecture: Deployment Modes](../architecture/deployment-modes.md)
- [ADR-0002 — Cloud, Local, Hybrid](../adr/accepted/ADR-0002-cloud-local-hybrid-deployment.md)
- [OI-0008 — Cloud Data Region Strategy](../issues/closed/OI-0008-cloud-data-region-strategy.md)
- [PLAN-0008 — Testing, Security, Deployment](../plans/active/PLAN-0008-testing-security-deployment-planning.md)
