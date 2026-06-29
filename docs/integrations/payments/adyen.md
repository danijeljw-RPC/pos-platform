# Integration: Adyen

## Overview

Adyen is a global enterprise payment platform with a unified commerce terminal API. Adyen is suitable for enterprise-grade multi-location and global deployments. Adyen Terminal API supports sending payment amounts from a POS to an Adyen payment terminal.

## Status

Planned — adapter not yet implemented. Lower priority for AU/NZ MVP. Target for APAC expansion and enterprise customers.

## Region

Global — AU, NZ, Singapore, Hong Kong, UK, US/CA, EU.

## Priority

Phase 3+ (APAC expansion and enterprise).

## Notes

- Adyen Terminal API provides a unified integration across Adyen's terminal range.
- Adyen requires a merchant agreement and certification.
- Adyen hardware includes the Adyen UX series and AMS1 terminals.
- Strong for multi-region, multi-currency enterprise deployments.

## Integration Points

- `DaxaPos.PaymentProviders.Adyen`
- Implements `IPaymentTerminalProvider`.

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md)
- [Region: Payment Provider Roadmap](../../regions/03-payment-provider-roadmap.md)
