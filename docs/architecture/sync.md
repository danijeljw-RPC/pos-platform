# Sync Architecture — Daxa POS

Daxa Sync is the local-to-cloud and cloud-to-local synchronisation layer for Daxa Local and Daxa Hybrid deployments.

See [ADR-0007](../adr/proposed/ADR-0007-local-hybrid-sync-principles.md) for the decision record.

---

## Sync Directions

```text
Daxa Local Server ──── orders, payments, refunds, audit ──→ Daxa Cloud
Daxa Cloud ──── menus, pricing, tax config, device config ──→ Daxa Local Server
```

---

## Principles

1. **Idempotency** — Every sync operation includes an idempotency key. Retries are safe.
2. **Append-only operational data** — Orders, payments, refunds are append-only. No conflicts possible.
3. **Cloud master for configuration** — Menus, pricing, tax rates, device config originate from cloud.
4. **Explicit conflicts** — Any residual conflict is written to `SyncConflict` table. Not silently overwritten.
5. **Sync failure does not stop trading** — Local operations continue unaffected by sync failure.
6. **Full-state reload** — Devices can request a full state reload from the authoritative server after reconnect.
7. **Audit all sync events** — Every sync operation is written to the audit log.

---

## Data Categories

| Category | Direction | Master | Conflict Risk |
|----------|-----------|--------|---------------|
| Orders, Payments, Refunds | Local → Cloud | Local | None (append-only) |
| Menus, Pricing, Tax Config | Cloud → Local | Cloud | Low (cloud wins) |
| Device Config | Cloud → Local | Cloud | Low (cloud wins) |
| User Accounts | Cloud → Local | Cloud | Low |
| Audit Events | Local → Cloud | Local | None (append-only) |
| Inventory | Local → Cloud | Local | Low |

---

## Sync Event Record

```text
SyncEvent
├─ Id
├─ Direction (LocalToCloud / CloudToLocal)
├─ EntityType
├─ EntityId
├─ IdempotencyKey
├─ Status (Pending / InProgress / Completed / Failed / Conflict)
├─ AttemptCount
├─ LastAttemptAt
├─ CompletedAt
├─ ErrorDetails
└─ AuditEventId
```

---

## Full-State Reload

After reconnect, a device or local server may request a full-state reload:

```text
GET /api/v1/sync/full-state?since=2024-01-01T00:00:00Z
→ Returns all orders, configs, and device data since the specified timestamp
```

This ensures correctness even if realtime events were missed during outage.

---

## Open Questions

- See [OI-0006 — Hybrid Sync Conflict Rules](../issues/open/OI-0006-hybrid-sync-conflict-rules.md)

---

## Related Documents

- [ADR-0007 — Local/Hybrid Sync Principles](../adr/proposed/ADR-0007-local-hybrid-sync-principles.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/proposed/ADR-0010-financial-records-ledger-and-audit.md)
- [Module: Sync](../modules/sync.md)
- [PLAN-0007 — Sync, Local, Hybrid](../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
