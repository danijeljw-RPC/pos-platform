# PLAN-0006 - Terminal, Display, and PWA

## Status

Milestone A - Complete.

See `docs/plans/active/PLAN-0006-worker-notes.md` for the full Milestone A implementation report.
Summary: `src/DaxaPos.Web` (standalone Blazor WebAssembly PWA) scaffolded with device-setup, staff
PIN login, session-backed shell, and route guarding; `tests/DaxaPos.Web.Tests` added (bUnit +
xunit, 31 tests); one small, documented backend change (CORS) to let the new browser-origin client
call the API. No migrations. Milestone B (Back Office skeleton, device PIN generation) is next.

This revision records the current human decisions:

- PLAN-0006 is PWA-first.
- Blazor is the only approved PWA/frontend framework.
- MAUI is deferred to a later dedicated Windows terminal plan.
- PWA can also operate as a terminal on supported devices.
- Blazor hosting model: **standalone Blazor WebAssembly** (not Blazor Server, not Auto render
  mode). Confirmed with human 2026-07-06 during Milestone A kickoff. Rationale: real PWA
  installability and future offline resilience (CLAUDE.md decision 11) require a client that can
  run without a live connection to a host process; Blazor Server's Interactive Server render mode
  needs a constant SignalR connection to the host and does not meet that bar. `docs/architecture/overview.md`
  already flagged "Blazor WASM (TBD)" for the PWA surfaces, so this closes that TBD.

## Goal

Implement the user-facing web/PWA layer that consumes the completed PLAN-0004 and PLAN-0005
backend foundations.

PLAN-0006 delivers:

- Blazor/PWA shell.
- Back Office PWA skeleton.
- Device registration PIN generation/viewing.
- POS Terminal PWA sales screen.
- Payment and receipt flow for cash/manual EFTPOS.
- Customer display/display mode.
- Minimal KDS PWA board.
- Consolidation, RBAC UX sweep, and documentation closeout.

## Non-Goals

- MAUI implementation.
- Windows installer/update mechanism.
- Local Windows USB printer support.
- Stripe Terminal/integrated payment UI.
- PLAN-0009 hardware/provider/device orchestration.
- Printer discovery.
- OI-0018 production routing implementation.
- Real KDS kitchen-ticket lifecycle.
- Full Back Office CRUD over every admin endpoint.
- PLAN-0007 offline/sync/local-hybrid data conflict handling.

## Human Decisions Recorded

| Decision | Outcome |
|----------|---------|
| UI sequence | PWA-first. |
| PWA framework | Blazor only. No React/Vue/Angular. |
| Terminal surface | PWA terminal first; MAUI terminal later. |
| MAUI | Deferred to a future dedicated Windows terminal plan. |
| Card payment UI | Show disabled placeholder only if useful; no integrated flow until PLAN-0009. |
| Refund UI | Deferred unless explicitly requested later. |
| Back Office first pass | Read-mostly plus device-registration-PIN generation/viewing. |
| KDS first pass | Minimal read-only board only. |

## Context

PLAN-0006 consumes:

- PLAN-0003 device registration and Staff PIN login endpoints.
- PLAN-0004 catalog/menu/tax/pricing foundations.
- PLAN-0005 order/payment/refund/receipt/printing foundations.

Open issues that remain open:

- OI-0017 - product archive-and-replace concurrency.
- OI-0018 - location-scoped production printer routing.

Neither issue blocks the first PWA milestones.

## Files Likely To Change

The exact project names should follow the existing solution conventions, but the plan expects new
Blazor/PWA UI projects, for example:

```text
src/DaxaPos.Web/                 (or existing Blazor host if one already exists)
src/DaxaPos.BackOfficePwa/       (if split from terminal shell)
src/DaxaPos.TerminalPwa/         (if split from back office)
src/DaxaPos.KdsPwa/              (if split from back office)
```

Do not create MAUI projects in PLAN-0006.

## Architecture Assumptions

- Server remains authoritative for order/payment/refund/receipt state.
- UI must not recalculate tax, pricing, discounts, or order totals.
- UI uses server-returned totals and receipt documents.
- UI reflects server-side RBAC failures gracefully.
- Blazor/PWA surfaces share the same API.
- PWA installability is required, but offline data conflict handling belongs to PLAN-0007.
- Realtime is optional for early milestones; polling/manual refresh is acceptable where scoped.

## Milestones

### Milestone A - Blazor/PWA Shell, Auth, Session, Device Context — Complete

Scope:

- Create or extend the Blazor/PWA app shell.
- Establish API client foundation.
- Establish authentication/session handling.
- Establish device context handling against existing PLAN-0003 endpoints.
- Establish Staff PIN login flow against existing `/api/v1/auth/staff-pin/login`.
- End at a logged-in shell with no sales screen yet.

Deliverables:

- Blazor/PWA app scaffold.
- Layout/navigation shell.
- Device context storage.
- Staff PIN login screen.
- Session expiry/logout handling.
- Basic 401/403 handling.

Explicitly out of scope:

