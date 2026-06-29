# OI-0005 — First Payment Terminal Reference Device

## Status

Open

## Area

Devices / Hardware / Payments

## Summary

Which integrated EFTPOS/payment terminal model should be the first supported reference device for Daxa POS?

## Context

Daxa POS integrates with EFTPOS terminals via the payment adapter architecture (ADR-0005). The POS sends the payment amount to the terminal; staff do not manually enter amounts. A reference terminal model must be chosen and tested first.

The terminal choice is tightly linked to the first payment provider choice (OI-0001).

## Impact

- Determines the first physical terminal to pair with Daxa POS.
- Affects payment adapter implementation and testing.
- Affects certification requirements.
- Affects venue hardware purchasing guidance.

## Options

| Provider | Terminal Model |
|----------|---------------|
| Tyro | Tyro payment terminal (various models) |
| Zeller | Zeller Terminal |
| Square | Square Terminal or Square Reader |
| Stripe Terminal | Stripe BBPOS WisePOS E, Stripe Reader M2, or similar |
| Windcave | PX series terminals |

## Recommendation

Choose the terminal that corresponds to the chosen provider from OI-0001. If Stripe Terminal is chosen as the first provider, the **Stripe BBPOS WisePOS E** (countertop terminal) is a strong starting reference device for cafe/hospitality counters.

## Decision Needed

- Which terminal model is the first reference device.
- This decision depends on OI-0001 (first payment provider).

## Related ADRs

- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md)

## Related Documents

- [OI-0001 — First Payment Provider](OI-0001-first-payment-provider.md)
- [Module: Payments](../../modules/payments.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
