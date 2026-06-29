# Module: Device and Terminal Service

The device service manages device registration, terminal assignment, and hardware configuration.

See also: `docs/modules/15-offline-device-admin-product-management.md`.

---

## Responsibilities

- Device registration (first-time setup).
- Terminal type assignment (POS, KDS, display, admin).
- Printer mapping per terminal.
- Payment terminal mapping per terminal.
- Display configuration per terminal.
- Device health status.
- App version tracking.
- Remote configuration delivery.
- Device deregistration.

## Device vs User Identity

Device identity is separate from user identity. A device is registered once and retains its configuration regardless of which staff member is logged in.

See [ADR-0008](../adr/proposed/ADR-0008-device-identity-vs-user-identity.md).

## Related Plans

- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
