# PLAN-0006 - Terminal, Display, and PWA

## Status

Milestone A - Complete. Milestone B - Complete. Milestone C - Complete. Milestone C.1 (TerminalId
resolution, modifiers, real order wiring) - In progress, see kickoff decision below.

See `docs/plans/active/PLAN-0006-worker-notes.md` for the full Milestone A implementation report.
Summary: `src/DaxaPos.Web` (standalone Blazor WebAssembly PWA) scaffolded with device-setup, staff
PIN login, session-backed shell, and route guarding; `tests/DaxaPos.Web.Tests` added (bUnit +
xunit, 31 tests); one small, documented backend change (CORS) to let the new browser-origin client
call the API. No migrations. Milestone B (Back Office skeleton, device PIN generation) is next.

### Milestone B kickoff decision (2026-07-07)

Every endpoint Back Office needs to consume (`device-registration-pins`, `devices`, `locations`,
`product-categories`, `products`, `menus`) is gated `RequirePermission(..., rejectStaffPin: true)`
on the backend. Milestone A's `SessionState`/`AuthSessionStore`/`MainLayout` model only ever holds a
Staff PIN session, which can never carry those permissions (ADR-0013's Staff ID/PIN Login Rules
explicitly forbid it for catalogue/tax/user/security-config actions). Back Office therefore needs
its own login, backed by the already-existing `POST /api/v1/auth/local/login` (username/password,
no device context required — ADR-0013's "Local admin portal login" row). This is not a new product
decision so much as a consequence of decisions already recorded in ADR-0013/ADR-0015 (Staff PIN and
admin/back-office sessions "are never interchangeable, never share a token format" — ADR-0015 §2).

Concretely: a new `BackOfficeSessionState`/`IBackOfficeSessionStore` (own localStorage key, mirrors
the existing `SessionState`/`AuthSessionStore` shape) and a new `BackOfficeLayout` with its own route
guard, kept structurally separate from `MainLayout`/`SessionState` so Milestone A's tested Terminal
shell is untouched. Back Office API calls attach their bearer token explicitly (new
`DaxaApiClient` overloads); `AuthHeaderHandler` gets one small, documented change — skip its implicit
Bearer/Device resolution if the request already carries an explicit `Authorization` header — so the
two sessions never collide on the shared `HttpClient`. See worker notes for the full file list.

Known gap, not fixed in Milestone B: there is no `GET`/list endpoint for `DeviceRegistrationPin` —
only create (returns the raw PIN once) and revoke. "Viewing" a PIN in Back Office therefore means
the one just generated in the current browser session, not a durable list of previously issued
PINs. Adding a list endpoint would mean exposing PIN metadata via a new GET, which is a backend
contract change outside this milestone's "reuse existing endpoints, don't change schema" rule — left
as a documented follow-up (see worker notes) rather than expanded into scope silently.

### Milestone B closeout (2026-07-07)

Implemented as planned above, with no deviations from the kickoff decision. See worker notes for
the full file list, verification results, and follow-up items. Summary: Back Office skeleton added
to the existing `src/DaxaPos.Web` project under `/back-office/**`, with its own local-login
session (`BackOfficeSessionState`/`BackOfficeSessionStore`) and `BackOfficeLayout`, structurally
separate from the Terminal shell. Device-registration-PIN generate/revoke, and read-only
devices/locations/products/menus views, all against pre-existing backend endpoints — no backend
code, schema, or migrations changed. 22 new Web tests added (53/53 passing); full solution suite
1105/1105 passing (144 unit + 53 Web + 908 API), no regressions against Milestone A's 1083
baseline. Backend wire formats were additionally verified live via `curl` against the already-running
local demo stack (`docker ps` showed `pos-platform-api-1` already up) — every endpoint/field
this milestone's client DTOs assume was exercised and matched exactly. The Blazor UI itself was
exercised via bUnit component tests (real rendering/click handling) but not in an actual browser,
since no browser-automation tool was available in this session; the running `pos-platform-web-1`
container was deliberately left untouched/un-rebuilt rather than mutating the user's existing demo
environment. Milestone C (POS Terminal PWA sales screen) is next.

### Milestone C kickoff decision (2026-07-07)

**Menu browsing is a straightforward reuse of `GET /api/v1/menus/resolved?locationId=`**
(`ResolvedMenuEndpoints`) — `RequireAuthorization()` only, no permission gate, no
`rejectStaffPin` check, so the existing Milestone A staff-PIN session works unmodified. This is
the "sales-screen-ready projection" the endpoint's own doc comment says it exists for.

**Real order submission is blocked by a genuine, pre-existing backend gap, not a Milestone C
choice**: `POST /api/v1/orders` requires a `TerminalId`, but:

- `GET /api/v1/terminals` (the only way to list `Terminal` rows) is gated
  `RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true)` — a staff-PIN session can
  never call it.
- No session ever carries a `TerminalId`: `DeviceTokenAuthenticationHandler` hard-codes
  `TerminalId: null`, and `SessionAuthenticationHandler` reads `AuthSession.TerminalId`, which no
  endpoint (staff-PIN login included) ever sets — confirmed by reading both handlers and
  `AuthEndpoints.StaffPinLoginAsync`'s `AuthSession` construction.
- `Terminal.DeviceId` (the column that would let a registered device resolve "its" terminal) is
  never written by any endpoint either — `CreateTerminalRequest`/`UpdateTerminalRequest` only
  carry `Name`/`LocationId`.

So there is currently no way for a Terminal PWA staff session to discover or resolve a valid
`TerminalId` at all. Per the task's own explicit escape hatch ("keep this as a local in-memory
draft unless an existing order-submit endpoint is already intended for this milestone") and the
hard rule against unplanned backend changes, **Milestone C implements the sales screen as a local,
in-memory order draft only** — menu browsing against the real resolved-menu endpoint, but
selection/quantity/notes/cart state lives entirely in the page's Blazor state and is never POSTed
to `/api/v1/orders`. This is flagged here rather than silently either (a) inventing a
staff-accessible terminal-listing endpoint, or (b) wiring one specific hard-coded `TerminalId`,
either of which would be an undiscussed backend/product decision.

Follow-up (not fixed here): a future milestone or open issue should resolve one of: a
staff-accessible `GET /api/v1/terminals?locationId=` (scoped to the caller's own location, mirroring
`ResolvedMenuEndpoints`' own location-scoping check), or populating `Terminal.DeviceId` at device
registration so a device can resolve "its" terminal, or a session-level default-terminal
resolution. Only once one of those exists can Milestone D's payment flow (and a real Milestone C
order-submit) be wired to the real order API.

**Modifiers are not shown**: `ResolvedMenuItemResponse` (the only staff-accessible menu DTO) does
not carry modifier data at all — no modifier group/option list is embedded in the resolved-menu
projection, and every modifier-catalog endpoint (`ModifierEndpoints`, `ModifierGroupEndpoints`,
`ProductModifierGroupEndpoints`) is `rejectStaffPin: true`. Per the task's own conditional
("if menu item modifier data already exists in the consumed DTOs, show enough UI... do not build a
full modifier-management system") — it does not exist in the consumed DTO, so no modifier UI is
built. Flagged as a gap: a real modifier-selection UI needs either modifiers embedded in
resolved-menu, or a staff-accessible modifier-lookup endpoint, whichever a future ADR/plan chooses.

Routes/components planned: `/sales` (`Pages/Sales.razor`) added to the existing Terminal shell
(`MainLayout`/`NavMenu`), gated by the existing device+session route guard — no new layout, no
new session type. A `Menu`/`Cart` split within one page (matching Milestone A/B's one-file-per-page
convention), using Bootstrap's grid for a responsive two-column-desktop/stacked-mobile layout (no
new CSS framework). `Home.razor` gets a link to `/sales`; `NavMenu.razor` gets a "Sales" link.

Endpoints to be consumed: `GET /api/v1/menus/resolved?locationId=` (new client DTOs:
`ResolvedMenuResult`/`ResolvedMenuSectionResult`/`ResolvedMenuItemResult`, added via the existing
implicit-auth `DaxaApiClient.GetAsync` pattern — no explicit-bearer plumbing needed, since this is
a Terminal-shell staff-session call, not a Back Office admin call).

Explicitly out of scope for Milestone C (per hard rules and the gap above): real order
create/add-line/hold/resume/void/cancel against `POST /api/v1/orders` and friends; payments;
customer display; KDS; printing; any Back Office expansion; any backend schema/endpoint change.

### Milestone C closeout (2026-07-07)

Implemented as planned above, no deviations. See worker notes for the full file list and
verification detail. Summary: `/sales` added to the existing Terminal shell in `src/DaxaPos.Web`
— a resolved-menu-backed tile grid with a local, in-memory order draft panel (add/increment/
decrement/remove, per-line notes, clear order). No backend code, schema, or migrations changed.
10 new Web tests added (63/63 passing); full solution suite 1115/1115 passing (144 unit + 63 Web +
908 API), no regressions against Milestone B's 1105 baseline.

The `GET /api/v1/menus/resolved` shape was verified live against the already-running local demo
stack with **real seeded data** (a tax category/definition/mapping, a venue tax configuration, a
product, and a menu/section/item created via `curl`) — going further than Milestone B's
empty-array-only verification. The exact same call was then re-run under a genuine staff-PIN
session (a freshly created zero-role/zero-permission staff member, matching realistic Milestone C
usage) and returned the identical shape, confirming both the DTO assumptions and that
`ResolvedMenuEndpoints` needs no permission grant to work from the Terminal shell. That same
staff-PIN test independently reconfirmed the `TerminalId`/`orders.manage` gap noted in the kickoff
decision: the staff member had no permissions at all by default, so even if a `TerminalId` were
resolvable, `POST /api/v1/orders` would still 403 for a freshly-provisioned staff member — further
evidence the local-draft decision was the pragmatic, correct scope for this milestone.

No browser-automation tool was available this session (same constraint as Milestone B); Blazor
component behaviour (render, tile tap, quantity +/-, remove, notes, clear order, error/empty
states) was verified via bUnit, and the backend contract was verified live via `curl` as above. The
running `pos-platform-web-1` container was left untouched/un-rebuilt.

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

### Milestone C.1 kickoff decision (2026-07-07)

Human decision (owner, 2026-07-07): TerminalId must be genuinely resolvable wherever an order
workflow needs it (no fake/null IDs used as a workaround); modifiers are mandatory on the sales
screen (required groups block completion, optional groups are offered); the sales draft must
survive a page refresh using the safest appropriate persistence; backend contract changes are
authorised where justified. This inserts **Milestone C.1** between Milestone C and Milestone D to
close the three gaps Milestone C's closeout flagged, before Milestone D (payments) can start.

**TerminalId resolution — completes an already-designed, half-built mechanism, not a new one.**
`Terminal.DeviceId` (nullable, FK `SetNull`) already exists as a real migrated column
(`TerminalConfiguration.cs`) — ADR-0008 already describes device↔terminal linkage as part of the
device-identity model; nothing today ever writes it or reads it back into a session. The fix is
additive, no migration required:

- `TerminalEndpoints.UpdateTerminalRequest` gains `Guid? DeviceId` (assign/unassign a device to a
  terminal). Validates the device belongs to the terminal's own location and tenant (404
  otherwise, matching this file's existing context-provenance pattern); rejects (409) assigning a
  device that is already linked to a *different* active terminal — admin must unassign first
  rather than the API silently stealing the link. This stays `TerminalsManage` +
  `rejectStaffPin: true`, unchanged from today.
- `AuthEndpoints.StaffPinLoginAsync` looks up `Terminal` by `DeviceId == deviceContext.DeviceId`
  (active only) and sets the new `AuthSession.TerminalId` from it — `AuthSession.TerminalId` and
  `SessionAuthenticationHandler`'s read of it already exist and are already unused/always-null
  today; this is the missing write. No response DTO changes (`StaffPinLoginResponse` is untouched);
  the value surfaces to the client exclusively through the already-existing, already-client-wired
  `GET /api/v1/auth/me` (`AuthContextResponse.TerminalId` / client `AuthContextResult.TerminalId`,
  both already implemented in Milestone A, `GetMeAsync` just never had a caller).
- Back Office needs a minimal way to create this link at all, since every `TerminalEndpoints` route
  is admin-only: a new `Pages/BackOffice/Terminals.razor` (list terminals, create terminal,
  assign/unassign device from the location's device list). This is the smallest Back Office surface
  that makes TerminalId resolvable end-to-end; without it there is no way for any device to ever
  get a non-null TerminalId.
- Web: `SessionState` gains `Guid? TerminalId`. `Login.razor` calls the already-implemented
  `GetMeAsync()` right after a successful staff-pin login and persists its `TerminalId` (nullable —
  a device genuinely not yet linked to a terminal shows a clear "not linked" state on `/sales`,
  never a fabricated ID). If `/auth/me` itself fails post-login, `TerminalId` stays null and the
  same "not linked" UX applies — not a login failure, since the server independently re-checks
  TerminalId at order-open time regardless of what the client believes.

**Modifiers — the backend catalog model (`ModifierGroup`/`Modifier`/`ProductModifierGroup`,
`SelectionMin`/`SelectionMax`/`IsRequired`) already exists in full from PLAN-0004**; the gap is
purely presentation and enforcement:

- `ResolvedMenuEndpoints`'s `ResolvedMenuItemResponse` gains `ModifierGroups`
  (`ResolvedModifierGroupResponse { Id, Name, SelectionMin, SelectionMax, IsRequired, DisplayOrder,
  Modifiers: ResolvedModifierResponse[] { Id, Name, PriceDelta } }`), batch-loaded per resolved
  menu call (no N+1), active groups/modifiers only. Still `.RequireAuthorization()` only — no
  permission-gate change, matching the endpoint's existing staff-accessible-by-design posture.
  This is the "sales-screen-ready projection" the endpoint's own doc comment already promises.
- `OrderEndpoints.AddLineAsync` gains real server-side enforcement of `IsRequired`/
  `SelectionMin`/`SelectionMax` per linked `ModifierGroup` (today it only checks that a submitted
  `ModifierId` is active and linked to the product — group cardinality is never checked). This is
  in-scope per the owner's modifier mandate and is a natural extension of validation code that
  already exists in this exact method, not a new subsystem. Client-side blocking (the modifier
  modal) is the primary UX; this is the server-side backstop, consistent with this codebase's
  "server remains authoritative" rule already applied to tax/pricing/permissions in this same file.

**Draft persistence — the safest mechanism is the already-existing real `Order`/`OrderLine` API,
not a richer local cache.** Milestone C's local-only draft could never show real
server-computed totals (it labelled its total "(estimate)"), which conflicts with "UI must not
recalculate tax/pricing." Now that TerminalId and modifiers are both resolvable, and
`orders.manage` is already staff-PIN-eligible with no `rejectStaffPin` (confirmed in Milestone C's
own verification), the sales screen switches to real orders:

- First tile tap opens a real `Order` (`POST /api/v1/orders`) and stores only its `OrderId` client
  side, in a new device-scoped localStorage key (`daxa.sales-draft.v1.{deviceId}` — naturally
  collision-free across terminals sharing a browser profile, satisfying the "scope by
  tenant/location/device/staff/session" requirement without needing a compound key). On `/sales`
  load, an existing `OrderId` is resolved via `GET /api/v1/orders/{id}`; a 404 or a
  non-Open/Held status clears the stored pointer and the screen starts empty. This means refresh
  survives via the server being authoritative — exactly CLAUDE.md's "Order state must be
  reconstructable from the server/database" rule, not a parallel client-side source of truth.
- `OrderLine` has no update endpoint by design (append/void only, per this codebase's
  no-silent-edit rule) and `AddOrderLineRequest.Quantity` already exists per line. Rather than
  void+recreate on every `+`/`-`, each unit tap adds/voids a **separate `Quantity: 1` line** for
  the same `(ProductId, ModifierIds, Notes)` combination; the sales screen groups same-combination
  active lines for display (one row, summed quantity) while every unit stays individually
  voidable. `+` adds another line; `-` voids the group's most-recently-added active line; "Remove"
  voids every line in the group. Notes are fixed at first-add time for a combination (changing
  notes starts a new group/row) — documented simplification, not silently mutated.
- "Clear order" now calls `POST /api/v1/orders/{id}/void` (reason: "Cleared by staff") instead of
  only clearing local state, so no order is left open/orphaned server-side, and clears the local
  pointer.
- No new order endpoints are added — `POST orders`, `GET orders/{id}`, `POST orders/{id}/lines`,
  `DELETE orders/{id}/lines/{lineId}`, `POST orders/{id}/void` are all pre-existing and already
  staff-PIN-eligible.

**Explicitly out of scope for Milestone C.1**: Hold/Resume UI (endpoints already exist but are not
wired here — flagged as a natural, separate follow-up now that real orders exist), payments,
customer display, KDS, printing, split bills, quantity direct-entry, item search, any Back Office
expansion beyond the one Terminals page needed to make TerminalId resolvable.

See worker notes for the full file list, DTO shapes, and test list.

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

### Milestone C.1 - TerminalId Resolution, Modifiers, And Real Order Wiring

Scope:

- Resolve TerminalId end-to-end (Terminal-Device assignment in Back Office, session resolution at
  staff-PIN login).
- Surface modifier groups/options on the resolved-menu projection; enforce required/min/max
  server-side on add-line.
- Replace the local-only order draft with real `Order`/`OrderLine` calls; persist only the
  `OrderId` client-side so a refresh rebuilds from the server.

See the "Milestone C.1 kickoff decision" section above for full detail. Rules: no fake/placeholder
TerminalId; required modifier groups block order-line completion in the UI; the server enforces
modifier group cardinality independently of the UI; no new order endpoints are added.

Explicitly out of scope:

- Hold/Resume UI.
- Payments, customer display, KDS, printing.
- Split bills, quantity direct-entry, item search.
- Back Office expansion beyond the Terminals page.

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
