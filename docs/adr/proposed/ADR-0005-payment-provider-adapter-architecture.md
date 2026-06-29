# ADR-0005 — Payment Provider Adapter Architecture

## Status

Proposed

## Context

Daxa POS must support multiple payment providers across different regions: AU/NZ (Tyro, Zeller, Square, Stripe Terminal, Windcave), APAC (Adyen, Worldline, Global Payments), UK, and North America. Different venues will connect their own payment provider accounts.

If the platform hard-codes any single provider, switching or adding providers later requires changes to core order and payment logic, which is high risk in production.

## Decision

All payment provider integrations use a **provider-agnostic adapter architecture** (`Daxa Payments`).

- A common `IPaymentTerminalProvider` interface is defined.
- Each payment provider implements this interface in its own adapter assembly.
- The POS payment flow calls the interface; it does not call provider-specific code directly.
- Venues configure their chosen provider and credentials per location/terminal.
- The adapter resolves at runtime based on configuration.

**Adapter interface (conceptual):**

```csharp
public interface IPaymentTerminalProvider
{
    Task<PaymentTerminalResult> StartPaymentAsync(StartPaymentRequest request);
    Task<PaymentTerminalResult> RefundAsync(RefundPaymentRequest request);
    Task<PaymentTerminalStatus> GetTerminalStatusAsync(string terminalId);
    Task CancelPaymentAsync(string paymentRequestId);
}
```

**Provider modules:**

```
DaxaPos.PaymentProviders.Tyro
DaxaPos.PaymentProviders.Zeller
DaxaPos.PaymentProviders.Square
DaxaPos.PaymentProviders.StripeTerminal
DaxaPos.PaymentProviders.Windcave
DaxaPos.PaymentProviders.Adyen
DaxaPos.PaymentProviders.Worldline
DaxaPos.PaymentProviders.GlobalPayments
```

Staff do not manually enter payment amounts into integrated terminals. The POS sends the amount automatically.

## Consequences

**Positive:**
- New providers can be added without changing the core payment flow.
- Venues can switch providers without code changes.
- Each provider can be tested in isolation.
- Multiple providers can be active at the same time across different terminals/locations.

**Negative:**
- Interface must be designed carefully to accommodate provider feature differences (tips, surcharges, offline capability).
- Provider certification requirements differ by region and must be tracked.
- Some provider-specific capabilities (e.g. tip prompt on terminal) may need provider extensions.

## Alternatives Considered

1. **Single-provider MVP** — Partially adopted as a phased approach, but the adapter architecture must be in place from the start.
2. **Direct integration without adapter** — Rejected. Creates tight coupling that is expensive to unpick later.

## Open Questions

- See [OI-0001 — First Payment Provider](../../issues/open/OI-0001-first-payment-provider.md)
- See [OI-0005 — First Payment Terminal Reference Device](../../issues/open/OI-0005-first-payment-terminal-reference-device.md)

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [Module: Payments](../../modules/payments.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [Integrations: Tyro](../../integrations/payments/tyro.md)
- [Integrations: Stripe Terminal](../../integrations/payments/stripe-terminal.md)
