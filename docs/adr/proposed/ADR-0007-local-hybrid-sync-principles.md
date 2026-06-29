# ADR-0007 — Local/Hybrid Sync Principles

## Status

Proposed

## Context

Daxa Local and Daxa Hybrid deployments require data to flow between the local server and the cloud. Orders created locally must eventually reach the cloud for reporting and backup. Configuration changes made in the cloud must reach local servers to update pricing, menus, and tax settings.

This sync must be reliable, auditable, and resilient to partial failures and conflicts.

## Decision

Local-to-cloud and cloud-to-local sync in Daxa POS follows these principles:

1. **Idempotency keys** — Every sync operation includes an idempotency key so that retries do not create duplicate records.
2. **Append-only event log** — Sync events are logged with timestamps, direction, status, and entity references. Sync history is not deleted.
3. **Server state is authoritative** — For Daxa Hybrid, the cloud is the master of configuration (menus, pricing, tax). The local server is the master of operational data (orders, payments) created during local operation.
4. **Conflicts are explicit** — When a sync conflict occurs (e.g. same record modified locally and in cloud before sync), the conflict is surfaced explicitly for review rather than silently overwritten.
5. **Sync failure must not stop trading** — If the sync service fails or the network is unavailable, local operations continue unaffected. Sync retries when connectivity is restored.
6. **Full-state reload** — After reconnect, devices must be able to rebuild their full operational state by requesting a full reload from the authoritative server, not by replaying missed events.
7. **Audit sync events** — All sync activity is written to the audit log.

## Consequences

**Positive:**
- Internet loss does not stop trading.
- Conflict handling is visible and auditable.
- Retry logic is safe via idempotency.
- Historical sync records support debugging and reporting.

**Negative:**
- Sync engine is a significant engineering investment.
- Conflict resolution rules must be explicitly designed.
- Full-state reload can be expensive for large datasets.

## Alternatives Considered

1. **Event sourcing only** — Considered. Useful but adds complexity; hybrid approach with idempotent operations preferred.
2. **Last-write-wins conflict resolution** — Rejected. Risk of silent data loss on financial records.

## Open Questions

- See [OI-0006 — Hybrid Sync Conflict Rules](../../issues/open/OI-0006-hybrid-sync-conflict-rules.md)

## Related Documents

- [ADR-0002 — Cloud, Local, Hybrid Deployment](ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0010 — Financial Records Ledger and Audit](ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Sync](../../architecture/sync.md)
- [Module: Sync](../../modules/sync.md)
- [PLAN-0007 — Sync, Local, Hybrid](../../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
