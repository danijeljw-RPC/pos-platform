# OI-0001 — First Payment Provider

## Status

Open

## Area

Payments

## Summary

Which AU/NZ payment provider should be integrated first as the first live EFTPOS integration in Daxa POS?

## Context

Daxa POS supports multiple payment providers via the adapter architecture (ADR-0005). For the MVP, at least one integrated provider is required beyond cash and manual EFTPOS.

AU/NZ candidate providers are: Tyro, Zeller, Square, Stripe Terminal, and Windcave.

Each has different developer experience, market presence, certification requirements, and business models.

## Impact

- Determines which provider SDK to integrate first.
- Affects the scope of certification and testing for MVP.
- Affects commercial partner relationships.
- Determines which hardware (payment terminal model) is tested first.

## Options

1. **Tyro** — Strong AU commercial presence, especially in hospitality. Requires Tyro certification. Good SDK.
2. **Zeller** — Growing AU presence, good developer experience, modern API.
3. **Square** — Global, clean API, Stripe Terminal-style developer flow. Lower AU market depth in hospitality.
4. **Stripe Terminal** — Global, excellent developer experience, good for technical teams. AU/NZ support available.
5. **Windcave** — AU/NZ focused, strong in hospitality and retail. Good regional coverage.

## Recommendation

Start with **Stripe Terminal** for developer speed and API quality during MVP development, then add **Tyro** or **Zeller** for AU commercial launch.

This is a recommendation only — human decision required.

## Decision Needed

Which provider to integrate first, and what hardware terminal model to certify first.

## Related ADRs

- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md)

## Related Documents

- [OI-0005 — First Payment Terminal Reference Device](OI-0005-first-payment-terminal-reference-device.md)
- [Integration: Tyro](../../integrations/payments/tyro.md)
- [Integration: Stripe Terminal](../../integrations/payments/stripe-terminal.md)
- [Region: Payment Provider Roadmap](../../regions/03-payment-provider-roadmap.md)
