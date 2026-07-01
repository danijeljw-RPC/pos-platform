# Module: Payment Service

The payment service processes payments and maintains the payment ledger.

See also: `docs/modules/08-payments.md`.

---

## Responsibilities

- Cash payments.
- Manual external EFTPOS payments.
- Integrated EFTPOS payments (via payment adapter).
- Gift card payments (later).
- Store credit payments (later).
- Account sale / charge account (later).
- Deposits.
- Split payments (multiple payments per order).
- Payment provider selection and adapter resolution.
- Payment status tracking.
- Payment ledger (append-only).
- Idempotency keys.

## Payment Lifecycle

```text
Created → SentToTerminal → AwaitingCustomer
→ Approved / Declined / Cancelled / TimedOut
→ Recorded → OrderClosed or PaymentRetry
```

## Payment Entities

```text
Payment
PaymentMethod      (Cash, ManualEftpos, Integrated, GiftCard, StoreCredit)
PaymentStatus
PaymentLedgerEntry
```

## Key Rules

- Staff do not manually type amounts into integrated terminals.
- Payments are append-only (no silent edits).
- Each payment has an idempotency key.
- All payment activity is audited.

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [ADR-0005 — Payment Adapter Architecture](../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
