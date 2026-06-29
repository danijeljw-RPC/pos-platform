# ADR-0008 — Device Identity vs User Identity

## Status

Proposed

## Context

A POS terminal has a fixed hardware identity (it is registered to a venue, assigned a terminal role, printer mapping, and payment terminal mapping). The staff member using the terminal changes throughout the day — staff clock on and off, cover each other's breaks, and share terminals.

If device and user identity are conflated, the system cannot correctly attribute orders, voids, refunds, and audit events to the right person. Equally, terminal configuration (printer routing, payment terminal mapping, display assignment) would be disrupted by staff login/logout.

## Decision

Device identity and user identity are **separate concerns** in Daxa POS.

- A `Device` is registered to a `Location` and has a `TerminalId`, `DeviceType`, `PrinterMapping`, `PaymentTerminalMapping`, and `DisplayConfig`.
- A `User` logs into the device (via PIN or other credential) to start a session.
- The `Device` retains its configuration regardless of who is logged in.
- Audit events, orders, and payments capture both the `DeviceId`/`TerminalId` and the `UserId`.
- Staff PIN login allows fast user switching without requiring a full app restart or device reconfiguration.

## Consequences

**Positive:**
- Terminal configuration is stable regardless of staff changes.
- Audit trail captures both device and user context.
- Fast staff handover via PIN without disrupting device state.
- Correct attribution of orders, voids, refunds, and overrides to users.

**Negative:**
- Two identity concepts must be managed (device registration + user login).
- Device registration flow adds onboarding steps.

## Alternatives Considered

1. **Combined device+user identity** — Rejected. Cannot correctly attribute actions or maintain stable device configuration across staff sessions.
2. **No device identity, user-only** — Rejected. Device-specific configuration (printers, payment terminals) cannot be maintained per-user.

## Open Questions

- How will device registration tokens be issued and rotated?
- Should device registration require a human admin approval step?

## Related Documents

- [ADR-0009 — Identity Provider Strategy](ADR-0009-keycloak-or-identity-provider-strategy.md)
- [Architecture: Device Strategy](../architecture/device-strategy.md)
- [Module: Devices](../modules/devices.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
