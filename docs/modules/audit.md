# Module: Audit Log Service

The audit log service records all security-significant and financially significant events.

---

## Responsibilities

- Security audit: login, PIN login, device registration, support access, permission changes.
- Financial audit: payments, refunds, voids, discounts, overrides, price changes.
- Configuration audit: tax settings, product changes, menu changes, payment provider changes.
- Operational audit: receipt reprints, cash drawer opens, terminal configuration changes.

## Audit Event Fields

```text
AuditEvent
├─ Id
├─ TenantId
├─ OrganisationId
├─ LocationId
├─ TerminalId
├─ UserId
├─ EventType
├─ EntityType
├─ EntityId
├─ BeforeValue (JSON snapshot)
├─ AfterValue  (JSON snapshot)
├─ Reason
├─ IpAddress
├─ OccurredAt
└─ LinkedEntityIds
```

## Key Rules

- Audit events are append-only (never deleted or modified).
- All refunds, voids, and discounts are audited with reason and who.
- Receipt reprints are audited.
- Support access is audited.

## Related Plans

- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
