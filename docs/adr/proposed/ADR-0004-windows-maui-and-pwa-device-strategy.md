# ADR-0004 — Windows MAUI and PWA Device Strategy

## Status

Proposed

## Context

Daxa POS must run on a range of devices: Windows counter POS terminals, customer-facing second displays, Linux kiosks, Android tablets, iPads, KDS screens, and admin portals. A single application type cannot serve all of these well without compromises.

Windows POS terminals require: full-screen/borderless mode, hardware integration (printers, cash drawers, EFTPOS terminals), second-window customer display, and performant touch UI on retail-grade hardware.

Non-Windows devices (Linux kiosks, tablets, KDS screens) need: easy deployment, cross-platform consistency, update agility, and minimal OS-specific dependencies.

## Decision

**Windows POS terminals use .NET MAUI:**
- `Daxa Terminal` is a .NET MAUI application for Windows.
- The customer-facing second display (`Daxa Display`) is a second MAUI window within the same application process — not a stretched single window across two monitors.
- MAUI provides native Windows integration for printers, peripherals, and full-screen mode.

**All other device types use PWA:**
- Admin/back-office portal (`Daxa Back Office`) is a PWA/web application.
- KDS screens (`Daxa KDS`) are PWA.
- Non-Windows POS fallback is PWA.
- Linux kiosks run PWA in Chromium kiosk mode.
- Android tablets and iPads use PWA.
- Future self-ordering kiosks use PWA with OS kiosk lockdown.

**Linux MAUI is not used for production:**
- .NET MAUI on Linux is not a reliable commercial baseline for venue deployments.

## Consequences

**Positive:**
- Windows POS gets the best native experience.
- Non-Windows devices get easy, update-friendly, cross-platform PWA.
- No separate Linux native app to maintain.
- Customer display is integrated with the terminal process (shared state).
- KDS can run on any networked device with a browser.

**Negative:**
- Two technology stacks (MAUI + web/PWA) must be maintained.
- MAUI updates and Windows-specific testing are required.
- PWA has limitations on some hardware APIs (though these are manageable for admin/KDS use cases).

## Alternatives Considered

1. **All PWA** — Rejected. PWA does not provide the native Windows integration required for counter POS hardware, cash drawers, and EFTPOS terminal pairing.
2. **Electron** — Considered but not preferred. Adds complexity, large bundle size, and is not idiomatic for .NET backends.
3. **WPF or WinForms** — Rejected. MAUI is the modern cross-platform path and provides better future portability if non-Windows MAUI improves.

## Open Questions

- See [OI-0009 — MAUI App Update Delivery](../../issues/open/OI-0009-maui-app-update-delivery.md)
- Which Windows POS hardware models will be targeted first?
- See [OI-0003 — Local Server Reference Hardware](../../issues/open/OI-0003-local-server-reference-hardware.md)

## Related Documents

- [Architecture: Device Strategy](../../architecture/device-strategy.md)
- [ADR-0008 — Device Identity vs User Identity](ADR-0008-device-identity-vs-user-identity.md)
- [PLAN-0006 — Terminal, Display, PWA](../../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [Module: Customer Display](../../modules/customer-display.md)
- [Module: KDS](../../modules/kds.md)
