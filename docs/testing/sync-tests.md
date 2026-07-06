# Testing: Sync Tests

Sync tests verify local-to-cloud and cloud-to-local data synchronisation.

---

## Required Test Categories

### Local Order Queue

- Order created offline is queued locally.
- Queued order has correct idempotency key.
- Queue is persisted across service restart.

### Local-to-Cloud Sync

- Order in queue is synced to cloud when connectivity restored.
- Synced order appears in cloud database.
- Idempotency key prevents duplicate on retry.
- Sync event is recorded in audit log.

### Cloud-to-Local Sync

- Menu change in cloud is applied to local server after sync.
- Pricing change in cloud is applied to local server after sync.
- Tax config change in cloud is applied to local server after sync.

### Sync Failure Resilience

- Sync failure does not abort local order processing.
- Failed sync events are retried on next cycle.
- Maximum retry count is respected.
- Failed events are flagged in sync status.

### Conflict Detection

- Conflict is detected when same record modified in both local and cloud (if applicable).
- Conflict is written to SyncConflict table.
- Original records are not silently overwritten.

### Full-State Reload

- Full-state reload endpoint returns all current data since timestamp.
- Local server can rebuild state from full-state reload.
- Device can rebuild state from full-state reload after reconnect.

---

## Test Project

```text
tests/DaxaPos.Sync.Tests/
tests/DaxaPos.IntegrationTests/
```

---

## Related Documents

- [ADR-0007 — Local/Hybrid Sync Principles](../adr/accepted/ADR-0007-local-hybrid-sync-principles.md)
- [Architecture: Sync](../architecture/sync.md)
- [Module: Sync](../modules/sync.md)
- [OI-0006 — Hybrid Sync Conflict Rules](../issues/closed/OI-0006-hybrid-sync-conflict-rules.md)
