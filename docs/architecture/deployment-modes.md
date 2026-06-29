# Deployment Modes — Daxa POS

Daxa POS supports three deployment modes. All modes use the same codebase.

See [ADR-0002](../adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md) for the decision record.

---

## Daxa Cloud

Fully cloud-hosted. All API, database, reporting, and admin functions run in Daxa-managed cloud infrastructure.

```text
Daxa Cloud
├─ Tenant / organisation data
├─ Product catalogue
├─ Orders
├─ Payments and refunds
├─ Reporting
├─ Audit logs
├─ Admin portal
└─ APIs

Venue devices
├─ Daxa Terminal (MAUI)
├─ Daxa Display (MAUI)
├─ Daxa KDS (PWA)
├─ Printers
└─ Payment terminals
```

Venue devices connect to the cloud API for all operations.

---

## Daxa Local

On-premises. A Daxa Local Server runs inside the venue's network. Internet is not required for day-to-day trading.

```text
Venue local network
├─ Daxa Local Server
│  ├─ Local API (ASP.NET Core)
│  ├─ Local database (PostgreSQL)
│  ├─ Local identity (Keycloak)
│  ├─ Local sync service
│  ├─ Local reporting
│  └─ Local device registry
│
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa KDS (PWA)
├─ Receipt printers
├─ Kitchen/bar printers
├─ Payment terminals
└─ Admin devices
```

Data may optionally sync or back up to Daxa Cloud or an external storage target.

---

## Daxa Hybrid

Combines local operational continuity with cloud management and reporting.

```text
Daxa Cloud
├─ Tenant / organisation management
├─ Central product catalogue
├─ Central reporting
├─ Central audit log aggregation
├─ Payment provider configuration
├─ Admin portal
└─ Remote backup

        ⇅ Daxa Sync

Daxa Local Server
├─ Local order processing
├─ Local database
├─ Local terminal/device control
├─ Local printer routing
├─ Local payment routing
├─ Local audit capture
└─ Local offline mode

        ⇅ local network

Venue devices
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa KDS (PWA)
├─ Printers
└─ Payment terminals
```

Configuration flows from cloud to local. Operational data flows from local to cloud.

---

## Comparison

| Feature | Cloud | Local | Hybrid |
|---------|-------|-------|--------|
| Internet required to trade | Yes | No | No |
| Central reporting | Yes | No | Yes |
| Local device control | Via cloud | Yes | Yes |
| Central admin portal | Yes | No | Yes |
| Multi-location visibility | Yes | No | Yes |
| Offline resilience | No | Yes | Yes |
| Data sync | N/A | Optional | Yes |

---

## Related Documents

- [ADR-0001 — Single Codebase](../adr/proposed/ADR-0001-single-codebase.md)
- [ADR-0002 — Cloud, Local, Hybrid](../adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0007 — Sync Principles](../adr/proposed/ADR-0007-local-hybrid-sync-principles.md)
- [Deployment: Cloud](../deployment/cloud.md)
- [Deployment: Local](../deployment/local.md)
- [Deployment: Hybrid](../deployment/hybrid.md)
