# PLAN-0006 — Terminal, Display, and PWA

## Status

**Draft — planning pass only (2026-07-06). No implementation started.** This rewrite turns the
original architecture-level draft into a milestone-by-milestone plan, following the same process
PLAN-0004's and PLAN-0005's own planning passes used. No `src/` changes, no migrations, no
endpoints, no UI code were written in this session.

## Goal

Implement the staff-facing UI layer that consumes PLAN-0005's completed order/payment/refund/
receipt/printing foundations: `DaxaPos.PosMaui` (Daxa Terminal, the Windows POS sales screen and
staff PIN login), the Daxa Display second window (customer-facing order/payment state), and the
PWA foundations for Daxa Back Office (admin portal) and Daxa KDS (kitchen display). This is the
first plan in the roadmap that builds a user-facing application rather than backend/API surface.

## Scope

- `DaxaPos.PosMaui` — .NET MAUI Windows app: device registration screen, staff PIN login screen,
  POS sales screen (product tiles from the resolved-menu endpoint, basket/order-line management,
  modifiers, hold/resume), payment screen (cash and manual EFTPOS only — see Non-goals), receipt
  view/print-trigger and reprint action.
- Daxa Display — second MAUI `Window` in the same process, reflecting live order/payment state.
- Daxa Back Office PWA — skeleton admin portal: device-registration-PIN generation/viewing (a hard
  dependency of the MAUI device registration screen, per ADR-0008), read views over PLAN-0004's
  catalog/venue/tax configuration, basic order/sales reporting.
- Daxa KDS PWA — minimal read-only "open orders" board (see Milestone F; full kitchen-ticket/
  station-routing lifecycle is explicitly out of scope, not just deferred UI work — the backend
  data model for it does not exist).
- RBAC/staff-PIN UX handling across all of the above (client-side reflection of server-side gates
  that already exist — see "RBAC/Staff PIN Expectations" below).

## Non-goals

- Integrated (Stripe Terminal) payment UI. PLAN-0009 (Stripe Terminal adapter) is still **Draft**,
  not implemented — `PaymentMethod.Integrated` is rejected by the API today (see
  `docs/modules/payments.md`). This plan's payment screen supports cash and manual EFTPOS only; see
  Human Decisions Needed #3 for whether to stub a disabled "card" button now.
- Kitchen-ticket/station-routing backend (entities, endpoints, printer routing). Tracked by
  OI-0018, which explicitly reserves "Admin UI for configuring printer routes" for "PLAN-0006 back
  office work, or a dedicated follow-up plan, once this OI's data model exists for it to
  configure" — that data model does not exist yet, so there is nothing for a Back Office or KDS
  screen to configure or display beyond a raw order list. See Milestone F.
- Hardware/provider/device orchestration, printer discovery, USB transport, daemons, installer/
  update tooling. All of that is PLAN-0009 or a later hardware-integration plan's scope, per
  approved Human Decision #1 in PLAN-0005's own planning record (a scope boundary this plan
  inherits, not re-derives).
- Full floor plan / table management, split bills, self-ordering kiosk, advanced KDS. Explicitly
  Phase 2/3 per CLAUDE.md's Phase Roadmap.
- A full admin CRUD UI sweep over every PLAN-0004 catalog/tax/pricing endpoint. Milestone E is
  read-mostly plus the specific write flows this plan's own UI depends on (see Human Decisions
  Needed #5).
- Fixing OI-0017 (product archive-and-replace race) or OI-0018 (printer routing) itself. Both stay
  open; neither is a blocker for this plan's UI work (see "Open Issues Re-Check" below).
- Localisation implementation. ADR-0016 is accepted but deferred; this plan must not hard-code UI
  strings in a way that blocks adopting `.resx`/`IStringLocalizer`-equivalent localisation later,
  per the existing Localisation note retained at the bottom of this document.

## Context Read

This planning pass read:

