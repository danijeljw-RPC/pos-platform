# OI-0006 — Hybrid Sync Conflict Rules

## Status

Open

## Area

Sync

## Summary

What are the rules for resolving conflicts when data is modified both locally (on a Daxa Local server) and in the cloud before sync occurs?

## Context

In Daxa Hybrid deployments, data flows in both directions: orders and operational data flow from local to cloud; configuration (menus, pricing, tax) flows from cloud to local. If the same record is modified in both places before sync completes, a conflict occurs.

ADR-0007 states conflicts must be explicit — not silently overwritten. The specific rules for how conflicts are detected, surfaced, and resolved are not yet defined.

## Impact

- Determines the conflict detection and resolution implementation in Daxa Sync.
- Affects the data model (`SyncConflict` table structure).
- Affects the admin UI (conflict review and resolution workflow).
- Affects financial record integrity.

## Options

1. **Last-write-wins** — Simplest. High risk of silent data loss, especially for financial records. Rejected.
2. **Cloud wins for config, local wins for operational data** — Reasonable default. Configuration (menus, pricing) always defers to cloud master. Operational data (orders, payments) always comes from local.
3. **Explicit conflict queue, human review** — All conflicts go into a `SyncConflict` review queue. Requires admin action to resolve.
4. **Operational data never conflicts (append-only)** — Orders and payments are append-only and never conflict by nature. Config conflicts use cloud-wins rule. Most conflicts are therefore self-resolving.

## Recommendation

Option 4 with Option 2 for config: define operational records (orders, payments, refunds, audit events) as append-only (no conflicts possible). Define configuration records as cloud-master (cloud wins). Any residual conflicts go to explicit review queue with alert.

## Decision Needed

- Conflict resolution rules per data category (operational vs config).
- Whether an admin review queue is required.
- How conflicts are surfaced to admins.

## Related ADRs

- [ADR-0007 — Local/Hybrid Sync Principles](../../adr/proposed/ADR-0007-local-hybrid-sync-principles.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../../adr/proposed/ADR-0010-financial-records-ledger-and-audit.md)

## Related Documents

- [Architecture: Sync](../../architecture/sync.md)
- [Module: Sync](../../modules/sync.md)
- [PLAN-0007 — Sync, Local, Hybrid](../../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
