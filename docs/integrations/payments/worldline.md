# Integration: Worldline

## Overview

Worldline is a global payment services company with presence across Europe, APAC, and other regions. Worldline offers integrated EFTPOS terminals and an API for POS integration.

## Status

Planned — adapter not yet implemented. Target for APAC expansion.

## Region

APAC, EU, and others.

## Priority

Phase 3+ (APAC expansion).

## Notes

- Worldline has presence in AU/NZ and Singapore.
- Terminal integration uses Worldline's commerce API.
- Suitable for enterprise and government-adjacent customers.

## Integration Points

- `DaxaPos.PaymentProviders.Worldline`
- Implements `IPaymentTerminalProvider`.

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
- [Region: Payment Provider Roadmap](../../regions/03-payment-provider-roadmap.md)
