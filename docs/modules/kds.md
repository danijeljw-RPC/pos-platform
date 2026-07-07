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

- A minimal, read-only KDS board shipped in PLAN-0006 Milestone F (2026-07-07):
  `src/DaxaPos.Web/Pages/Kds.razor` (`/kds`), staff-facing (sits in the existing Terminal shell,
  requires the same device+session as `/sales`). Polls `GET /api/v1/orders?locationId=` (no new
  backend endpoint — the existing PLAN-0005 order-list endpoint was already sufficient), filters to
  `Open`/`Held` orders client-side, sorts oldest-first, and shows each order's number, status, opened
  time, and active lines (quantity, product name, modifiers, notes).
- This is explicitly **not** the real kitchen-ticket lifecycle described below: no station/
  production routing (OI-0018 remains open), no mark-ready/mark-complete, no real-time push (a plain
  poll only). Full lifecycle scope (station filtering, ready/complete actions, WebSocket/SignalR
  real-time updates, full-state reload after reconnect) remains Phase 2 scope, not MVP.
- Known simplification: the client fetches every order at the location on each poll (no server-side
  status filter) and filters to `Open`/`Held` in the browser, since no existing test proves the
  `status` query parameter's enum-from-query-string binding. A future OI should confirm and use
  server-side status filtering (or add pagination/date-bounding) once order history at a location
  grows large. See `docs/plans/active/PLAN-0006-worker-notes.md`'s Milestone F report for detail.
- API endpoints for kitchen order routing (station/production routing) must still be designed for
  future use — OI-0018.

## Related Plans

- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [Architecture: Device Strategy](../architecture/device-strategy.md)
