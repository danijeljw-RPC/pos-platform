# Module: Sync Service

The sync module provides local-to-cloud and cloud-to-local data synchronisation.

See also: `docs/architecture/sync.md`.

---

## Responsibilities

- Local order queue (persist orders locally before sync).
- Local-to-cloud sync (orders, payments, refunds, audit events).
- Cloud-to-local sync (menus, pricing, tax config, device config).
- Idempotency key generation and tracking.
- Sync status tracking (pending, in-progress, completed, failed, conflict).
- Conflict detection and SyncConflict table.
- Full-state reload after reconnect.
- Sync audit logging.

## Key Rules

- Sync failure must not stop trading.
- Retries use idempotency keys (safe to retry).
- Operational data (orders, payments) is append-only and cannot conflict.
- Configuration data (menus, pricing) is cloud-master.
- Conflicts are written to `SyncConflict` table for review.

## Related Plans

- [PLAN-0007 — Sync, Local, Hybrid](../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
- [ADR-0007 — Local/Hybrid Sync Principles](../adr/accepted/ADR-0007-local-hybrid-sync-principles.md)
- [OI-0006 — Hybrid Sync Conflict Rules](../issues/closed/OI-0006-hybrid-sync-conflict-rules.md)
