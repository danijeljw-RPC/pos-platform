# ADR-0005 — Payment Provider Adapter Architecture

## Status

Accepted

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

```text
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

- See [OI-0001 — First Payment Provider](../../issues/closed/OI-0001-first-payment-provider.md)
- See [OI-0005 — First Payment Terminal Reference Device](../../issues/closed/OI-0005-first-payment-terminal-reference-device.md)

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [Module: Payments](../../modules/payments.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [Integrations: Tyro](../../integrations/payments/tyro.md)
- [Integrations: Stripe Terminal](../../integrations/payments/stripe-terminal.md)

---

## Acceptance Addendum

ADR-0005 is accepted.

The first implementation of the payment provider adapter architecture will use **Stripe Terminal** as the first payment provider and the **Stripe BBPOS WisePOS E** as the first payment terminal reference device.

This resolves the related open questions:

- OI-0001 — First Payment Provider: **Stripe Terminal**
- OI-0005 — First Payment Terminal Reference Device: **Stripe BBPOS WisePOS E**

## Accepted Implementation Direction

The core payment flow must remain provider-agnostic.

The first concrete provider adapter will be:

```text
DaxaPos.PaymentProviders.StripeTerminal
```

The Stripe Terminal adapter will implement the common payment terminal provider interface used by the POS payment flow.

The POS must not call Stripe-specific code directly from core order, checkout, or payment workflow logic. Stripe-specific behaviour must stay inside the Stripe Terminal adapter.

The first hardware implementation target will be:

```text
Stripe BBPOS WisePOS E
```

## MVP Scope

The first accepted implementation should support:

- Starting a card payment from the POS.
- Sending the payment amount from the POS to the terminal.
- Preventing staff from manually entering payment amounts for integrated payments.
- Receiving payment success, failure, and cancellation outcomes.
- Recording the payment result against the POS order.
- Checking terminal status where supported.
- Cancelling an in-progress payment where supported.

The first implementation does not need to support every provider-specific feature.

The following can be added later as provider extensions or follow-up work:

- Terminal-based tipping.
- Offline payment mode.
- Provider-specific surcharge handling.
- Advanced reconciliation.
- Multi-provider certification.
- Additional Stripe terminal models.
- Additional AU/NZ providers such as Tyro, Zeller, Square, or Windcave.

## Consequence

The ADR is now accepted with a phased implementation approach:

1. Build the provider-agnostic payment adapter interface.
2. Implement Stripe Terminal first.
3. Test against the Stripe BBPOS WisePOS E.
4. Keep the architecture open for future payment providers without changing the core POS payment flow.

## Status Update

Status: **Accepted**
