# Integration: Tyro

## Overview

Tyro is an AU-focused payment provider with strong hospitality and retail presence. Tyro offers integrated EFTPOS with a proprietary SDK and terminal range.

## Status

Planned — adapter not yet implemented. Pending resolution of [OI-0001](../../issues/closed/OI-0001-first-payment-provider.md).

## Region

Australia (primary).

## Priority

AU/NZ MVP — high priority for commercial fit.

## Notes

- Tyro integration requires Tyro merchant account and certification.
- Tyro terminal pairing uses a Tyro-specific pairing protocol.
- Tyro SDK available for Windows and web.
- Tyro has strong AU hospitality adoption (cafes, restaurants, pubs).

## Integration Points

- `DaxaPos.PaymentProviders.Tyro`
- Implements `IPaymentTerminalProvider`.

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
- [OI-0001 — First Payment Provider](../../issues/closed/OI-0001-first-payment-provider.md)
