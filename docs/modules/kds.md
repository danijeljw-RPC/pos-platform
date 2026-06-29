# Module: KDS (Kitchen Display System)

Daxa KDS is the kitchen/bar/prep display system.

---

## Overview

- KDS screens are separate from the POS terminal and Daxa Display.
- KDS runs as a PWA on a dedicated screen device.
- KDS connects to the API via WebSocket/SignalR for real-time updates.
- KDS must rebuild its full state from the server after reconnect (missed events do not cause incorrect state).

## Responsibilities

- Display open orders for kitchen/bar/prep stations.
- Show order line details and modifiers.
- Allow staff to mark items as ready or complete.
- Filter orders by prep station or category routing.
- Real-time updates from the POS (new orders, modifications, cancellations).
- Full-state reload after reconnect.

## Deployment

- KDS runs in a browser on any connected device.
- A dedicated wall-mounted Android tablet, iPad, or Linux mini PC with display is typical.
- KDS does not require MAUI; it is a PWA.

## Status

- KDS is Phase 2 scope (not MVP).
- API endpoints for kitchen order routing must be designed in MVP for future use.

## Related Plans

- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [Architecture: Device Strategy](../architecture/device-strategy.md)
