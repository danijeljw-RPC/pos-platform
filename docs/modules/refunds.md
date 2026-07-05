# Module: Refund Service

The refund service processes refunds linked to original orders and payments.

---

## Responsibilities

- Full refunds.
- Partial refunds (by line or by amount).
- Linked provider refunds (where provider supports it).
- Manual refunds (where integrated refund is not available).
- Refund reason capture.
- Refund permission enforcement (RBAC).
- Refund receipt generation.
- Refund audit trail.
- Reversal records linked to original orders and payments.

## Refund Rules

- Refunds must be linked to the original order.
- Refunds must be linked to the original payment where possible.
- Refunds create a reversal record — original records are not modified.
- Refund amounts are limited to the original payment amount.
- Refunds require elevated staff role (configurable).
- All refunds are audited with reason, who, when, terminal, and linked order/payment IDs.

## Implementation Status (PLAN-0005 Milestone C, 2026-07-06)

Refund service foundation is implemented with 2 endpoints under `src/DaxaPos.Api/Endpoints/Refunds/`. No receipts or printing yet.

- `Refund`: `TenantId`, `PaymentId`, `OrderId`, `Amount`, `ReasonCode` (caller-supplied free-form code, matching `TaxCategory.Code`'s precedent — not a closed enum; the plan names the field but does not enumerate a fixed set of reason values), `ReasonNote?`, `RequestedByUserId?`/`RequestedByStaffMemberId?`, `Status` (`Recorded`/`ProviderPending`/`ProviderConfirmed` — only `Recorded` is reachable until PLAN-0009's adapter refund path exists), `RecordedAtUtc`, `ProviderReference?`. No `OrganisationId`/`LocationId` column of its own — scoped entirely through `PaymentId`/`OrderId`, matching `Payment`'s own precedent. No `IdempotencyKey`, unlike `Payment` — the plan's Milestone C entity field list does not include one.
- **A refund is a pure reversal record (ADR-0010): the original `Payment`/`Order` rows are never mutated.** A refunded order's `GrandTotalAmount`/line data and the original payment's `Status`/`AmountApproved` stay exactly as they were at sale time — `Order.Status` has no `Refunded` value and none is added by this milestone.
- **Settlement (`DaxaPos.Application.Payments.RefundSettlement`, TDD'd first):** the running total of `Recorded` refunds against a payment, plus a new refund's amount, may never exceed `Payment.AmountApproved` — full and partial refunds both add up against this same ceiling, enforced server-side (never client-trusted), rejected with 400 if it would be exceeded. A payment must be in `PaymentStatus.Recorded` to be refunded at all (409 otherwise).
- No new ledger table for refunds — a `Refund` row, once created, is never mutated in this milestone (mirroring how `Payment` itself is append-only despite `PaymentLedgerEntry` existing only because `Payment.Status` will transition through multiple states once PLAN-0009 lands). ADR-0010's refund-ledger/audit requirement is satisfied by the `Refund` row itself plus the audit event below.
- New permission `payments.refund` — **`AdminSensitive`** category, **`rejectStaffPin: true`**, granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager` only (not `Staff`, not `SupportAccess`) — a firmer floor than `orders.manage`/`payments.record`'s `Operational` category (approved Human Decision #4: manager/admin-only by default). Note the staff-PIN login endpoint independently rejects any role granting an `AdminSensitive` permission at login time (defense-in-depth) — a staff-PIN session can never even acquire `payments.refund`, regardless of role assignment.
- `RefundLifecycleAuditHandler` writes one `AuditEvent` row per refund (`EventType = "RefundRecorded"`), `AfterValue` carrying the linked `PaymentId`/`OrderId`/`Amount`/`ReasonCode`/`ReasonNote` as JSON — satisfies ADR-0010's "who, when, reason, linked order/payment ids" refund-audit requirement.
- See `docs/plans/active/PLAN-0005-worker-notes.md`'s "Milestone C Report" for full detail and deviations.

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
