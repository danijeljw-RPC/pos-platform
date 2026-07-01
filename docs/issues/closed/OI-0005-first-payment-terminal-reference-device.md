# OI-0005 — First Payment Terminal Reference Device

## Status

Closed

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
| -------- | -------------- |
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

- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)

## Related Documents

- [OI-0001 — First Payment Provider](OI-0001-first-payment-provider.md)
- [Module: Payments](../../modules/payments.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)

---

## Decision

The first payment terminal reference device for Daxa POS is the **Stripe BBPOS WisePOS E**.

This terminal will be used as the first integrated EFTPOS/payment hardware target for the Stripe Terminal adapter.

The POS will send the payment amount to the terminal through the payment provider adapter. Staff must not manually enter payment amounts into the terminal for integrated payment flows.

## Rationale

The Stripe BBPOS WisePOS E is selected because:

- It aligns with the selected first payment provider from OI-0001: **Stripe Terminal**.
- It is suitable as a countertop reference terminal for cafe, restaurant, bar, and hospitality environments.
- It provides a concrete hardware target for MVP testing.
- It allows the payment adapter interface in ADR-0005 to be validated against real terminal behaviour.
- It gives Daxa POS a known first reference device while leaving room for additional terminals later.

## Scope

The first implementation should focus on:

- Pairing or registering the terminal against the Stripe account/location.
- Sending POS-originated payment requests to the terminal.
- Receiving payment success/failure/cancellation results.
- Recording the payment result against the POS order.
- Supporting cancellation where Stripe Terminal allows it.
- Checking terminal status where supported.

The first implementation does not need to support every Stripe Terminal feature. Advanced features such as tipping, offline payments, terminal-driven surcharges, and multi-provider certification can be handled later.

## Outcome

- First reference device: **Stripe BBPOS WisePOS E**
- First provider: **Stripe Terminal**
- First adapter: `DaxaPos.PaymentProviders.StripeTerminal`
- Related ADR: ADR-0005 — Payment Provider Adapter Architecture

## Status Update

This open issue is resolved by selecting **Stripe BBPOS WisePOS E** as the first payment terminal reference device.

Status: **Closed**
