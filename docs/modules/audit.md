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

## Implementation status (PLAN-0003 Milestone D, 2026-07-02)

Confirmed: `BeforeValue`/`AfterValue` are stored as `jsonb` columns (not plain strings) — the audit handler assigns whatever the domain event carries directly, so callers constructing a lifecycle domain event must `JsonSerializer.Serialize(...)` the before/after snapshot themselves (a bare string like a plain name is not valid JSON and the write fails). `OrganisationLifecycleDomainEvent`/`LocationLifecycleDomainEvent`/`TerminalLifecycleDomainEvent` (one event type per entity, carrying an `Action` field) write `EventType` as `$"{EntityType}{Action}"` (e.g. `"OrganisationDeactivated"`), matching the existing `"LocalUserLoginSucceeded"`-style naming from Milestone C.

## Implementation status (PLAN-0003 Milestone E, 2026-07-02)

Six device-lifecycle audit event types added per ADR-0008's audit requirements: `DeviceRegistrationPinCreated`, `DeviceRegistrationPinRevoked`, `DeviceRegistered`, `DeviceRegistrationFailed`, `DeviceCredentialRotated`, `DeviceRevoked`. `DeviceRegistrationFailed` is written **only when the presented PIN matched a real row resolving to a single tenant** (expired/revoked/exhausted, or an ambiguous multi-match within one tenant) — an unknown PIN writes nothing, because `AuditEvent.TenantId` is non-nullable (the same rule as Milestone C's unknown-email login failures). ADR-0008 asks for failed attempts to be audit logged, so this is a recorded gap: auditing unauthenticated/global security events (unknown registration PINs, unknown login emails) needs a separate tenant-less table — a flagged future decision, not solved in PLAN-0003.
