# PLAN-0009 — First Payment Adapter: Stripe Terminal

## Status

Draft

## Goal

Deliver the first working integrated payment adapter for Daxa POS, following the provider-agnostic architecture in ADR-0005 and the provider/hardware decisions in OI-0001 and OI-0005: **Stripe Terminal**, tested against the **Stripe BBPOS WisePOS E**. This plan is the "separate sub-plan" referenced by PLAN-0005's Non-goals section for the first integrated payment provider.

This plan deliberately separates payment methods that need no adapter (cash, manual EFTPOS) from the one integrated adapter in scope for MVP (Stripe Terminal), and defers every other AU/NZ/APAC/global provider to later follow-up plans.

## Scope

- Cash payment recording (no adapter dependency).
- Manual external EFTPOS payment recording (no adapter dependency; staff confirm an amount taken on an external terminal, Daxa POS does not send anything to it).
- `IPaymentTerminalProvider` adapter interface, implemented in code per ADR-0005's conceptual interface.
- `DaxaPos.PaymentProviders.StripeTerminal` — the first concrete adapter.
- Stripe Terminal MVP flow (per ADR-0005's Acceptance Addendum): start a card payment from the POS, send the amount POS→terminal, receive success/failure/cancelled outcomes, record the result against the order, check terminal status where supported, cancel an in-progress payment where supported.
- Stripe BBPOS WisePOS E pairing/registration flow.
- Encrypted payment provider credential storage, per-location/per-terminal.

## Non-goals

- Tyro, Zeller, Square, Windcave, Adyen, Worldline, and Global Payments adapters — each gets its own follow-up plan once Stripe Terminal is working end to end.
- Terminal-based tipping, offline payment mode, provider-specific surcharge handling, advanced reconciliation, and multi-provider certification — explicitly deferred by ADR-0005's MVP Scope section.
- The general refund service (full/partial refunds, refund permissions) — that is PLAN-0005. This plan only needs enough of `RefundAsync` to prove the adapter round-trips a refund request.
- Receipt generation and printing — PLAN-0005.
- Order service / order state machine — PLAN-0005.

## Context Read

- `docs/adr/accepted/ADR-0005-payment-provider-adapter-architecture.md`
- `docs/issues/closed/OI-0001-first-payment-provider.md`
- `docs/issues/closed/OI-0005-first-payment-terminal-reference-device.md`
- `docs/adr/accepted/ADR-0010-financial-records-ledger-and-audit.md`
- `docs/regions/03-payment-provider-roadmap.md`
- `docs/integrations/payments/stripe-terminal.md`
- `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md`
- `docs/architecture/payment-adapters.md`

## Files Likely To Change

```
src/DaxaPos.Modules.Payments/                 (payment ledger, cash + manual EFTPOS recording)
src/DaxaPos.Application/                      (IPaymentTerminalProvider interface, adapter resolution)
src/DaxaPos.PaymentProviders.StripeTerminal/  (Stripe Terminal adapter implementation)
src/DaxaPos.Infrastructure/                   (provider credential encryption/storage)
```

## Architecture Assumptions

- The adapter resolves at runtime from per-location/per-terminal configuration, per ADR-0005 — no provider-specific code appears in the core order/payment flow.
- Payments are ledgered and idempotent (ADR-0010); this plan extends that ledger, it does not create a parallel one.
- Module communication with Orders/Receipts/Audit follows ADR-0014 (accepted): direct calls for synchronous confirmation, in-process domain events for side effects such as audit logging and future receipt triggering. The Stripe Terminal `StartPaymentAsync`/`RefundAsync` calls themselves are direct, user-initiated calls (not event-handler-triggered), so ADR-0014's Handler I/O Rule doesn't apply to them; it does apply if a future domain-event handler (e.g. reacting to `PaymentSucceededDomainEvent`) needs to call out to an external system — that must go through the outbox/`DaxaPos.Workers` path, not run inline.

## Domain Assumptions

- Cash and manual EFTPOS require no terminal API call — staff confirm the amount was taken on an external terminal; Daxa POS never sends an amount to it.
- Integrated (Stripe Terminal) payments always send the amount from POS to terminal — staff never manually type an amount into the Stripe terminal.
- A location may have zero or one Stripe Terminal connection for MVP. Multiple concurrent providers per location is a capability ADR-0005/CLAUDE.md already anticipate architecturally, but it is not required by this plan.

## Risks

- Stripe Terminal SDK/API specifics (connection token flow, reader discovery/pairing for the BBPOS WisePOS E) are not yet detailed in `docs/integrations/payments/stripe-terminal.md` — verify against current Stripe documentation before implementation.
- AU/NZ certification/compliance requirements for going live with Stripe Terminal need confirmation; sandbox/test-mode only is acceptable for this plan.
- Credential storage/encryption must be implemented before any real Stripe secret key is used, even in development.
- This plan depends on PLAN-0004 (Tax) being complete, since ledgered payment amounts reference taxed order totals.

## Implementation / Documentation Steps

1. Implement cash payment recording against the payment ledger.
2. Implement manual EFTPOS payment recording against the payment ledger.
3. Define `IPaymentTerminalProvider` in `DaxaPos.Application` and the adapter-resolution mechanism (per-location/per-terminal configuration lookup).
4. Implement `DaxaPos.PaymentProviders.StripeTerminal`: `StartPaymentAsync`, `RefundAsync`, `GetTerminalStatusAsync`, `CancelPaymentAsync`.
5. Implement the Stripe BBPOS WisePOS E pairing/registration flow.
6. Implement encrypted provider credential storage (Stripe secret key, location/terminal mapping).
7. Wire adapter results into the payment ledger (success/failure/cancelled outcomes recorded, one idempotency key per attempt).
8. Write adapter tests against Stripe's test/sandbox mode.
9. Write payment ledger tests for cash, manual EFTPOS, and Stripe Terminal outcomes.
10. Update `docs/modules/payments.md` and `docs/integrations/payments/stripe-terminal.md` with the implemented flow.

## Tests To Run Later

- Cash payment recorded correctly in the ledger.
- Manual EFTPOS payment recorded correctly in the ledger.
- Stripe Terminal: start-payment sends the correct amount to the sandbox terminal.
- Stripe Terminal: success/failure/cancelled outcomes recorded against the order.
- Stripe Terminal: terminal status check.
- Stripe Terminal: cancel an in-progress payment.
- Payment idempotency — retrying a request does not create a duplicate ledger entry or double-charge.
- Credential storage — secret key is never returned in API responses or written to logs.

## Documentation To Update

- `docs/modules/payments.md`
- `docs/integrations/payments/stripe-terminal.md`
- `docs/architecture/payment-adapters.md`

## ADRs Required

- ADR-0005 (already accepted, including the Stripe Terminal acceptance addendum). No new ADR is required.

## Open Issues Required

- None. OI-0001 and OI-0005 are already resolved.

## Commit Sequence

```
feat(payments): add cash payment recording
feat(payments): add manual EFTPOS payment recording
feat(payments): add IPaymentTerminalProvider interface and adapter resolution
feat(payments): add Stripe Terminal adapter (DaxaPos.PaymentProviders.StripeTerminal)
feat(payments): add Stripe BBPOS WisePOS E pairing flow
feat(payments): add encrypted payment provider credential storage
test(payments): add cash, manual EFTPOS, and Stripe Terminal ledger tests
docs: update payments and Stripe Terminal integration docs
```

## Handoff Notes

This plan depends on PLAN-0004 (Catalog/Tax) being complete, and should be executed alongside or immediately after the non-adapter parts of PLAN-0005 (order service, refund service, receipts, printing) — PLAN-0005 explicitly defers "full integrated payment provider implementations" to this plan.

Later provider adapters — Tyro, Zeller, Square, Windcave, then Adyen/Worldline/Global Payments per the roadmap in `docs/regions/03-payment-provider-roadmap.md` — should each get their own short follow-up plan once Stripe Terminal is working, reusing the same `IPaymentTerminalProvider` interface with no changes to the core payment flow.
