# Integration: Zeller

## Overview

Zeller is a modern AU payment provider offering integrated EFTPOS, business banking, and debit cards. Zeller has a clean REST API and modern developer experience.

## Status

Planned — adapter not yet implemented. Pending resolution of [OI-0001](../../issues/closed/OI-0001-first-payment-provider.md).

## Region

Australia.

## Priority

AU/NZ MVP — high priority for commercial fit and developer experience.

## Notes

- Zeller offers REST API for terminal integration.
- Zeller Terminal is a modern countertop EFTPOS device.
- Growing hospitality and retail adoption in AU.
- Zeller also offers business banking (BNPL/debit) — payment integration only for Daxa POS.

## Integration Points

- `DaxaPos.PaymentProviders.Zeller`
- Implements `IPaymentTerminalProvider`.

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
- [OI-0001 — First Payment Provider](../../issues/closed/OI-0001-first-payment-provider.md)
