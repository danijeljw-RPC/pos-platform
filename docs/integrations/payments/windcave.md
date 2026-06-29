# Integration: Windcave

## Overview

Windcave (formerly Payment Express) is an AU/NZ payment provider with strong regional coverage across Australia, New Zealand, and parts of APAC. Windcave supports integrated EFTPOS and has a well-established presence in AU/NZ hospitality and retail.

## Status

Planned — adapter not yet implemented. Pending resolution of [OI-0001](../../issues/open/OI-0001-first-payment-provider.md).

## Region

Australia, New Zealand, and APAC.

## Priority

AU/NZ MVP — medium-high priority for regional depth.

## Notes

- Windcave supports a range of EFTPOS terminal hardware.
- Windcave has REST and legacy XML API options.
- Good coverage of AU/NZ card schemes and transaction types.

## Integration Points

- `DaxaPos.PaymentProviders.Windcave`
- Implements `IPaymentTerminalProvider`.

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/proposed/ADR-0005-payment-provider-adapter-architecture.md)
- [OI-0001 — First Payment Provider](../../issues/open/OI-0001-first-payment-provider.md)
