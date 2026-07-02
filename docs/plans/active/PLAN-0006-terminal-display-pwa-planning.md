# PLAN-0006 — Terminal, Display, and PWA

## Status

Draft

## Goal

Implement the Daxa Terminal (.NET MAUI Windows POS app), Daxa Display (second MAUI window for customer-facing display), and the PWA foundations for Daxa Back Office and Daxa KDS.

## Scope

- `DaxaPos.PosMaui` — .NET MAUI app for Windows POS terminal.
- Staff POS sales screen (order entry, product tiles, modifiers, payments).
- `DaxaPos.DisplayMaui` or second MAUI window for customer display.
- Customer display states (idle, order building, payment, receipt).
- PWA admin portal skeleton (Daxa Back Office).
- PWA KDS skeleton (Daxa KDS).
- Device registration flow in MAUI app.
- Staff PIN login in MAUI app.

## Non-goals

- Full floor plan.
- Full KDS implementation.
- Self-ordering kiosk.
- Non-Windows MAUI deployment.

## Context Read

- `docs/adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md`
- `docs/adr/accepted/ADR-0008-device-identity-vs-user-identity.md`
- `docs/modules/customer-display.md`
- `docs/modules/kds.md`
- `docs/architecture/device-strategy.md`
- `docs/deployment/windows-terminal.md`

## Files Likely To Change

```
src/DaxaPos.PosMaui/
src/DaxaPos.AdminPwa/
src/DaxaPos.KdsPwa/
```

## Architecture Assumptions

- MAUI app uses two windows (staff POS + customer display) from a single process.
- Customer display is not stretched from one window — it is a separate `Window` instance.
- MAUI communicates with the API; it does not have its own database (except local cache).
- PWA admin and KDS use the same API as the MAUI app.

## Domain Assumptions

- POS sales screen reflects current order state in real time on customer display.
- Customer display transitions through defined states (idle, ordering, payment, receipt).
- Staff PIN login is required before the sales screen is accessible.

## Risks

- MAUI second-window support for customer display must be tested on real hardware.
- PWA admin portal scope can expand rapidly — keep it scoped.
- KDS real-time updates require SignalR/WebSockets from the start.

## Implementation / Documentation Steps

1. Scaffold `DaxaPos.PosMaui` MAUI project.
2. Implement device registration flow in MAUI.
3. Implement staff PIN login screen.
4. Implement sales screen (product tiles, order basket, modifiers, totals).
5. Implement two-window setup (staff window + customer display window).
6. Implement customer display states.
7. Implement payment flow in MAUI (cash, manual EFTPOS, integrated).
8. Scaffold `DaxaPos.AdminPwa` (product management, venue settings, basic reporting).
9. Scaffold `DaxaPos.KdsPwa` (order display, status updates, reconnect/rebuild state).
10. Update docs.

## Tests To Run Later

- MAUI device registration.
- Staff PIN login.
- Order creation and display on customer screen.
- Payment flow end-to-end.
- KDS reconnect and state rebuild.

## Documentation To Update

- `docs/architecture/device-strategy.md`
- `docs/modules/customer-display.md`
- `docs/modules/kds.md`
- `docs/deployment/windows-terminal.md`

## ADRs Required

- ADR-0004, ADR-0008 (already proposed).

## Open Issues Required

- OI-0003 (Windows POS hardware reference device — may need to create a new issue for POS hardware target).
- OI-0009 (MAUI app update delivery).

## Commit Sequence

```
feat(maui): scaffold Daxa Terminal MAUI app
feat(maui): add device registration and staff PIN login
feat(maui): add POS sales screen
feat(maui): add customer display second window
feat(pwa): scaffold Daxa Back Office PWA
feat(pwa): scaffold Daxa KDS PWA
docs: update device strategy, customer display, and KDS docs
```

## Handoff Notes

Depends on PLAN-0005 (Payments/Receipts). MAUI development requires a Windows machine with Visual Studio 2022 or Visual Studio 2022 Preview. Customer display hardware (secondary monitor) is required for full testing of the two-window setup. Next: PLAN-0007 (Sync/Local/Hybrid).

**Localisation note (added 2026-07-02, planning-only):** UI localisation for the POS/customer-display/KDS/admin surfaces this plan scaffolds is planned but deferred — see [ADR-0016 — Multi-Language and Localisation Strategy](../../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) (proposed). This plan should avoid hard-coding UI strings in a way that would block adopting standard .NET/framework localisation later (`.resx`/`IStringLocalizer` for MAUI, an equivalent for whichever PWA framework is chosen), but does not need to implement localisation itself.
