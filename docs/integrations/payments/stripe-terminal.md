# Integration: Stripe Terminal

## Overview

Stripe Terminal is a global integrated payment terminal SDK. It provides a clean, well-documented API for sending payment amounts from a POS to a Stripe Terminal device. Stripe Terminal supports AU, NZ, US, UK, EU, and other regions.

## Status

Planned — adapter not yet implemented. Candidate for first integration.

## Region

AU, NZ, US, UK, EU, SG, and others — broad global coverage.

## Priority

AU/NZ MVP — recommended as the first integrated provider for developer experience and global coverage. See [OI-0001](../../issues/closed/OI-0001-first-payment-provider.md).

## Notes

- Stripe Terminal has excellent documentation and sandbox environment.
- Stripe Terminal SDKs available for iOS, Android, and JavaScript (web).
- .NET SDK is available or HTTP API can be used directly.
- Stripe Terminal hardware includes: BBPOS WisePOS E (countertop), Stripe Reader M2, BBPOS WisePad 3.
- Stripe Terminal supports AU EFTPOS and card schemes.

## Integration Points

- `DaxaPos.PaymentProviders.StripeTerminal`
- Implements `IPaymentTerminalProvider`.

## Related Documents

- [Architecture: Payment Adapters](../../architecture/payment-adapters.md)
- [ADR-0005 — Payment Provider Adapter Architecture](../../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
- [OI-0001 — First Payment Provider](../../issues/closed/OI-0001-first-payment-provider.md)
- [OI-0005 — First Payment Terminal Reference Device](../../issues/closed/OI-0005-first-payment-terminal-reference-device.md)
