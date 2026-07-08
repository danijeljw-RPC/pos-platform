# Module: Sync Service

The sync module provides local-to-cloud and cloud-to-local data synchronisation.

See also: `docs/architecture/sync.md`.

**Not this document:** browser/PWA offline and reconnect resilience for the existing single-API
Terminal/Display/KDS screens (connectivity tracking, reconnect revalidation, offline-safe action
retry, cross-tab draft-pointer detection) is a separate, already-implemented concern — see
[PLAN-0007](../plans/active/PLAN-0007-sync-local-hybrid-planning.md). It does not use a local
server, a sync worker, or anything described below; it operates entirely browser-side against the
one existing Daxa API.

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

- [PLAN-0007 — Browser/PWA Offline and Reconnect Resilience](../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
- [ADR-0007 — Local/Hybrid Sync Principles](../adr/accepted/ADR-0007-local-hybrid-sync-principles.md)
- [OI-0006 — Hybrid Sync Conflict Rules](../issues/closed/OI-0006-hybrid-sync-conflict-rules.md)
