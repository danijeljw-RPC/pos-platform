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

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/proposed/ADR-0010-financial-records-ledger-and-audit.md)
