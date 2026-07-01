# Module: Customer Display (Daxa Display)

Daxa Display is the customer-facing second screen at the POS counter.

Daxa Display is **not** the KDS.

---

## Overview

- Daxa Display runs as a second MAUI window within the same process as Daxa Terminal (the staff POS app).
- It shares order state via a shared `CurrentOrderService` within the MAUI app process.
- It is displayed on a secondary monitor attached to the Windows POS machine.
- Do not stretch one giant app window across two screens — use two separate MAUI windows.

## Display States

| State | Content |
|-------|---------|
| Idle | Venue logo, promotions, daily specials |
| Order building | Item list, quantities, running total |
| Item removed | Updated basket |
| Discount applied | Discount line added |
| Surcharge applied | Surcharge line added |
| Payment started | "Please tap / insert / swipe" |
| Payment approved | "Payment approved — Thank you!" |
| Payment declined | "Payment declined — please see staff" |
| Receipt options | QR code / email / SMS option |
| Loyalty prompt | Scan membership QR code |
| Tip prompt | Gratuity options (US/CA markets, later) |

## Responsibilities

- Reflect current order state in real time.
- Show payment progress.
- Show receipt options.
- Show idle branding.
- Show loyalty prompts (later).
- Show tip prompts (US/CA, later).

## Related Plans

- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [Architecture: Device Strategy](../architecture/device-strategy.md)
- [ADR-0004 — Windows MAUI and PWA Device Strategy](../adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md)
