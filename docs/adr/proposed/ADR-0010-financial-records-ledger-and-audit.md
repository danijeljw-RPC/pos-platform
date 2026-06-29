# ADR-0010 — Financial Records, Ledger, and Audit

## Status

Proposed

## Context

Daxa POS processes payments, refunds, voids, discounts, and surcharges. Financial records must be accurate, tamper-evident, and reconstructable for tax compliance, dispute resolution, and regulatory requirements.

Silent editing of financial records (e.g. changing an order total after payment) is a common source of fraud and compliance failures in POS systems.

## Decision

Daxa POS treats financially meaningful records as **append-only or reversal-based**.

- Orders, payments, and refunds must not be silently edited after they are created.
- Corrections use explicit records: **void**, **refund**, **reversal**, or **adjustment**.
- Payment, refund, gift card, and store credit activity is ledgered — every movement has a signed record.
- All financially significant operations are written to the audit log with: who, what, when, terminal, location, before value, after value, reason, and linked entity IDs.
- Receipt reprints are audited.
- Refund receipts link to the original order and original payment.
- Discount overrides and manual price changes are audited with reason capture.

**Prohibited patterns:**
- `UPDATE orders SET total = X` (without a correction record).
- Silent deletion of order lines after payment.
- Payment amount changes without a reversal + new payment.

## Consequences

**Positive:**
- Full audit trail for every financial transaction.
- Reconstructable order and payment history.
- Compliant with AU/NZ tax record-keeping requirements.
- Reduces fraud risk.
- Clear basis for dispute resolution.

**Negative:**
- More complex than simple CRUD.
- Storage grows as reversal/adjustment records accumulate.
- UI must expose void/refund flows rather than simple edit.

## Alternatives Considered

1. **Simple CRUD with history log** — Rejected. History log can be deleted or modified; append-only ledger is more robust.
2. **Full event sourcing** — Valuable model; a hybrid approach (ledger + audit events) is preferred for pragmatism without sacrificing auditability.

## Open Questions

- How long should financial records be retained?
- Should tax invoices be immutable PDFs stored in object storage?
- What is the required retention period under AU/NZ tax law?

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0007 — Local/Hybrid Sync Principles](ADR-0007-local-hybrid-sync-principles.md)
- [Module: Audit](../modules/audit.md)
- [Module: Payments](../modules/payments.md)
- [Module: Refunds](../modules/refunds.md)
- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
