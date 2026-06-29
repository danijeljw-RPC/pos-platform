# Integration: Square Terminal

## Overview

Square offers integrated EFTPOS terminals and a clean API for sending payment amounts from a POS to a Square Terminal device. The Square Terminal API allows the POS to initiate a payment on the terminal without requiring staff to manually enter the amount.

## Status

Planned — adapter not yet implemented. Pending resolution of [OI-0001](../../issues/open/OI-0001-first-payment-provider.md).

## Region

Australia, New Zealand, US, Canada, UK, and others.

## Priority

AU/NZ MVP — medium priority. Good for multi-region coverage.

## Important Notes

- The Square Terminal API must be used so that amounts flow from Daxa POS to the Square Terminal — staff must not manually enter amounts.
- The Square POS app must not replace Daxa Terminal as the main POS.
- Square Reader and Square Terminal are different hardware options.

## Integration Points

- `DaxaPos.PaymentProviders.Square`
- Implements `IPaymentTerminalProvider`.
- Uses Square Terminal API (not Square POS app).

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md)
- [OI-0001 — First Payment Provider](../../issues/open/OI-0001-first-payment-provider.md)
- [Region: Square Terminal Notes](../../regions/04-square-terminal-notes.md)
