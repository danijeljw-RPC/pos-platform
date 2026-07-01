# Device Strategy — Daxa POS

Daxa POS uses .NET MAUI for Windows POS terminals and PWA for all other device types.

See [ADR-0004](../adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md) for the decision record.

---

## Device Types and App Assignments

| Device / OS | App Type | Notes |
| ----------- | -------- | ----- |
| Windows POS terminal | .NET MAUI (Daxa Terminal) | Native Windows, full-screen/borderless |
| Windows customer-facing display | .NET MAUI second window (Daxa Display) | Same process as Daxa Terminal |
| Linux mini PC / kiosk | PWA in Chromium kiosk mode | No MAUI on Linux for production |
| Android tablet | PWA | |
| Android mobile | PWA | |
| iPad | PWA | |
| iPhone | PWA | |
| KDS screen (any OS) | PWA (Daxa KDS) | Separate device/session |
| Admin / back-office | PWA (Daxa Back Office) | Web browser |
| Self-ordering kiosk | PWA + OS kiosk lockdown | Future |

---

## Daxa Terminal (MAUI)

- Staff-facing POS sales screen.
- Full-screen / borderless mode on Windows.
- Hardware integration: printers, cash drawers, EFTPOS terminal pairing, barcode scanners.
- Device registration on first launch.
- Staff PIN login.
- Local cache for offline/resilience.

## Daxa Display (MAUI Second Window)

- Customer-facing display at the POS counter.
- Runs as a second `Window` in the same MAUI app process as Daxa Terminal.
- Shares order state via `CurrentOrderService`.
- States: Idle, OrderBuilding, PaymentStarted, PaymentApproved, PaymentDeclined, Receipt, Loyalty, TipPrompt.
- Do not stretch the POS app across two monitors.

## Daxa KDS (PWA)

- Kitchen/bar preparation display.
- Separate device from the POS counter.
- Connects to the API via WebSocket/SignalR.
- Must rebuild state from the server after reconnect.

## Daxa Back Office (PWA)

- Admin and management portal.
- Product management, venue settings, reporting, staff management.
- Accessible from any browser.

---

## Device Identity vs User Identity

Device identity and user identity are separate:

- A registered `Terminal` has its own token and configuration.
- A `User` logs in to the terminal for their session.
- Terminal configuration (printers, payment terminal) is not affected by user login/logout.

See [ADR-0008](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md).

---

## Open Questions

- See [OI-0009 — MAUI App Update Delivery](../issues/closed/OI-0009-maui-app-update-delivery.md)
- See [OI-0003 — Local Server Reference Hardware](../issues/closed/OI-0003-local-server-reference-hardware.md)

---

## Related Documents

- [ADR-0004 — Windows MAUI and PWA Device Strategy](../adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md)
- [ADR-0008 — Device Identity vs User Identity](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)
- [Module: Devices](../modules/devices.md)
- [Module: Customer Display](../modules/customer-display.md)
- [Module: KDS](../modules/kds.md)
- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
