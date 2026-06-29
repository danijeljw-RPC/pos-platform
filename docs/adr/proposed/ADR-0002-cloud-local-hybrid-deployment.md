# ADR-0002 — Cloud, Local, and Hybrid Deployment Modes

## Status

Proposed

## Context

Different venues have different infrastructure requirements. Venues with reliable internet may operate fully in the cloud. Venues that require operational continuity during internet outages (e.g. food trucks, remote sites, high-volume counters) need a local server. Some organisations want the control benefits of a local server combined with the management and reporting benefits of a cloud connection.

The platform must accommodate all three scenarios without forking the codebase.

## Decision

Daxa POS will support three named deployment modes:

**Daxa Cloud** — Fully cloud-hosted. All operational data, APIs, and reporting run in Daxa-managed cloud infrastructure. Venue devices connect to the cloud to process orders and payments.

**Daxa Local** — Local/on-premises. A Daxa Local Server runs inside the venue's network. The local server operates as the authoritative runtime for that site during trading. Internet connectivity is not required for day-to-day operations.

**Daxa Hybrid** — Combines cloud and local. The cloud provides central management, multi-location reporting, backups, and updates. The local server provides operational continuity, local device control, local payment routing, and resilience when internet access fails. Data syncs between local and cloud.

All three modes use the same codebase, same domain model, and same API concepts.

## Consequences

**Positive:**
- Venues choose the deployment model that fits their infrastructure and business risk tolerance.
- Internet outages do not automatically stop trading for Local and Hybrid venues.
- Central reporting and management remain available in Hybrid and Cloud modes.
- Single codebase and test suite apply across all modes.

**Negative:**
- All three modes must be designed for and tested, increasing upfront design complexity.
- Sync and conflict resolution (Hybrid mode) adds engineering effort.
- Local server hardware must be specified and supported.

## Alternatives Considered

1. **Cloud-only** — Rejected. Does not meet the needs of food trucks, remote locations, or venues with unreliable internet.
2. **Local-only** — Rejected. Does not meet the needs of multi-location chains needing central reporting.

## Open Questions

- See [OI-0003 — Local Server Reference Hardware](../issues/open/OI-0003-local-server-reference-hardware.md)
- See [OI-0008 — Cloud Data Region Strategy](../issues/open/OI-0008-cloud-data-region-strategy.md)
- How will deployment mode be detected and enforced at runtime?
- Will Daxa Local require a Daxa-issued device certificate for trust?

## Related Documents

- [ADR-0001 — Single Codebase](ADR-0001-single-codebase.md)
- [ADR-0007 — Local/Hybrid Sync Principles](ADR-0007-local-hybrid-sync-principles.md)
- [PLAN-0007 — Sync, Local, Hybrid](../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
- [Architecture: Deployment Modes](../architecture/deployment-modes.md)
