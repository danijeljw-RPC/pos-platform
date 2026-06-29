# Deployment: Daxa Hybrid

Daxa Hybrid combines a local server (Daxa Local) with cloud management (Daxa Cloud).

---

## Overview

In Daxa Hybrid, the local server handles all day-to-day trading. The cloud provides central management, reporting, backups, and multi-location visibility.

Data flows in both directions via Daxa Sync:
- Local → Cloud: orders, payments, refunds, audit events.
- Cloud → Local: menus, pricing, tax config, device config.

Internet loss does not stop trading. Sync resumes when connectivity is restored.

---

## Architecture

```text
Daxa Cloud
├─ Central management
├─ Central reporting
├─ Central audit aggregation
├─ Menu/pricing/tax config (master)
├─ Backup
└─ Admin portal

      ⇅ Daxa Sync (HTTPS)

Daxa Local Server
├─ daxa-api
├─ daxa-worker
├─ db (PostgreSQL)
├─ keycloak (or cloud Keycloak via cache)
├─ daxa-sync
└─ local backup

      ⇅ local network

Venue devices
├─ Daxa Terminal (MAUI)
├─ Daxa Display (MAUI)
├─ Daxa KDS (PWA)
├─ Receipt printers
└─ Payment terminals
```

---

## Sync Behaviour

See [Architecture: Sync](../architecture/sync.md) for full details.

- Configuration (menus, pricing, tax) syncs from cloud to local on a schedule and on change.
- Operational data (orders, payments) syncs from local to cloud after each transaction.
- Sync queue retries on failure.
- Idempotency keys prevent duplicate records.

## Identity in Hybrid Mode

Identity for hybrid mode is pending decision. See [OI-0010 — Local Keycloak vs Cloud Keycloak](../issues/open/OI-0010-local-keycloak-vs-cloud-keycloak.md).

---

## Related Documents

- [Deployment: Local](local.md)
- [Deployment: Cloud](cloud.md)
- [Architecture: Deployment Modes](../architecture/deployment-modes.md)
- [Architecture: Sync](../architecture/sync.md)
- [PLAN-0007 — Sync, Local, Hybrid](../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