- MAUI.
- Sales screen.
- Payments.
- Customer display.
- KDS.
- Printer routing.
- Backend schema changes.

### Milestone B - Back Office PWA Skeleton And Device PIN Management

Scope:

- Back Office shell.
- Device-registration-PIN generation/viewing.
- Read-mostly admin views needed to support terminal setup.

Deliverables:

- Device registration PIN screen.
- Basic device/location context views.
- Read views for catalog/menu/location data as needed for setup.

Explicitly out of scope:

- Full catalog/tax/pricing CRUD sweep.
- Printer route configuration UI.
- User/role management UI unless directly required for PIN management.

### Milestone C - POS Terminal PWA Sales Screen

Scope:

- Staff-facing POS order-entry screen.
- Product tiles from resolved-menu endpoint.
- Basket/order-line management.
- Modifiers.
- Quantity.
- Notes.
- Hold/resume.
- Void line.
- Void/cancel order.

Rules:

- Use PLAN-0004 resolved-menu data.
- Use PLAN-0005 order APIs.
- Do not recalculate pricing/tax client-side.
- Display server-computed totals.

Explicitly out of scope:

- Payments.
- Customer display.
- Table/floor plan.
- Split bills beyond existing payment API support.
- Production printer routing.

### Milestone D - Payment And Receipt Flow In PWA

Scope:

- Cash payment.
- Manual EFTPOS payment.
- Split payment UI if supported cleanly by existing APIs.
- Receipt view.
- Receipt reprint.
- Optional disabled Card placeholder.

Rules:

- Cash/manual EFTPOS only.
- No integrated Stripe Terminal flow.
- No provider/device pairing.
- No local printer access.
- Receipt view consumes server `ReceiptDocument`.
- Reprint uses existing `receipts.reprint`.

Explicitly out of scope:

- Refund screen by default.
- Stripe Terminal.
- PLAN-0009.
- MAUI.
- USB/local printer support.

### Milestone E - Customer Display / Display Mode In PWA

Scope:

- Customer-facing display mode using the current order/payment state.
- This may be a second browser window, display route, or browser-display mode.

Deliverables:

- Idle state.
- Order-building state.
- Payment state.
- Completion/receipt state.

Explicitly out of scope:

- MAUI second window.
- Customer input.
- Loyalty/tip prompts unless explicitly added later.

### Milestone F - Minimal KDS PWA Board

Scope:

- Read-only open-orders board.
- Manual refresh or simple polling.
- Reuse existing order list/read endpoints if sufficient.

Rules:

- This is not a real kitchen-ticket lifecycle system.
- No station routing.
- No mark-ready/complete.
- No OI-0018 implementation.
- If existing endpoints are insufficient, document the gap or create a narrow open issue rather
  than expanding scope silently.

### Milestone G - Consolidation, RBAC UX Sweep, Documentation Closeout

Scope:

- UX polish across all PLAN-0006 screens.
- 401/403 handling review.
- Device/session expiry review.
- Docs update.
- Handoff notes for PLAN-0007, PLAN-0009, MAUI terminal, and OI-0018.

Explicitly out of scope:

- New feature work.

## RBAC / Staff PIN Expectations

- Server remains the authority.
- UI must treat server 401/403 as expected states, not crashes.
- `orders.manage`, `payments.record`, and `receipts.reprint` are staff-PIN-eligible.
- `payments.refund` is `AdminSensitive` and is not staff-PIN-eligible.
- UI permission checks are only UX hints. Security is enforced server-side.

## Printer And Hardware Boundaries

PLAN-0006 does not implement printer routing or hardware integration.

- Server-side network receipt printing already exists from PLAN-0005.
- Production printer routing is OI-0018.
- USB/local Windows printer access belongs to future MAUI terminal work.
- Printer discovery and hardware/device orchestration belong to PLAN-0009 or a later hardware plan.

## PWA / Offline Considerations

PLAN-0006 may implement:

- PWA installability.
- Basic asset caching.
- Reconnect-friendly UI behaviour.

PLAN-0006 does not implement:

- Offline order queueing.
- Conflict resolution.
- Local authoritative database.
- Sync engine.

Those belong to PLAN-0007.

## Tests To Run Later

- PWA shell loads.
- Staff PIN login succeeds/fails cleanly.
- Device context is stored and restored.
- Device PIN generation works in Back Office.
- POS terminal can create and edit orders.
- Payment flow records cash/manual EFTPOS payments.
- Receipt view and reprint work.
- Customer display reflects the active order.
- Minimal KDS board can load current open orders.
- 401/403 states degrade gracefully.

## Documentation To Update During Implementation

- `docs/modules/orders.md`
- `docs/modules/payments.md`
- `docs/modules/receipts.md`
- `docs/modules/customer-display.md`
- `docs/modules/kds.md`
- `docs/architecture/device-strategy.md`
- deployment docs for the Blazor/PWA host

## Handoff Notes

Recommended next implementation session:

Start PLAN-0006 Milestone A only: Blazor/PWA shell, authentication/session/device context, and
Staff PIN login.

Do not start MAUI.

Do not start PLAN-0009.

Do not implement OI-0018.