- `CLAUDE.md` (planning-before-work rules, device strategy, RBAC/staff-PIN rules).
- `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md` and
  `PLAN-0005-worker-notes.md` (all six milestone reports — the source of the Order/Payment/Refund/
  Receipt/Printing APIs this plan's UI consumes).
- `docs/modules/orders.md`, `payments.md`, `refunds.md`, `receipts.md`, `printing.md`.
- `docs/issues/open/OI-0017-product-archive-and-replace-concurrency.md`,
  `OI-0018-location-scoped-production-printer-routing.md`.
- `docs/adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md` (including its 2026-06-30
  addendum — device registration flow, MAUI update delivery, Windows hardware baseline).
- `docs/adr/accepted/ADR-0008-device-identity-vs-user-identity.md` (full accepted registration flow
  — this is the spec Milestone A's device registration screen must implement against).
- `docs/modules/customer-display.md`, `docs/modules/kds.md`.
- `docs/architecture/device-strategy.md`, `docs/deployment/windows-terminal.md`.
- `docs/plans/active/PLAN-0009-first-payment-adapter-stripe-terminal.md` (confirmed **Draft**,
  unimplemented — this is why integrated payment UI is out of scope here).
- Current source: confirmed `DeviceRegistrationPinEndpoints.cs`, `DeviceRegistrationEndpoints.cs`,
  and `AuthEndpoints.cs`'s `/staff-pin/login` all already exist and are mapped in `Program.cs` (from
  PLAN-0003) — Milestone A has no backend dependency gap for registration/login, only for
  PIN-generation UI (see Milestone E).

## Files Likely To Change

```
src/DaxaPos.PosMaui/                 (new — Windows MAUI Daxa Terminal + Daxa Display second window)
src/DaxaPos.AdminPwa/                (new — Daxa Back Office PWA; framework TBD, see Human Decisions Needed #1)
src/DaxaPos.KdsPwa/                  (new — Daxa KDS PWA skeleton; same framework as AdminPwa)
docs/modules/customer-display.md, kds.md
docs/architecture/device-strategy.md, windows-terminal.md
docs/CHANGELOG.md (only once real milestone implementation begins, matching PLAN-0004/0005's own convention of not logging pure planning passes)
```

No `src/DaxaPos.Api`, `Application`, `Domain`, `Infrastructure`, or `Persistence` changes are
expected for Milestones A–E (they consume already-shipped PLAN-0003/0004/0005 endpoints as-is).
Milestone F may need one narrow, explicitly-scoped backend addition (see below) — flagged, not
assumed.

## Architecture Assumptions

- MAUI app uses two `Window` instances (staff POS + customer display) from a single process, per
  ADR-0004 — never a single window stretched across two monitors.
- MAUI has no database of its own beyond a local cache for offline/resilience; all order/payment/
  catalog state is server-authoritative, fetched from the API.
- PWA admin and PWA KDS use the same API as the MAUI app — no separate backend surface per client
  type.
- Realtime updates (order changes, KDS board updates) follow ADR-0014: server state is always
  authoritative, pushed events are convenience notifications only, and any realtime-consuming
  screen must be able to fully rebuild its state from a `GET` after reconnect. This plan does not
  reopen that rule, only builds a UI that must obey it.
- Device identity and user identity remain separate per ADR-0008: the MAUI app registers itself as
  a `Device` once, independent of which staff member is PIN-logged-in at any moment.

## Domain Assumptions

- A device registration PIN is generated/viewed through an admin surface (ADR-0008: "Admin creates
  or views a short-lived 6-digit device registration PIN... configured in the admin portal") — this
  makes a minimal Back Office PIN-management screen a hard dependency of Milestone A's device
  registration screen being end-to-end usable outside of direct API calls (Milestone A itself can
  still be built and manually tested by calling `DeviceRegistrationPinEndpoints` directly before
  Milestone E's UI exists).
- Staff PIN login already structurally prevents a role carrying any `AdminSensitive` permission
  (e.g. `payments.refund`) from completing PIN login at all (PLAN-0005 Milestone C's discovered
  defense-in-depth behaviour). The sales/payment UI therefore never needs to hide a refund action
  from a staff-PIN session for security reasons — that session cannot exist with refund rights in
  the first place. Any such UI hiding this plan does add is a UX nicety (avoiding a confusing 403),
  never a security control.
- The customer display reflects order/payment state pushed from the same MAUI process
  (`CurrentOrderService`), not a second independent API poller — per `docs/modules/customer-
  display.md`.
- Cash and manual EFTPOS payment recording require no terminal-pairing UI (per PLAN-0005 Milestone
  B: neither method calls out to any external system). The payment screen's only "pairing" concern
  in this plan is therefore none — that requirement arrives with PLAN-0009.

## Risks

- **MAUI cannot be built, run, or tested from this development environment.** The current session
  runs on macOS (Darwin); .NET MAUI's Windows target requires a Windows machine with Visual Studio
  2022 (or Visual Studio 2022 Preview), per the existing Handoff Notes below and
  `docs/deployment/windows-terminal.md`. This is a hard constraint on *implementation*, not on this
  planning pass — flagged as Human Decisions Needed #2.
- PWA admin portal scope can expand rapidly if not held to "read-mostly + the specific write flows
  this plan's own UI needs" (Human Decisions Needed #5) — the same risk the original draft already
  flagged.
- KDS real-time updates require SignalR/WebSockets from the start if built at all in this plan —
  mitigated by Milestone F's reduced scope (a polled/refreshed read-only board, not a full realtime
  ticket system), deferring the SignalR investment to whichever future plan builds the real kitchen
  ticket lifecycle.
- No PWA framework has been chosen anywhere in the documented decisions (CLAUDE.md says "web
  application/PWA" without naming one). This blocks starting Milestone E/F's actual scaffolding —
  Human Decisions Needed #1.
- Integrated payment UI's absence (Non-goals) means the payment screen's layout may need rework
  once PLAN-0009 lands a card-payment option — mitigated by Human Decisions Needed #3's stub-button
  recommendation, but not eliminated.

## Open Issues Re-Check

- **OI-0017** (product archive-and-replace race): unaffected by this plan. The POS sales screen
  reads products via the existing resolved-menu endpoint exactly as any other read client would;
  no new concurrent-write path against `Product` is introduced by UI work. Stays open, untouched.
- **OI-0018** (location-scoped production printer routing): unaffected in the sense that this plan
  does not implement the missing routing table or dockets (that stays out of scope per Non-goals).
  It is, however, the reason Milestone F's KDS scope is deliberately minimal — there is no routing
  data model yet for a kitchen screen to filter by station. Stays open, untouched.

## Milestones

### Milestone A — Device registration and staff PIN login (MAUI shell)

**Scope:** Scaffold `DaxaPos.PosMaui`. Implement the device registration screen against the
already-existing `DeviceRegistrationPinEndpoints`/`DeviceRegistrationEndpoints` (PLAN-0003 — no new
backend work). Implement the staff PIN login screen against the already-existing `POST /api/v1/
auth/staff-pin/login`. Store the issued device credential using the OS-appropriate secret store
(Windows Credential Manager) per ADR-0008, not a plaintext config file, for anything beyond a
throwaway dev spike.

**Deliverables:**
- `DaxaPos.PosMaui` project scaffold, buildable on Windows.
- Device registration screen: enter registration PIN, select device type, optional friendly name,
  store issued device identity/credential.
- Staff PIN login screen: location context from the registered device, staff code + PIN entry,
  session establishment, fast staff-switch (logout without app restart).
- No sales screen yet — this milestone ends at a logged-in, empty shell.

**Tests/verification:** Manual verification against a real Windows machine (blocked on Human
Decisions Needed #2) plus, where the MAUI project structure allows it, unit tests for any
non-UI-bound logic (credential storage helper, PIN-entry validation) that can run on any platform.
No `dotnet test DaxaPos.sln` regression is expected since no `src/DaxaPos.Api/Application/Domain/
Infrastructure/Persistence` code changes.

**Docs to update:** `docs/deployment/windows-terminal.md` (first-time setup, once a real screen
exists to describe), `docs/architecture/device-strategy.md` (implementation-status note, matching
the convention every PLAN-0004/0005 module doc used).

**Explicitly out of scope:** Sales screen, payments, customer display, kiosk/assigned-access OS
configuration (that's an operator/OS-level task per `docs/deployment/windows-terminal.md`, not app
code), printer/EFTPOS pairing (PLAN-0009/hardware plan).

### Milestone B — POS sales screen (order entry)

**Scope:** Build the staff-facing sales screen against PLAN-0005's `Order` API and PLAN-0004's
resolved-menu endpoint. Product tiles, category navigation, add/remove line, modifiers, quantity,
notes, hold/resume, void line, void/cancel order.

**Deliverables:**
- Product-tile grid sourced from the resolved-menu endpoint (location-scoped, already
  price/tax-resolved — the UI does not recompute anything the API already returns).
- Basket/order view: running subtotal/tax/total exactly as returned by `Order`'s server-computed
  fields (never client-recomputed).
- Modifier selection UI for products with `ModifierGroup`s.
- Hold/resume, void-line, void/cancel-order actions wired to the existing endpoints.
- `orders.manage`-gated actions reflect a 403 gracefully (a session lacking the permission should
  see a clear message, not a raw error) — client-side reflection only, server remains the actual
  gate.

**Tests/verification:** Same platform constraint as Milestone A. Where feasible, extract any
pure client-side calculation (there should be very little — totals come from the server) into
platform-agnostic unit tests.

**Docs to update:** `docs/modules/orders.md` (UI-consumption note).

**Explicitly out of scope:** Payments (Milestone C), customer display sync (Milestone D), table/
floor plan, split bills (Phase 2 per CLAUDE.md).

### Milestone C — Payment and receipt flow (MAUI)

**Scope:** Payment screen for cash and manual EFTPOS only (Non-goals: no integrated/Stripe
Terminal UI — PLAN-0009 is still Draft). Receipt view after order completion, reprint action.

**Deliverables:**
- Cash payment entry (amount tendered / change calculation is a **client-side convenience only**;
  the amount recorded against the order is always the order's `GrandTotalAmount`, per PLAN-0005
  Milestone B's settlement rule — the UI must not invent a different recorded amount).
- Manual EFTPOS payment entry (staff confirms an amount was taken on an external terminal; the app
  never talks to that terminal, per PLAN-0005's existing behaviour).
- Split-payment UI (multiple payment calls against one order, matching the API's existing support).
- Receipt view rendering the API's `ReceiptDocument` (`GET /api/v1/orders/{id}/receipt`), including
  the GST-free marker and tax summary exactly as the API returns them — no client-side tax
  recomputation.
- Reprint action wired to `POST /api/v1/orders/{id}/receipt/reprint` (`receipts.reprint`,
  staff-PIN-eligible).
- A visibly disabled "Card" payment option is recommended (Human Decisions Needed #3) so the
  screen's layout doesn't need reflowing once PLAN-0009 lands, but this is not decided here.

**Tests/verification:** Same platform constraint as Milestones A/B.

**Docs to update:** `docs/modules/payments.md`, `receipts.md` (UI-consumption notes).

**Explicitly out of scope:** Any integrated payment terminal pairing/adapter call, refund UI (see
Milestone G's RBAC note — a refund screen may be added here or deferred; not decided, see Human
Decisions Needed #4), cash drawer UI (the drawer kick is already fully automatic server/printer-
side per PLAN-0005 Milestone E — no app-side trigger is needed).

### Milestone D — Daxa Display (customer-facing second window)

**Scope:** Second MAUI `Window` reflecting the states documented in `docs/modules/customer-
display.md` (Idle, OrderBuilding, PaymentStarted, PaymentApproved, PaymentDeclined, Receipt,
Loyalty/Tip placeholders for later markets).

**Deliverables:**
- Second-window launch on secondary-monitor detection (no window shown if none is attached, per
  `docs/deployment/windows-terminal.md`).
- Shared in-process `CurrentOrderService` (or equivalent) driving both windows from one order
  state — not a second independent API client.
- All documented display states implemented except Loyalty/Tip (later markets, not MVP).

**Tests/verification:** Requires a real secondary monitor on a real Windows machine to fully
verify per the original draft's own Risks section — same platform constraint as Milestones A–C,
compounded by needing actual dual-monitor hardware.

**Docs to update:** `docs/modules/customer-display.md` (implementation-status note).

**Explicitly out of scope:** Loyalty/tip prompts (later markets), any customer-facing input
(display is output-only per its module doc).

### Milestone E — Daxa Back Office PWA (skeleton)

**Scope:** Read-mostly admin portal plus the specific write flows this plan's own UI depends on.
Cannot start until Human Decisions Needed #1 (PWA framework) is resolved.

**Deliverables:**
- Device-registration-PIN generation/viewing screen (hard dependency of Milestone A, per ADR-0008).
- Read views: catalog (products/categories/modifiers), venue/tax configuration, resolved menu
  preview, basic order/sales listing (reusing existing `GET` endpoints from PLAN-0004/0005 — no new
  backend endpoints expected).
- Basic reporting view (daily sales, payment-method breakdown) — scoped to whatever the existing
  API already exposes; if no reporting endpoint exists yet, this becomes a documented gap/open
  issue rather than new backend work invented mid-UI-milestone.

**Tests/verification:** Standard web-app testing for whichever framework is chosen (component
tests, e2e smoke test against a running API) — concrete tooling depends on Human Decisions Needed
#1's answer, not fixed here.

**Docs to update:** `docs/architecture/device-strategy.md` (Back Office section), a new or updated
`docs/deployment/` note for however the PWA is hosted/served.

**Explicitly out of scope:** Full CRUD editing UI for every PLAN-0004 catalog/tax/pricing endpoint
(Human Decisions Needed #5), printer-route configuration UI (OI-0018 — no data model to configure
yet), staff/role management UI beyond what device-PIN generation needs.

### Milestone F — Daxa KDS PWA (minimal skeleton)

**Scope:** Deliberately reduced. `docs/modules/kds.md` marks full KDS as Phase 2/non-MVP; this
milestone builds only a read-only "open orders" board, not a kitchen-ticket lifecycle system.

**Deliverables:**
- A board listing currently open (`Order.Status == Open/Held`) orders and their active lines,
  reusing the existing `GET /api/v1/orders` (list) and per-order line data — no new backend
  entities, no station/route filtering (that data model doesn't exist — OI-0018).
- Manual refresh or simple polling is acceptable for this milestone; full SignalR/WebSocket
  reconnect-and-rebuild (per `docs/modules/kds.md`'s stated requirement) is deferred to whichever
  future plan builds the real kitchen-ticket system, since there is no ticket *state* yet to make
  realtime or resilient.

**Tests/verification:** Same as Milestone E's chosen framework tooling.

**Docs to update:** `docs/modules/kds.md` (implementation-status note explicitly marking this as
the minimal MVP board, not the Phase 2 system the rest of that doc describes).

**Explicitly out of scope:** Marking items ready/complete, station/route filtering, realtime
push updates, reconnect-and-rebuild guarantees, any backend "kitchen ticket" entity. If any of
these turn out to be required before Milestone F can ship something useful, that is itself a
finding this milestone should surface as a new open issue rather than silently expanding scope to
build the missing backend model.

### Milestone G — Consolidation and documentation closeout

**Scope:** Mirrors PLAN-0004 Milestone H / PLAN-0005 Milestone F's own consolidation shape.
RBAC/staff-PIN UX sweep across every screen built in Milestones A–F (confirm each screen degrades
gracefully on a 403 rather than crashing), documentation closeout, and explicit handoff notes for
PLAN-0007 (Sync/Local/Hybrid) and PLAN-0009 (Stripe Terminal, once it lands, will need this
milestone's payment-screen stub point).

**Deliverables:** Updated implementation-status sections across every module/architecture doc
touched by Milestones A–F; a short "known gaps" list (KDS ticket lifecycle, printer routing UI,
integrated payment UI) carried forward explicitly rather than left implicit.

**Explicitly out of scope:** Any new feature work — this is a test/UX-sweep/documentation
milestone only, matching the established PLAN-0004/PLAN-0005 precedent for a plan's final
milestone.

## RBAC/Staff PIN Expectations For UI-Facing Flows

- The server remains the sole authority. Every permission gate already exists (`orders.manage`,
  `payments.record`, `payments.refund`, `receipts.reprint`) — this plan's UI work is presentation
  only: reflect a 403 as a clear message, don't attempt a client-side permission model that could
  drift from the server's.
- `payments.refund` (`AdminSensitive`) cannot be reached by a staff-PIN session at all — PIN login
  itself is rejected for any role carrying an `AdminSensitive` permission (PLAN-0005 Milestone C's
  discovered behaviour). A refund screen, if built in this plan, should therefore assume it is only
  ever reachable from a full User (manager/admin) login context, not a PIN session — whether a
  refund screen belongs in Milestone C or is deferred is Human Decisions Needed #4.
- `orders.manage`/`payments.record`/`receipts.reprint` are all `Operational` and staff-PIN-eligible
  — the sales, payment, and reprint screens in Milestones B/C are expected to work under a normal
  staff-PIN session without any elevated login.

## Offline/PWA Considerations

- MAUI: "local cache for offline/resilience" is named in `docs/architecture/device-strategy.md`
  but its concrete shape (what's cached, how conflicts are handled, how long a device can operate
  disconnected) is Sync/Hybrid territory — PLAN-0007's scope, not this plan's. This plan's
  Milestones A–D assume a reachable API and do not need to design offline order queuing themselves.
- Back Office/KDS PWAs: standard PWA installability (manifest, service worker for asset caching) is
  a reasonable Milestone E/F deliverable once the framework is chosen, but *data* offline-resilience
  (queued writes, conflict resolution) is out of scope for the same reason — PLAN-0007 owns it.
- KDS's documented "must rebuild full state from the server after reconnect" requirement (ADR-0014)
  applies in full once a real ticket-lifecycle system exists; Milestone F's minimal read-only board
  satisfies this trivially (a fresh `GET` on every refresh has no missed-event problem to solve).

## Display/KDS/Terminal Boundaries

- Daxa Display (Milestone D) is not the KDS — it is the customer-facing second window in the same
  process as Daxa Terminal, output-only, no staff interaction.
- Daxa KDS (Milestone F) is a separate device/session, PWA, staff-interactive (in its full future
  form — ready/complete marking), not built in this plan beyond the read-only board described
  above.
- Neither surface performs any printer routing itself; both are pure reads over already-existing
  order data.

## Printer-Routing and Hardware Boundaries

- No printer discovery, USB transport, printer-routing configuration, or hardware/provider adapter
  work is in this plan's scope, per approved Human Decision #1 in PLAN-0005's planning record
  (inherited, not re-derived) and per OI-0018 (the routing data model doesn't exist yet for any
  screen to configure).
- Cash drawer behaviour needs no UI trigger — it is fully automatic, driven by the printer service
  built in PLAN-0005 Milestone E, keyed off whether the order's payments include cash.

## Human Decisions Needed

1. **PWA framework for Daxa Back Office / Daxa KDS.** CLAUDE.md specifies only "web application/
   PWA" without naming a framework. ADR-0004's addendum mentions a separate "local Blazor SSR
   application" for MAUI update management on the Daxa Local server, which does not by itself
   settle this plan's Back Office/KDS framework choice. Recommendation: whichever framework is
   chosen should be the same for both Back Office and KDS (both are read-heavy, both need
   installability), decided before Milestone E starts. No default assumed here.
2. **MAUI implementation environment.** This and any future Claude Code session working this plan
   cannot compile, run, or test a .NET MAUI Windows target from a non-Windows machine. Milestones
   A–D's actual implementation needs either a human-operated Windows machine with Visual Studio
   2022 (Claude Code assisting via that machine or a remote/CI Windows runner), or the milestone
   stays blocked until such an environment is available. Recommendation: confirm the intended
   Windows development environment before Milestone A's implementation session begins — this is a
   process decision, not an architecture one, but it blocks all MAUI work regardless.
3. **Stub a disabled "Card" payment button now, or omit integrated payment UI entirely until
   PLAN-0009 ships?** Recommendation: a visibly disabled placeholder, so Milestone C's screen
   layout survives PLAN-0009 landing without a rework — not decided here.
4. **Does a refund screen belong in this plan (Milestone C) or should it be deferred to a later
   plan/milestone?** `payments.refund` already exists and works server-side; nothing blocks a UI
   for it today. Recommendation: include a minimal refund screen in Milestone C (full/partial
   refund against a payment, gated the same way the API already gates it) since the backend is
   fully ready and deferring it only pushes the same small amount of work into Milestone G with no
   benefit — not decided here.
5. **How much of PLAN-0004's admin surface does Milestone E need to expose as writable UI on its
   first pass?** Recommendation: read-mostly plus device-registration-PIN generation (a hard
   dependency, per ADR-0008) — a full catalog/tax/pricing CRUD UI sweep is deferred to a follow-up
   Back Office milestone or plan, not built here. Not decided here.
6. **Milestone F's exact backend touch, if any.** The plan currently assumes zero new backend work
   for the KDS board (reusing the existing order-list `GET`). If that turns out to be insufficient
   even for a minimal board (e.g. it can't cheaply filter to "open orders across all terminals at
   this location" the way a kitchen screen needs), a narrowly-scoped new read endpoint may be
   needed — flagged as a possibility, not assumed or designed here.

## Tests To Run Later

- Device registration end-to-end (PIN entry → device record created → configuration loaded).
- Staff PIN login and fast staff-switch.
- Order creation, line add/void, hold/resume from the sales screen against a real API.
- Cash and manual EFTPOS payment recording, split payments, order completion.
- Receipt view and reprint.
- Customer display state transitions in sync with the staff window.
- Back Office device-registration-PIN generation, read views.
- KDS board reflects current open orders after a manual refresh.
- RBAC: every screen's 403 path degrades gracefully (client-side proof only — server-side RBAC
  proofs already exist from PLAN-0003/0004/0005 and are not re-tested here).

## Documentation To Update

- `docs/architecture/device-strategy.md`
- `docs/deployment/windows-terminal.md`
- `docs/modules/customer-display.md`
- `docs/modules/kds.md`
- `docs/modules/orders.md`, `payments.md`, `receipts.md` (short UI-consumption notes)

## ADRs Required

- ADR-0004, ADR-0008 (already accepted) — no new ADR required for Milestones A–D.
- Milestone E/F's PWA framework choice (Human Decisions Needed #1) may warrant its own ADR once
  decided, mirroring how ADR-0005/ADR-0006 documented other foundational technology choices — not
  written here, since the choice itself isn't made yet.

## Open Issues Required

- None filed by this planning pass. OI-0017 and OI-0018 are discussed under "Open Issues Re-Check"
  above and remain open, untouched.

## Commit Sequence

Illustrative only — the actual sequence depends on which Human Decisions above are resolved first
and in what order milestones are executed. Not a commitment to this exact order:

```
feat(maui): scaffold Daxa Terminal MAUI app
feat(maui): add device registration and staff PIN login
feat(maui): add POS sales screen
feat(maui): add cash and manual EFTPOS payment flow, receipt view, reprint
feat(maui): add Daxa Display second window
feat(pwa): scaffold Daxa Back Office PWA
feat(pwa): scaffold Daxa KDS PWA (minimal open-orders board)
docs: close PLAN-0006 Milestone G
```

## Handoff Notes

Depends on PLAN-0005 (Payments/Receipts/Printing — complete, 1052/1052 tests, 17 migrations clean)
for every API this plan's UI consumes, and on PLAN-0003 for device registration/staff-PIN-login
endpoints (already implemented, confirmed during this planning pass). Does not depend on PLAN-0009
(Stripe Terminal — still Draft); integrated payment UI is explicitly deferred until that plan
ships. Does not implement PLAN-0009's hardware/adapter/device-orchestration scope, and does not
implement OI-0018's printer-routing data model.

**Before Milestone A's implementation session starts:** resolve Human Decisions Needed #2 (Windows/
MAUI development environment) — this blocks all of Milestones A–D regardless of any other
decision. Human Decisions Needed #1 (PWA framework) similarly blocks Milestones E/F but not A–D, so
those two tracks can proceed independently once each is unblocked.

**Recommended next session:** human reviews and (dis)approves the six Human Decisions Needed items
above. On approval of #2 (or acceptance that MAUI work is blocked until a Windows environment is
available), Milestone A can start with a `docs/plans/active/PLAN-0006-worker-notes.md` session
following the exact TDD-where-applicable / CRUD-endpoint-convention split PLAN-0005 used — though
note Milestone A has almost no pure/testable logic of its own (it's thin UI over two already-
existing endpoints), so the "TDD the one financial-logic unit" pattern from PLAN-0005 may not
apply until Milestone B or C's basket/settlement-display logic, if any of that turns out to be more
than a pass-through of server-computed values.

Next plan after this one completes (or in parallel, per the original draft): PLAN-0007 (Sync/Local/
Hybrid).

**Localisation note (carried forward, planning-only; ADR accepted 2026-07-05):** UI localisation
for the POS/customer-display/KDS/admin surfaces this plan scaffolds is planned but deferred — see
[ADR-0016 — Multi-Language and Localisation Strategy](../../adr/accepted/ADR-0016-multi-language-and-localisation-strategy.md)
(accepted). This plan should avoid hard-coding UI strings in a way that would block adopting
standard .NET/framework localisation later (`.resx`/`IStringLocalizer` for MAUI, an equivalent for
whichever PWA framework Human Decisions Needed #1 selects), but does not need to implement
localisation itself.
