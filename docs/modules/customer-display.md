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

## Implementation Status (PLAN-0006 Milestone E, 2026-07-07)

The overview and states above describe the **target MAUI second-window implementation** for a
Windows POS terminal. That does not exist yet — MAUI is deferred to a future dedicated Windows
terminal plan (CLAUDE.md). What exists today is a **PWA interim implementation**, built ahead of
MAUI so the display concept is usable now:

- `src/DaxaPos.Web/Pages/Display.razor` (`/display`, `@layout DisplayLayout`) — a plain browser
  tab/window opened from the Terminal shell's nav menu (`target="_blank"`), not a second MAUI
  `Window` in the same process.
- State is **not** shared via an in-process `CurrentOrderService` — the display tab polls the
  same device-scoped `IDraftOrderStore` localStorage pointer `Sales.razor`/`Pay.razor` read/write,
  then resolves it against the already-terminal-scoped `GetOrderAsync`/`GetPaymentsAsync`/
  `GetReceiptAsync` endpoints. This only works because both tabs share one browser origin's
  `localStorage` on the same physical device — the PWA analogue of the MAUI same-process model,
  not a literal implementation of it.
- Three states are implemented: idle, order-building/payment (server-computed lines/total/balance
  due), and completed/receipt (`ReceiptResult` rendered verbatim, "Payment approved — Thank you!").
  Item-removed/discount-applied/surcharge-applied/payment-started/receipt-options/loyalty/tip
  states from the table above are **not** implemented — no discount/surcharge line items or
  receipt-options data exist in the client DTOs yet, and there is no "payment started" signal
  distinct from "order still open" in a cash/manual-EFTPOS-only flow.
- No SignalR/realtime — a plain poll (`[Parameter] TimeSpan PollInterval`, default 4s), per
  PLAN-0006's hard rule.
- No separate customer login/authorization — rides on the browser tab's existing device/session
  context; a 401/403/404 on any poll degrades to idle rather than an error (CLAUDE.md's "UI
  permission checks are only UX hints — security is enforced server-side").

See `docs/plans/active/PLAN-0006-worker-notes.md`'s Milestone E report for full detail, including
the "sticky-completed" rule (a receipt keeps showing after `Pay.razor` clears the draft pointer,
until a genuinely new order starts).

## Related Plans

- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [Architecture: Device Strategy](../architecture/device-strategy.md)
- [ADR-0004 — Windows MAUI and PWA Device Strategy](../adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md)
