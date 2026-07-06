# ADR-0004 — Windows MAUI and PWA Device Strategy

## Status

Accepted

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

- See [OI-0009 — MAUI App Update Delivery](../../issues/closed/OI-0009-maui-app-update-delivery.md)
- Which Windows POS hardware models will be targeted first?
- See [OI-0003 — Local Server Reference Hardware](../../issues/closed/OI-0003-local-server-reference-hardware.md)

## Related Documents

- [Architecture: Device Strategy](../../architecture/device-strategy.md)
- [ADR-0008 — Device Identity vs User Identity](ADR-0008-device-identity-vs-user-identity.md)
- [PLAN-0006 — Terminal, Display, PWA](../../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [Module: Customer Display](../../modules/customer-display.md)
- [Module: KDS](../../modules/kds.md)

---

## Addendum — Open Question Answers (2026-06-30)

### OI-0009 — MAUI App Update Delivery

The production MAUI application update flow will be handled from the local Blazor SSR application running on the venue's Daxa Local server.

The local Blazor SSR application will check a public Daxa update feed to determine whether a newer published version of Daxa Terminal is available. When an update is available, the user will be shown the update option inside the local administration/deployment UI.

Updates are initiated manually. If the user chooses to update, they press an update button in the local Blazor SSR application. That action calls a daemon/tool running on the Linux Daxa Local machine. The daemon is responsible for connecting to each Windows POS device on the local network and performing the required installation/update tasks.

This update daemon is not required for the first implementation of the MAUI application. It will be built after-the-fact as part of the production deployment/update tooling.

The likely packaging strategy is:

- MSIX package preferred where practical.
- Alternative installer packaging may be considered where operationally simpler.
- A Winamp-style installer approach may be considered because it is open source and easy to package.

Code signing will use Microsoft's Azure code signing facility / Azure Trusted Signing where practical. The signing process should cover the MAUI binary artefacts and installer/package artefacts used for distribution.

Updates are not mandatory by default. Venue operators may defer or ignore updates. It would be unusual for Daxa to force an update. If a major security issue requires urgent customer action, Daxa will publish the advisory to customers via email and on the public website, but installation remains an operator-controlled action unless a future contract or deployment model explicitly requires managed updates.

### Windows POS Hardware Targeting

Daxa Terminal should be built to work across Windows POS hardware rather than being designed around one specific hardware model.

The first target is therefore a capability baseline, not a single model:

- Windows 11 capable x86-64 POS terminal or small form-factor PC.
- Touch-capable display where the device is used as a counter terminal.
- Support for full-screen / borderless operation.
- Support for a secondary monitor where Daxa Display is required.
- Network access to the local Daxa server.
- Ability to connect to required peripherals such as receipt printers, cash drawers, barcode scanners, and EFTPOS/payment terminals.
- Sufficient CPU, memory, and storage for a responsive MAUI application and local peripheral integration.

The MAUI UI must use responsive layouts so it can adapt to different screen sizes and retail-grade POS hardware. Hardware-specific work should be isolated behind device/peripheral integration layers rather than leaking into the main POS UI.

Formal hardware certification can happen later after the MAUI application works against the generic Windows capability baseline.

### OI-0003 — Local Server Reference Hardware

The Daxa Local server remains separate from the Windows POS terminal hardware decision.

The proposed local server baseline is:

- Minimum: 8GB RAM, 4-core Intel/AMD x86-64 CPU, 256GB NVMe SSD.
- Recommended: 16GB RAM, 4-core Intel/AMD x86-64 CPU, 512GB NVMe SSD.
- Production should use x86-64 hardware, not Raspberry Pi / ARM, unless separately tested and approved.
- Venues may supply their own hardware if it meets the published specification.
- Daxa may later publish a tested reference device list or supply a managed appliance, but the initial architecture should not require one specific vendor model.

This keeps Daxa Local supportable while avoiding unnecessary lock-in to one hardware vendor too early.
