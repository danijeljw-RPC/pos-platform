# Testing: Payment Tests

Payment tests are mandatory. Payment logic must not be changed without test coverage.

---

## Required Test Categories

### Cash Payment

- Cash payment is recorded.
- Cash payment creates ledger entry.
- Cash payment is linked to order.
- Cash payment closes the order.

### Manual EFTPOS Payment

- Manual EFTPOS payment is recorded.
- Manual EFTPOS payment creates ledger entry.
- Manual EFTPOS payment is linked to order.

### Integrated Payment (Adapter)

- Adapter interface is called with correct amount.
- Approved response closes the order.
- Declined response leaves order open for retry.
- Cancelled response leaves order open.
- TimedOut response leaves order open.

### Idempotency

- Retry with same idempotency key does not create duplicate payment.
- Two different payments with different idempotency keys are both recorded.

### Split Payment

- Two payments on one order.
- First payment partially covers order total.
- Second payment covers remainder.
- Order closes when total paid meets or exceeds order total.

### Refund

- Full refund creates reversal record linked to original payment.
- Partial refund records correct refund amount.
- Refund amount cannot exceed original payment amount.
- Refund requires elevated role (authorisation test).

### Audit

- Cash payment creates audit event.
- Manual EFTPOS payment creates audit event.
- Refund creates audit event with reason and linked order/payment IDs.
- Void creates audit event.

---

## Test Project

```text
tests/DaxaPos.PaymentProvider.Tests/
tests/DaxaPos.IntegrationTests/
```

---

## Related Documents

- [ADR-0005 — Payment Provider Adapter Architecture](../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
- [Module: Payments](../modules/payments.md)
- [Module: Refunds](../modules/refunds.md)
