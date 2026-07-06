# PLAN-0007 — Sync, Local, and Hybrid

## Status

Draft

## Goal

Implement Daxa Sync — the local-to-cloud and cloud-to-local synchronisation layer. Enable Daxa Local and Daxa Hybrid deployment modes by building the sync service, local order queue, conflict detection, and audit of all sync events.

## Scope

- `DaxaPos.Modules.Sync` — sync service core.
- Local order queue (persist and replay when online).
- Local-to-cloud sync (orders, payments, refunds, audit events).
- Cloud-to-local sync (menus, pricing, tax config, device config).
- Idempotency key handling.
- Conflict detection (explicit, not silent).
- Sync event audit log.
- Full-state reload after reconnect.

## Non-goals

- Multi-region cloud replication.
- Cross-tenant sync.
- External data export (accounting systems).

## Context Read

- `docs/adr/accepted/ADR-0007-local-hybrid-sync-principles.md`
- `docs/adr/accepted/ADR-0002-cloud-local-hybrid-deployment.md`
- `docs/modules/sync.md`
- `docs/architecture/sync.md`
- `docs/issues/closed/OI-0006-hybrid-sync-conflict-rules.md`

## Files Likely To Change

```text
src/DaxaPos.Modules.Sync/
src/DaxaPos.Workers/    (sync background worker)
src/DaxaPos.Infrastructure/ (local queue, HTTP sync client)
```

## Architecture Assumptions

- Sync is a background service, not blocking to the main API.
- Local order queue is PostgreSQL-backed (not in-memory).
- Idempotency keys are UUIDs generated on the local server before transmission.
- Conflicts are written to a `SyncConflict` table for human or automated resolution.
- Full-state reload endpoint exists on the cloud API.

## Domain Assumptions

- Local server has its own PostgreSQL instance.
- Local server connects to cloud API via HTTPS on port 443.
- Cloud configuration is the master for menu/pricing/tax; local orders are the master for operational data.

## Risks

- Conflict resolution rules are not yet defined — see OI-0006.
- Large queues (extended offline period) may cause slow sync on reconnect.
- Cloud API rate limits during bulk sync must be handled.

## Implementation / Documentation Steps

1. Resolve OI-0006 (conflict rules) or create proposed ADR.
2. Implement local order queue (enqueue order, mark as synced).
3. Implement local-to-cloud sync worker (batch sync, idempotency).
4. Implement cloud-to-local sync worker (config pull, menu pull).
5. Implement conflict detection and `SyncConflict` table.
6. Implement full-state reload API endpoint.
7. Implement sync event audit logging.
8. Write sync tests (happy path, offline-reconnect, conflict).
9. Update `docs/modules/sync.md` and `docs/architecture/sync.md`.

## Tests To Run Later

- Local order queued when offline.
- Order synced to cloud when connectivity restored.
- Idempotent sync (retry does not duplicate records).
- Conflict detected and written to SyncConflict table.
- Full-state reload after reconnect.
- Cloud config applied to local server after sync.
- Sync audit log entries.

## Documentation To Update

- `docs/modules/sync.md`
- `docs/architecture/sync.md`
- `docs/deployment/hybrid.md`
- `docs/deployment/local.md`

## ADRs Required

- ADR-0007 (already proposed).
- New ADR for conflict resolution rules (after OI-0006 is resolved).

## Open Issues Required

- OI-0006 (hybrid sync conflict rules) — must be resolved before implementing conflict handling.

## Commit Sequence

```text
feat(sync): add local order queue
feat(sync): add local-to-cloud sync worker
feat(sync): add cloud-to-local config sync worker
feat(sync): add conflict detection and SyncConflict table
feat(api): add full-state reload endpoint
feat(audit): add sync event audit logging
test(sync): add sync, offline, and conflict tests
docs: update sync, hybrid, and local deployment docs
```

## Handoff Notes

Depends on PLAN-0002 (Platform Skeleton) and PLAN-0004 (Catalog/Tax). Can be started in parallel with PLAN-0005 and PLAN-0006 as it is a separate module. OI-0006 must be resolved before this plan can be fully completed.
