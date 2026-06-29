# Payment Adapter Architecture — Daxa POS

Daxa Payments uses a provider-agnostic adapter architecture. See also `docs/architecture/03-payment-adapter-architecture.md`.

See [ADR-0005](../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md) for the decision record.

---

## Adapter Model

```text
Daxa Payments
├─ IPaymentTerminalProvider (interface)
├─ TyroAdapter
├─ ZellerAdapter
├─ SquareAdapter
├─ StripeTerminalAdapter
├─ WindcaveAdapter
├─ AdyenAdapter
├─ WorldlineAdapter
└─ GlobalPaymentsAdapter
```

---

## Interface

```csharp
public interface IPaymentTerminalProvider
{
    Task<PaymentTerminalResult> StartPaymentAsync(StartPaymentRequest request);
    Task<PaymentTerminalResult> RefundAsync(RefundPaymentRequest request);
    Task<PaymentTerminalStatus> GetTerminalStatusAsync(string terminalId);
    Task CancelPaymentAsync(string paymentRequestId);
}
```

---

## Payment Request Lifecycle

```text
Created
↓
SentToTerminal
↓
AwaitingCustomer
↓
Approved / Declined / Cancelled / TimedOut
↓
Recorded
↓
OrderClosed or PaymentRetry
```

---

## Provider Configuration

Each venue configures its payment provider per location/terminal:

```text
Venue: Main Street Cafe
Location: Sydney CBD
POS Terminal: Counter 1
Payment Provider: Zeller
EFTPOS Terminal: Zeller-BAR-01
```

---

## Provider Definition

Each adapter must declare:

- Supported regions and countries.
- Supported currencies.
- Supported payment types.
- Terminal pairing method.
- Credential fields.
- Refund capability.
- Partial refund capability.
- Tip capability.
- Surcharge capability.
- Offline payment capability.
- Webhook/result model.
- Settlement/reporting support.
- Certification requirements.

---

## Provider Roadmap

| Region | Priority Providers |
|--------|--------------------|
| AU/NZ MVP | Tyro, Zeller, Square, Stripe Terminal, Windcave |
| APAC | Adyen, Windcave, Worldline, Global Payments |
| UK | Stripe Terminal, Square, Adyen |
| US/CA | Stripe Terminal, Square, Adyen |

---

## Open Questions

- See [OI-0001 — First Payment Provider](../issues/open/OI-0001-first-payment-provider.md)
- See [OI-0005 — First Payment Terminal Reference Device](../issues/open/OI-0005-first-payment-terminal-reference-device.md)

---

## Related Documents

- [ADR-0005 — Payment Provider Adapter Architecture](../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md)
- [Architecture: 03-payment-adapter-architecture.md](03-payment-adapter-architecture.md)
- [Module: Payments](../modules/payments.md)
- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
