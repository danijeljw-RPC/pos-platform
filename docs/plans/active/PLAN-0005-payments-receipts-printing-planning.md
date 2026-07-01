# PLAN-0005 — Payments, Receipts, and Printing

## Status

Draft

## Goal

Implement the payment service (cash, manual EFTPOS, integrated payment adapter foundation), refund service, receipt generation, and receipt printing. This is the core commercial transaction layer.

## Scope

- Order service (create order, add/remove lines, modifiers, notes).
- Cash payment.
- Manual external EFTPOS payment.
- Payment adapter interface (`IPaymentTerminalProvider`).
- Payment ledger.
- Refund service (full and partial).
- Receipt generation (thermal, tax invoice).
- ESC/POS receipt printing.
- Receipt tax marker (`F = GST-free`).
- Audit logging for all payment and refund activity.

## Non-goals

- Full integrated payment provider implementations — the first integration (Stripe Terminal) is [PLAN-0009](PLAN-0009-first-payment-adapter-stripe-terminal.md).
- Kitchen/bar docket routing (PLAN-0006 onwards).
- Gift cards and store credit.
- Split bills.

## Context Read

- `docs/adr/accepted/ADR-0005-payment-provider-adapter-architecture.md`
- `docs/adr/accepted/ADR-0010-financial-records-ledger-and-audit.md`
- `docs/adr/accepted/ADR-0011-receipt-tax-marker-strategy.md`
- `docs/modules/payments.md`
- `docs/modules/refunds.md`
- `docs/modules/receipts.md`
- `docs/modules/printing.md`
- `docs/architecture/payment-adapters.md`

## Files Likely To Change

```
src/DaxaPos.Modules.Orders/
src/DaxaPos.Modules.Payments/
src/DaxaPos.Modules.Refunds/  (or within Payments module)
src/DaxaPos.Modules.Receipts/
src/DaxaPos.Infrastructure/    (ESC/POS printer service)
src/DaxaPos.PaymentProviders.*/  (interface + manual EFTPOS stub)
```

## Architecture Assumptions

- Orders use the tax engine from PLAN-0004 to calculate and snapshot taxes.
- Payments are ledgered (append-only, no silent edits).
- Refunds create reversal records linked to original orders/payments.
- Receipt printer uses ESC/POS commands over network or USB.

## Domain Assumptions

- Cash and manual EFTPOS require no integrated terminal API call.
- Integrated payment always sends amount from POS to terminal (never manual entry).
- Refunds may only be performed by authorised staff (RBAC from PLAN-0003).

## Risks

- ESC/POS compatibility varies by printer model.
- Payment idempotency keys must be generated and tracked correctly.
- Refund permissions must prevent unauthorised refunds.

## Implementation / Documentation Steps

1. Implement Order service (create, add/remove lines, modifiers, notes, state machine).
2. Implement cash payment recording.
3. Implement manual EFTPOS payment recording.
4. Define `IPaymentTerminalProvider` interface and register adapter resolution.
5. Implement payment ledger (idempotent records).
6. Implement refund service (linked to original payment).
7. Implement receipt generation (line items, tax summary, markers).
8. Implement ESC/POS printer service (receipt printing, cash drawer kick).
9. Add audit logging for all payment and refund activity.
10. Write payment, refund, and receipt tests.
11. Update module docs.

## Tests To Run Later

- Order creation and tax calculation (using tax engine from PLAN-0004).
- Cash payment recording and ledger entry.
- Manual EFTPOS payment recording.
- Refund with linked original payment.
- Receipt rendering with GST-free marker.
- Receipt tax summary accuracy.
- Payment idempotency (retry does not create duplicate payment).
- Audit log entries for payment and refund.

## Documentation To Update

- `docs/modules/orders.md`
- `docs/modules/payments.md`
- `docs/modules/refunds.md`
- `docs/modules/receipts.md`
- `docs/modules/printing.md`
- `docs/modules/audit.md`
- `docs/architecture/payment-adapters.md`

## ADRs Required

- ADR-0005, ADR-0010, ADR-0011 (all already accepted). ADR-0005's Acceptance Addendum already names Stripe Terminal as the first provider — no new ADR is required.

## Open Issues Required

- None. OI-0001 (first payment provider → Stripe Terminal), OI-0004 (first receipt printer → Epson TM-T88VI), and OI-0005 (first payment terminal → Stripe BBPOS WisePOS E) are already resolved.

## Commit Sequence

```
feat(orders): add order service and state machine
feat(payments): add cash and manual EFTPOS payment
feat(payments): add payment ledger and idempotency
feat(refunds): add refund service with original payment link
feat(receipts): add receipt generation with tax markers
feat(printing): add ESC/POS printer service
feat(audit): add payment and refund audit logging
test(payments): add payment, refund, and receipt tests
docs: update payment, receipt, printing, and audit docs
```

## Handoff Notes

Depends on PLAN-0004 (Catalog/Tax). Payment tests require the tax engine to be working. The first integrated payment provider (Stripe Terminal) is implemented in [PLAN-0009](PLAN-0009-first-payment-adapter-stripe-terminal.md), which should proceed alongside or immediately after this plan's non-adapter work (order service, cash/manual EFTPOS, refunds, receipts, printing). Next parallel track: PLAN-0006 (Terminal, Display, PWA).
