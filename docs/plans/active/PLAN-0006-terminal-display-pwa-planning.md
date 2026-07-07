# PLAN-0006 - Terminal, Display, and PWA

## Status

Milestone A - Complete. Milestone B - Complete. Milestone C - Complete. Milestone C.1 (TerminalId
resolution, modifiers, real order wiring) - Complete. Milestone C.2 (terminal assignment integrity,
terminal-scoped order authorization) - Complete. Milestone D (payments/receipts) - Complete.
**Milestone E (customer display) - Complete, see closeout below.**

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

### Milestone C.1 closeout (2026-07-07)

Implemented as planned above, with one documented deviation: `TerminalEndpoints` gained a
dedicated `POST /{terminalId}/assign-device` action rather than folding `DeviceId` into
`UpdateTerminalRequest` — matching this file's existing one-action-per-endpoint convention
(Deactivate/Reactivate) and avoiding the ambiguity of an omitted `DeviceId` meaning "leave
unchanged" vs. "clear" on a plain rename call. No schema/migration changes anywhere in this
milestone — `Terminal.DeviceId` was already a migrated column with nothing writing or reading it.

Summary: `Terminal.assign-device` (admin-only) plus `AuthEndpoints.StaffPinLoginAsync` resolving
`AuthSession.TerminalId` from it; `ResolvedMenuEndpoints` now includes each product's modifier
groups/options (batch-loaded); `OrderEndpoints.AddLineAsync` enforces
`IsRequired`/`SelectionMin`/`SelectionMax` server-side; `/sales` rewritten to open/add-line/
void-line/void-order against the real API instead of a local-only draft, restoring from a
device-scoped stored `OrderId` on refresh; a new Back Office `/back-office/terminals` page is the
one admin surface that makes device-to-terminal assignment possible at all.

Full solution suite: 1140/1140 passing (144 unit + 78 Web + 918 API), no regressions against
Milestone C's 1115-test baseline (25 new API tests, 15 new Web tests).

**Live verification**: the running local demo stack's `api`/`web` containers were docker images
built before this milestone's changes — rebuilding them was confirmed with the human first (unlike
Milestones B/C, which deliberately left the stale containers untouched since their verification
didn't require new endpoints). After `docker compose build api web && docker compose up -d api
web` (no migration ran — confirmed "No migrations were applied" in the `migrations` container's
own log, matching the "no schema change" claim), the full new flow was exercised live end-to-end
via `curl` against the bootstrap admin and a fresh staff member:

- Created a location, venue tax config, terminal, tax category/definition, a product ("C1 Steak")
  with a required `Doneness` modifier group (`selectionMin: 1, selectionMax: 1, isRequired: true`)
  and one `Modifier` ("Medium Rare"), and a menu/section/item.
- Registered a device via PIN, then called the new
  `POST /api/v1/terminals/{id}/assign-device` — the terminal's `deviceId` updated correctly.
- Created a staff member and staff-PIN-logged in: `GET /api/v1/auth/me` returned the assigned
  terminal's id in `terminalId` (previously always `null` for every staff session — the core gap
  this milestone closes).
- `GET /api/v1/menus/resolved` for that session returned `modifierGroups: []` for a product with
  none (an existing "Flat White" from prior demo data) and the full `Doneness`/`Medium Rare` group
  for the new steak product — confirming the DTO shape and that no permission gate was added.
- Granted the `Staff` role (via direct DB lookup, since there is no `GET /api/v1/roles` endpoint)
  to pick up `orders.manage`, then: `POST /api/v1/orders` with the resolved `TerminalId` succeeded;
  `POST .../lines` **without** the required modifier returned `400` ("'Doneness' requires at least
  one selection."); the same call **with** the modifier succeeded and returned correct
  server-computed totals (`$20.00` inclusive, `$1.82` GST); `DELETE .../lines/{lineId}` and
  `POST .../void` both worked and correctly zeroed the order's totals.

No browser-automation tool was available in this session (same constraint as every prior PLAN-0006
milestone). The rebuilt `pos-platform-web-1` container now serves the new Blazor code (confirmed
`GET /` returns `200`), so the human's own manual browser walkthrough (see worker notes) will
exercise the real thing, not stale code. bUnit component tests exercise real Blazor
rendering/event-handling for the modifier modal, grouped-cart display, and draft-restore paths.

Known gap, not addressed here: line `Notes` free-text entry was dropped from the real-order sales
screen (Milestone C's local draft had a per-line notes input). There is no order-line update
endpoint — `OrderLine` is append/void-only — so supporting notes-on-an-existing-line would need
either a modal on every tap or a void+recreate dance; deferred as a documented simplification
rather than adding either silently. Hold/Resume UI remains unwired (endpoints exist, unused).

### Milestone C.2 kickoff decision (2026-07-07)

**Owner decision**: fix two correctness gaps surfaced by a post-C.1 review before starting
Milestone D, not file them as open issues. Payments/receipts must not be built against order
ownership rules that let the wrong terminal touch another terminal's order — a payment or receipt
bug on top of that would be far more expensive to unwind than fixing the ownership rule now. This
is Milestone C.2, inserted between C.1 and D.

**Gap 1 — `Terminal.DeviceId` assignment had no DB-level uniqueness.** The C.1 review found
`AssignDeviceAsync`'s 409-conflict check (`Terminals.AnyAsync(t => t.DeviceId == deviceId && t.Id
!= terminal.Id)`) was check-then-act with no backing constraint — `TerminalConfiguration.cs` only
had the index EF Core auto-creates for the `DeviceId` foreign key, not a unique one. Two concurrent
`assign-device` calls for the same device could both pass the check before either wrote, leaving
two terminals pointing at the same device — and `StaffPinLoginAsync`'s `.SingleOrDefaultAsync()`
lookup by `DeviceId` would then throw an unhandled exception at login time instead of degrading.

Fix: a Postgres **partial unique index** — `HasIndex(t => t.DeviceId).IsUnique().HasFilter("\"DeviceId\"
IS NOT NULL")` — so unassigned terminals (`DeviceId: null`) never collide with each other, but two
terminals can never share the same non-null `DeviceId`. No data cleanup was needed (checked the dev
DB directly: zero existing duplicates). `AssignDeviceAsync`'s `SaveChangesAsync()` is wrapped to
catch the specific `PostgresException` (SQLSTATE `23505`, constraint name
`IX_terminals_DeviceId`) and return the same 409 the pre-check already returns — the pre-check
stays as a fast, informative path; the constraint is the actual guarantee.
`StaffPinLoginAsync`'s lookup is changed from `.SingleOrDefaultAsync()` to loading a list and
treating more-than-one match as a login failure (`FailAsync(..., "DuplicateTerminalAssignment")`)
— the same generic-401-but-audited-with-specific-reason shape every other failure in that method
already uses — rather than crashing, in case pre-migration or manually-inserted data ever violates
the invariant the new index otherwise enforces.

**Gap 2 — order authorization was location-scoped, not terminal-scoped.** `LoadAuthorizedOrderAsync`
(duplicated verbatim in `OrderEndpoints.cs`, `PaymentEndpoints.cs`, `ReceiptEndpoints.cs`) checked
only `OrganisationId` and, for a location-bound session, `LocationId` — never `TerminalId`. This
predates C.1 (it's how hold/resume/void/add-line were already written for PLAN-0005), but C.1's
draft-restore (`GetOrderAsync` from a cached, device-scoped `OrderId`) was the first caller where
the gap has a real, visible consequence: a device at Terminal A could restore, read, mutate, void,
or (once Milestone D exists) pay/receipt an order that actually belongs to Terminal B, as long as
both terminals share a location and the caller has *an* `OrderId` for it.

Fix, applied identically in all three `LoadAuthorizedOrderAsync` copies:

```csharp
if (authContext.LocationId is not null
    && (authContext.TerminalId is null || authContext.TerminalId != order.TerminalId))
{
    return null;
}
```

This only ever fires for a **location-bound** session (staff-PIN or device-token) — an
organisation-scoped admin/Back-Office session (`LocationId: null`) is unrestricted, same as the
existing location check right above it, preserving "admin/back-office access remains appropriate."
For a location-bound session: `TerminalId: null` (device not yet assigned to a terminal) is
rejected outright — satisfies the owner's requirement that an unlinked device cannot mutate any
terminal order, not just "the wrong one" — and a non-matching `TerminalId` is rejected the same
way. The same rule is added to `OrderEndpoints.OpenAsync` (which doesn't go through
`LoadAuthorizedOrderAsync`, since there's no existing order yet) so a location-bound session can't
open an order for a *different* terminal at its own location by supplying that terminal's id in the
request body — this is exactly ADR-0015's Context Provenance principle ("a client can never widen
or redirect its own tenant/location/terminal scope by changing... a JSON field"), finally
enforceable now that `AuthContext.TerminalId` is ever non-null (before C.1 it was always null, so
this check would have been meaningless).

**`RefundEndpoints.LoadAuthorizedPaymentAsync` is deliberately NOT changed.** `payments.refund` is
`AdminSensitive` + `rejectStaffPin: true` — no location-bound session can ever reach it (confirmed:
`StaffPinLoginAsync` itself refuses to issue a session carrying any `AdminSensitive` permission, so
a staff-PIN session with `payments.refund` cannot exist; a bare device-token session has no role
permissions at all). The new guard would be a permanent no-op there. Left out to keep the change
scoped, not because refunds are exempt in principle.

**Existing test fallout, not a regression**: three pre-C.2 tests (`OrderEndpointsTests
.Open_Succeeds_ForStaffPinSession`, `PaymentEndpointsTests.RecordPayment_Succeeds_ForStaffPinSession`,
`ReceiptEndpointsTests.Reprint_Succeeds_ForStaffPinSession`) built a staff-PIN session whose device
was never assigned to a terminal — which used to work (TerminalId was always null and never
checked) and now correctly fails closed. Fixed by adding the missing
`POST /api/v1/terminals/{id}/assign-device` call to each test's setup, matching the real
precondition these endpoints now require; not a weakening of the new check.

### Files changed

- `src/DaxaPos.Persistence/Configurations/TerminalConfiguration.cs` — unique filtered index.
- `src/DaxaPos.Persistence/Migrations/20260707023452_AddUniqueTerminalDeviceIdIndex.cs` (+
  `.Designer.cs`, `DaxaDbContextModelSnapshot.cs`) — drops and recreates `IX_terminals_DeviceId` as
  `UNIQUE ... WHERE "DeviceId" IS NOT NULL`. Applied cleanly to the dev DB; zero pre-existing
  duplicates.
- `src/DaxaPos.Api/Endpoints/Identity/TerminalEndpoints.cs` — `AssignDeviceAsync` catches the
  specific unique-violation exception; new `IsUniqueDeviceIdViolation` helper.
- `src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs` — `StaffPinLoginAsync`'s terminal lookup
  hardened against more-than-one match.
- `src/DaxaPos.Api/Endpoints/Orders/OrderEndpoints.cs` — `LoadAuthorizedOrderAsync` and `OpenAsync`
  gain the `TerminalId` check.
- `src/DaxaPos.Api/Endpoints/Payments/PaymentEndpoints.cs`,
  `src/DaxaPos.Api/Endpoints/Receipts/ReceiptEndpoints.cs` — their own `LoadAuthorizedOrderAsync`
  copies get the identical check.
- `tests/DaxaPos.Api.Tests/TerminalEndpointsTests.cs`, `AuthEndpointsTests`/`StaffPinLoginTests.cs`,
  `OrderEndpointsTests.cs`, `PaymentEndpointsTests.cs`, `ReceiptEndpointsTests.cs` — new/updated
  tests (see worker notes for the full list).

### Explicitly out of scope for Milestone C.2

Payments UI, receipts UI, customer display, KDS, MAUI, OI-0018, any change beyond assignment
integrity and terminal-scoped order authorization — per the owner's hard rules for this milestone.

See worker notes for the full test list and closeout verification.

### Milestone C.2 closeout (2026-07-07)

Implemented as planned above, with one deviation discovered while writing tests (see worker
notes' "Deviations from the kickoff plan" — the DB-constraint test needed two independent
`DbContext`s racing a commit rather than one context writing two rows, and the login-hardening
path has no live-duplicate-row integration test since the new index makes that precondition
impossible to construct). No other deviations.

Full solution suite: **1152/1152 passing** (144 unit + 78 Web + 930 API), 12 new API tests, no
regressions against Milestone C.1's 1140-test baseline.

**Live verification**: rebuilt/restarted the `api` container (same local demo stack from C.1;
migration confirmed already applied, "No migrations were applied" in the `migrations` container
log) and exercised the full scenario via `curl`:

- Assigning a device to Terminal A succeeded (200); assigning the *same* device to Terminal B
  was rejected cleanly (409, "This device is already assigned to a different terminal").
- Staff-PIN login on Terminal A's device resolved `terminalId` to Terminal A in `/auth/me`; a
  second device assigned to Terminal B and logged in resolved to Terminal B — two independent,
  correctly-scoped sessions.
- Terminal A's session opening an order for Terminal B was rejected (404); opening for its own
  Terminal A succeeded.
- With an order open on Terminal A: Terminal B's session got 404 on `GET` the order, `POST
  .../void`, `POST .../payments`, and `GET .../receipt` — every one of them, against a single real
  order, from a genuinely different terminal session at the same location. Terminal A's own
  session still succeeded on the same order (`GET` 200, then `POST .../void` 200 to close out the
  verification).

This is the exact attack the review flagged as possible before this milestone (a device could
restore/read/mutate another terminal's order at the same location) — now closed and confirmed
closed against a live, rebuilt API, not just the test suite.

Milestone D (payments/receipts UI) is unblocked: both ownership gaps (TerminalId resolution from
C.1, terminal-scoped authorization from C.2) that made building payments/receipts on top of order
ownership unsafe are now closed.

### Milestone D kickoff decision (2026-07-07)

**No backend/API changes are required for Milestone D.** Every endpoint the payment/receipt flow
needs already exists and is terminal-scoped as of C.2: `POST`/`GET /api/v1/orders/{orderId}/payments`,
`GET /api/v1/orders/{orderId}/receipt`, `POST /api/v1/orders/{orderId}/receipt/reprint`. Confirmed by
reading `PaymentEndpoints.cs`/`ReceiptEndpoints.cs` directly, not assumed from
`docs/modules/payments.md`'s aspirational lifecycle (`Created → SentToTerminal → AwaitingCustomer →
Approved/Declined/Cancelled/TimedOut → Recorded → OrderClosed/PaymentRetry`) — Cash/ManualEftpos jump
straight to `Recorded`, so only `Created` (unreachable) and `Recorded` ever matter for this milestone.

Key confirmed facts driving the design:

- `Order`/`OrderResponse` has **no balance/amount-due/total-paid field** — `GrandTotalAmount` is the
  only total. The payment screen computes
  `amountDue = order.GrandTotalAmount - payments.Where(p => p.Status == Recorded).Sum(p => p.AmountApproved ?? 0)`
  client-side from two already-server-computed numbers (`GET .../orders/{id}` +
  `GET .../orders/{id}/payments`). This is arithmetic over server-authoritative amounts, not a
  tax/pricing recalculation — the same reasoning Milestone C used for its (now-replaced) draft
  subtotal.
- `PaymentSettlement` (unchanged) rejects any payment that would push the running `Recorded` total
  over `Order.GrandTotalAmount` (400, plain string body, no structured error DTO) and flips
  `Order.Status` to `Completed` only on **exact** equality — the UI must treat "order still
  Open/Held after a successful payment" as a normal partial-payment outcome, not an error.
- `PaymentMethod` only has `Cash = 0`, `ManualEftpos = 1`, `Integrated = 2` in code
  (`docs/modules/payments.md`'s GiftCard/StoreCredit are aspirational, not implemented);
  `Integrated` is rejected 400 server-side with no adapter. Milestone D's UI offers only Cash and
  Manual EFTPOS — no Card/Integrated button at all, not even disabled, so as not to imply a
  capability that would just 400.
- `ReceiptResponse` has no persisted `Receipt` row — every `GET .../receipt` and
  `POST .../receipt/reprint` call re-renders live from `Order`/`OrderLine`/`Payment`/`Refund`; reprint
  returns the identical `ReceiptResponse` shape as the plain view and is independently audited
  (`GetAsync` is not).
- `Sales.razor`'s existing Milestone C.1 restore logic already clears `IDraftOrderStore` if a
  restored order's status isn't `Open`/`Held` — `/sales` already can't show a completed order as
  active on a fresh mount. Milestone D adds an **explicit** clear the moment a payment completes the
  order (in the new Pay page), rather than relying solely on that pre-existing defensive check.

**Routes/components**: a new `Pages/Pay.razor` (`@page "/sales/pay/{OrderId:guid}"`), inside the
existing Terminal shell (`MainLayout`, no new layout/session — identical posture to `Sales.razor`).
`Sales.razor` gets one new "Pay" button next to the existing "Clear order" button (same
`_order is { Lines.Count: > 0 }` guard), navigating via
`NavigationManager.NavigateTo($"/sales/pay/{_order.Id}")`.

`Pay.razor` is a single page with two states, not two routes:

1. **Paying** (`order.Status` is `Open`/`Held`): balance-due banner, a payment-history table (from
   `GET .../payments`), a Cash amount field + button and a Manual EFTPOS amount field + button (both
   prefilled with the current balance due, both call `POST .../payments` with a fresh
   `Guid.NewGuid()` idempotency key per submit — see idempotency note below), inline server-error
   display (overpayment, wrong order state, 401/403/404).
2. **Paid/receipt** (`order.Status` is `Completed`, reached either immediately after a payment
   settles it or by loading a `/sales/pay/{id}` URL for an already-completed order): renders the
   `ReceiptResponse` verbatim (lines + marker legend, tax summary, payments, refunds, totals — no
   client math beyond what's already in the DTO), a "Reprint receipt" button
   (`POST .../receipt/reprint`, shows a transient success/failure note), and a "New sale" link back
   to `/sales`.

A `Voided`/`Cancelled` order (only reachable via a stale/shared link — `Sales.razor` itself can't
produce one) shows a plain "This order is no longer payable" message with a link back to `/sales` —
not a crash, not a payment form.

**Idempotency key handling** (documented simplification, not silently glossed over): a new `Guid` is
generated per button click, not persisted/reused across clicks. Submit buttons are disabled while a
request is in flight (`_isBusy`, matching every existing `Sales.razor` handler), which prevents a
double-click from creating two payments for one click. The unhandled edge case is a client-observed
network failure *after* the server already committed the payment (e.g. a dropped response) —
retrying would use a new key and could record a second real payment. No retry-with-same-key UI is
built this milestone; flagged here rather than adding a hidden "remember last key" cache, which would
need its own state-cleanup story.

**Explicitly out of scope** (per the task's hard rules): Stripe/Square/Tap to Pay/integrated card,
provider/device pairing, refund UI, local/USB printer integration, customer display, KDS, any Back
Office expansion, any backend/schema change.

**Existing endpoints consumed** (no new endpoints, no schema changes):

- `GET /api/v1/orders/{orderId}` (existing `GetOrderAsync`)
- `POST /api/v1/orders/{orderId}/payments` (new client method `RecordPaymentAsync`)
- `GET /api/v1/orders/{orderId}/payments` (new client method `GetPaymentsAsync`)
- `GET /api/v1/orders/{orderId}/receipt` (new client method `GetReceiptAsync`)
- `POST /api/v1/orders/{orderId}/receipt/reprint` (new client method `ReprintReceiptAsync`)

**New client DTOs in `Contracts.cs`** (field-for-field mirrors of the server DTOs, confirmed by
reading `PaymentEndpoints.cs`/`ReceiptEndpoints.cs` and their tests directly): `PaymentMethodResult`/
`PaymentStatusResult` (ordinal-matching mirrors of `PaymentMethod`/`PaymentStatus`, same convention as
`OrderStatusResult`), `RecordPaymentRequest`, `PaymentResult`, `ReceiptLineResult`,
`ReceiptTaxSummaryResult`, `ReceiptPaymentResult`, `ReceiptRefundResult`, `ReceiptResult`. The client
`RecordPaymentRequest` omits `TenantId` entirely (never supplied by the client, matching the server's
own "reject client-supplied TenantId" rule) — `ProviderReference` is included but always sent `null`
from the cash/manual EFTPOS UI.

**Draft-clearing strategy**: `Pay.razor` calls `IDraftOrderStore.ClearAsync(device.DeviceId)` the
moment it observes `order.Status == Completed` (whether that's immediately after a payment call or
on initial page load for an already-completed order) — belt-and-braces alongside `Sales.razor`'s
pre-existing on-mount check.

**Tests expected**: `DaxaApiClientTests` (4 new methods × success/401/403/network-failure, matching
the existing pattern); `Pages/PayTests.cs` (new, bUnit) covering: balance-due calculation from
order+payments, full cash payment reaching the receipt state, partial cash payment staying in the
paying state with updated balance and a visible history row, manual EFTPOS payment, a server-rejected
overpayment shown as an inline error, reprint button success/failure, a `Completed`-status initial
load going straight to the receipt state, a `Voided`/404/403 order showing the "not
payable"/error state, draft-store clearing on completion; `Pages/SalesTests.cs` gets one new case
(the "Pay" button navigates to `/sales/pay/{id}` when the order has lines, absent otherwise).

### Milestone D closeout (2026-07-07)

Implemented as planned above, **no deviations**. No backend/API/schema changes anywhere in this
milestone (confirmed: `git diff --stat` touches only `src/DaxaPos.Web`, `tests/DaxaPos.Web.Tests`,
and these two docs).

Summary: `Pages/Pay.razor` (`/sales/pay/{OrderId:guid}`) added to the existing Terminal shell, with
two states — paying (balance-due banner computed client-side from `GetOrderAsync` +
`GetPaymentsAsync`, payment history, Cash/Manual-EFTPOS amount fields prefilled with the balance
due) and paid/receipt (renders `ReceiptResult` verbatim, reprint action, "New sale" link). `Sales.razor`
gained a "Pay" button next to "Clear order" (same `_order is { Lines.Count: > 0 }` guard),
navigating via `NavigationManager`. `Contracts.cs` gained `PaymentMethodResult`/`PaymentStatusResult`
(ordinal-matching mirrors) and `RecordPaymentRequest`/`PaymentResult`/`ReceiptLineResult`/
`ReceiptTaxSummaryResult`/`ReceiptPaymentResult`/`ReceiptRefundResult`/`ReceiptResult`, field-for-field
mirrors confirmed against `PaymentEndpoints.cs`/`ReceiptEndpoints.cs` and their tests directly.
`DaxaApiClient` gained `RecordPaymentAsync`/`GetPaymentsAsync`/`GetReceiptAsync`/`ReprintReceiptAsync`,
all implicit-auth, matching the existing order methods exactly.

`FakeOrderBackend` (the bUnit test double `SalesTests`/`PayTests` both drive) was extended to
simulate payment settlement (rejects a payment that would exceed `GrandTotalAmount`, flips
`Order.Status` to `Completed` on exact settlement) and receipt rendering, so `PayTests.cs` exercises
the same state machine the real API enforces without needing a live backend.

Full solution suite: **1169/1169 passing** (144 unit + 95 Web + 930 API — up from Milestone C.2's
1152; 17 new Web tests, 0 new/changed API tests since no backend code changed). No regressions.

**Test-driven throughout**: each new behaviour (the 4 `DaxaApiClient` methods, `Pay.razor`'s states,
the `Sales.razor` "Pay" button) was written test-first — watched fail for the expected reason (missing
method/component, not a typo), then implemented minimally. One micro-refactor during the green phase:
an initial redundant `orderResult.StatusCode == HttpStatusCode.NotFound ? "..." : "..."` ternary with
identical branches in `Pay.razor` was simplified to a plain string assignment, and the now-unused
`@using System.Net` was removed; both kept the test suite green throughout.

**Known gap flagged, not fixed here**: the payment-record failure path in `Pay.razor` shows one
generic message ("This payment could not be recorded. It may exceed the amount owing.") for every
non-success `RecordPaymentAsync` outcome except 401/403, rather than surfacing the server's actual
message text (e.g. "This payment would exceed the order's grand total." vs. "Payments can only be
recorded against an order that is open or held."). `PaymentEndpoints.cs` returns these as plain-string
`Results.BadRequest(...)`/`Results.Conflict(...)` bodies, not a structured `ProblemDetails`/error DTO
— parsing free-text response bodies client-side felt fragile enough to defer rather than build
silently. A future milestone could add a structured error response server-side (or read the plain
string through `ApiResult<T>.Error` and display it directly) if more precise in-UI messaging becomes
a real need; flagged here rather than adding either silently.

Other gaps carried over from earlier milestones, unaffected by this one: Hold/Resume UI remains
unwired (Milestone C.1); a freshly-provisioned staff member still needs `payments.record` /
`receipts.reprint` granted via a role (unchanged since Milestone C.1's `orders.manage` note — same
`Staff` role already carries all three).

**Live/browser verification**: not performed this session. No browser-automation tool has been
available in any PLAN-0006 session; this milestone's Blazor rendering/event-handling (both `Pay`'s
states and `Sales`'s new button) is exercised via bUnit component tests only, and the state machine
they exercise is proven equivalent to the real server via `FakeOrderBackend`'s settlement logic —
but the `pos-platform-web-1` container has not been rebuilt to serve this code, and no live `curl`
walkthrough against the real API has been run for this milestone specifically (unlike C.1/C.2, which
did rebuild and curl-verify). Rebuilding/restarting the `web` container needs the human's
confirmation first, per this project's standing "ask before rebuilding/restarting api/web containers"
practice — offered as a next step, not done unprompted.

## Manual UI Test Instructions (Milestone D addendum)

Continues from Milestone C.1's manual walkthrough (`docs/plans/active/PLAN-0006-worker-notes.md`'s
"Manual UI Test Instructions" — device setup, terminal assignment, staff PIN login, and adding items
via `/sales` all still apply unchanged). Once the `web` container is rebuilt to serve this milestone's
code:

1. On `/sales` with at least one item in the cart, confirm a **Pay** button appears next to **Clear
   order** — click it. You should land on `/sales/pay/{orderId}` showing "Balance due" equal to the
   order's grand total, with the Cash and Manual EFTPOS amount fields prefilled to that same value.
2. Reduce the Cash amount to less than the balance and click **Record cash payment** — the balance
   due should drop by that amount, the payment should appear under "Payments so far", and the screen
   should stay in the payment-entry state (order not yet fully settled).
3. Enter the remaining balance in Manual EFTPOS and click **Record manual EFTPOS payment** — the
   page should switch to the **Receipt** view automatically (order fully settled server-side).
4. Confirm the receipt shows the correct line items, total, tax summary, and both payments listed.
5. Click **Reprint receipt** — confirm a "Receipt reprinted." confirmation appears.
6. Click **New sale** — you should land back on `/sales` with an empty cart (the draft pointer was
   cleared when the order completed, so a stale/completed order is never restored).
7. Re-navigate to the same `/sales/pay/{orderId}` URL directly (e.g. paste it back into the address
   bar) — you should see the **Receipt** view immediately, not a payment form, since the order is
   already `Completed`.
8. From a second terminal/device session at the same location, attempt to open the first terminal's
   `/sales/pay/{orderId}` URL — you should see "This order could not be found." (C.2's terminal-scoped
   404), not the receipt or payment form.
9. Open a fresh order, attempt to pay more than the balance due in Cash — confirm you see "This
   payment could not be recorded. It may exceed the amount owing." and the order remains unpaid/open.

### Milestone E kickoff decision (2026-07-07)

**No backend/API changes are required for Milestone E.** Every endpoint the display needs already
exists and is already terminal-scoped as of C.2: `GET /api/v1/orders/{orderId}`,
`GET /api/v1/orders/{orderId}/payments`, `GET /api/v1/orders/{orderId}/receipt`. All three already
have `DaxaApiClient` methods from Milestone D (`GetOrderAsync`, `GetPaymentsAsync`,
`GetReceiptAsync`) — no new client DTOs, no new endpoints.

**How the display finds "the current order"**: it reads the same device-scoped `IDraftOrderStore`
pointer (`daxa.sales-draft.v1.{deviceId}`) that `Sales.razor`/`Pay.razor` already read/write. This
only works if the display is a second browser tab/window on the *same device/browser profile* as
the terminal — which matches the product framing directly (Daxa Display is the second screen at the
same counter, sharing the same physical device's browser storage, the PWA analogue of the MAUI
second-window/shared-process model in `docs/modules/customer-display.md`). `localStorage` is shared
across tabs of the same origin, so a second tab's own `DraftOrderStore` instance (each tab is a
separate WASM app instance with its own DI container — singletons do not cross tabs) still reads the
same on-disk value the terminal tab wrote. This is why polling the store each cycle works
cross-tab without any event/broadcast mechanism.

**Display state model**: idle (no device, or device present but no order currently tracked) →
order-building/payment (stored `OrderId` resolves to an `Open`/`Held` order — server-computed line
items/subtotal/tax/total from `GetOrderAsync`, balance-due/paid-so-far from `GetPaymentsAsync`) →
completed/receipt (`Completed` order — renders `GetReceiptAsync` verbatim, same fields `Pay.razor`
already renders). No client-side tax/pricing recalculation anywhere, per architecture rule.

**The "completed orders and cleared drafts behave sensibly" requirement is handled with a
sticky-completed rule.** `Pay.razor` clears the draft pointer the instant an order reaches
`Completed` (Milestone D's belt-and-braces clear) — if the display only ever trusted the store's
current pointer, it would lose the receipt the moment it disappears, right when a customer most
needs to see it. So the display keeps its own in-memory "last displayed order" state, independent of
the store: each poll compares the store's current `OrderId` against what's already shown — a new/
different id switches to it immediately (discarding whatever was shown before, including a still-
displayed receipt); the store going empty is only treated as "reset to idle" if the last known order
was **not** `Completed` (i.e. voided/cleared mid-sale); if it was `Completed`, the receipt keeps
showing until a genuinely new order starts.

**Authorization**: no separate customer login is added. The display rides on whatever
device/session context is already active in that browser tab (there is no other option — a bare
device-token session has `TerminalId: null`, which C.2's `LoadAuthorizedOrderAsync` already rejects
outright for any location-bound session). A 401/403/404 from any poll (mismatched terminal, expired
session, order genuinely gone) degrades to the idle state, never a crash or an error banner — this
is also the real security boundary: the server remains authoritative, matching "UI permission
checks are only UX hints" (CLAUDE.md).

**Routes/components**: `src/DaxaPos.Web/Pages/Display.razor` (`/display`), using a new
`src/DaxaPos.Web/Layout/DisplayLayout.razor` (no staff sidebar/nav — visually distinct per the hard
rule) instead of `MainLayout`. This deliberately bypasses `MainLayout`'s device/session
redirect-to-login guard: forcing a customer-facing screen into the staff PIN/login form would be
wrong UX, and the guard's actual purpose (don't show order data without a valid session) is already
enforced server-side by the same 401/403/404-degrades-to-idle handling above.

**Polling**: a `[Parameter] TimeSpan PollInterval` (default a few seconds) drives a simple
cancellable poll loop over the three read-only endpoints above — no SignalR/realtime infrastructure
added, consistent with the hard rule. Tests override the interval to a small value for speed.

**How staff opens it**: one added link in `NavMenu.razor` (Terminal shell sidebar), `target="_blank"`,
opening `/display` as a second tab/window — `Sales.razor`'s existing (tested) button layout is left
untouched.

**Explicitly out of scope** (per the task's hard rules): MAUI second window, customer input,
loyalty/tip prompts, KDS, printing, any Back Office expansion, any backend/schema change.

**Tests expected**: `tests/DaxaPos.Web.Tests/Pages/DisplayTests.cs` (bUnit, reusing the existing
`FakeOrderBackend`/`InMemoryBrowserStorage` fakes from Milestone D) — no device → idle; device but no
tracked order → idle; an `Open` order with lines → grouped line items and server totals; a partial
payment → balance-due reflects the remainder; a `Completed` order → receipt view; draft cleared
after completion → receipt stays showing (sticky-completed); an order voided and its draft cleared →
resets to idle; a new order starting while an old receipt is showing → switches to the new order; a
terminal-mismatched/404 order → degrades to idle, not an error.

### Milestone E closeout (2026-07-07)

Implemented as planned above, **no deviations**. No backend/API/schema changes anywhere in this
milestone (confirmed: `git diff --stat` touches only `src/DaxaPos.Web` and
`tests/DaxaPos.Web.Tests`, plus these two docs and the pre-existing uncommitted Milestone D files
already in the working tree at session start).

Summary: `Pages/Display.razor` (`/display`, `@layout DisplayLayout`) polls the same device-scoped
`IDraftOrderStore` pointer `Sales.razor`/`Pay.razor` already read/write, resolving it against the
already-terminal-scoped `GetOrderAsync`/`GetPaymentsAsync`/`GetReceiptAsync`. Three states: idle (no
device, or nothing tracked), order-building/payment (grouped line items, server-computed total,
balance-due once a payment is recorded), and completed/receipt (renders the same `ReceiptResult`
fields `Pay.razor` already shows). A new `Layout/DisplayLayout.razor` (no staff sidebar/nav, dark
full-screen styling) keeps it visually distinct from the Terminal shell, and deliberately does not
reimplement `MainLayout`'s redirect-to-login guard — a 401/403/404 from any poll degrades to idle
instead, which is both the correct customer-facing UX (no staff login form on a customer screen) and
the real security boundary (the server remains authoritative). `NavMenu.razor` gained one
`target="_blank"` link to open `/display` as a second tab/window; `Sales.razor`'s existing
Pay/Clear-order buttons were not touched.

The **sticky-completed** rule (the task's explicit "ensure completed orders and cleared drafts
behave sensibly" requirement) tracks the displayed order id in the component's own memory,
independent of the store's pointer: a new/different stored id always switches immediately
(discarding anything currently shown, including a still-visible receipt); the stored pointer
disappearing is only treated as "reset to idle" if the tracked order's **freshly re-fetched**
status isn't `Completed` — re-fetching rather than trusting a possibly-stale in-memory status
matters because `Pay.razor` can clear the pointer in the same tick the order settles, and a poll
landing exactly then must not misread a stale "still Open" snapshot as a voided/abandoned sale.

Full solution suite: **1178/1178 passing** (144 unit + 104 Web + 930 API — up from Milestone D's
1169; 9 new Web tests, 0 API changes). No regressions.

**Test-driven throughout**: `DisplayTests.cs` was written and watched to fail (missing `Display`
type) before `Display.razor`/`DisplayLayout.razor` existed, then implemented to green in one pass —
all 9 cases (idle with/without a device, an open order's line items and total, partial-payment
balance due, a completed order's receipt, sticky-completed after the draft clears, voided-and-
cleared resetting to idle, a new order replacing an old shown receipt, and a 404'd/mismatched order
degrading to idle) passed without further changes.

**Known simplification, not fixed here**: `Display.razor`'s line-item grouping merges by product
name only, not by product+modifier combination like `Sales.razor`'s cart does — two different
modifier selections of the same product would merge into one row on the display (still correct
totals, since those come straight from the server, just a coarser breakdown). Flagged as a
deliberate choice for a read-only customer overview rather than duplicating `Sales.razor`'s
`CartGroup` machinery; a future pass could share a single grouping helper if this ever needs to
match exactly.

**Live/browser verification**: not performed this session, same constraint as every prior PLAN-0006
milestone (no browser-automation tool available). bUnit component tests exercise real Blazor
rendering and the poll-loop's timer-driven state transitions (using a short `PollInterval` test
seam); the `pos-platform-web-1` container has not been rebuilt to serve this code — offered as a
next step, not done unprompted, per the standing "ask before rebuilding/restarting api/web
containers" practice.

## Manual UI Test Instructions (Milestone E addendum)

Continues from the Milestone D addendum above (device setup, terminal assignment, staff PIN login,
adding items, and paying all still apply unchanged). Once the `web` container is rebuilt to serve
this milestone's code:

1. While signed in on the Terminal shell, open the sidebar and click **Customer display** — it
   should open `/display` in a new browser tab. With no active order yet, it should show a plain
   "Daxa POS" / "Welcome — please wait to be served" idle screen (dark, full-screen, no staff
   sidebar/nav — visually distinct from the Sales screen in the other tab).
2. Back in the original tab's `/sales`, add an item to the cart. Within a few seconds, the customer
   display tab should update on its own (no manual refresh) to show that item, its quantity, and the
   order's total.
3. Add a couple more items (including a repeat tap on the same item, and one with a modifier if
   available) — confirm the display's line list and total keep updating.
4. Click **Pay**, record a **partial** cash payment less than the total — within a few seconds the
   display tab should show a **Balance due** line reflecting the remaining amount.
5. Record the remaining balance (Cash or Manual EFTPOS) so the order settles — the display tab
   should switch to "Payment approved — Thank you!" with the same line items/total as the receipt
   view, without needing a manual refresh, even though completing the payment clears the sales
   draft pointer in the other tab.
6. Click **New sale** in the Sales tab and start a fresh order — the display tab should drop the old
   "Thank you" screen and switch to the new order's items once the first item is added, without
   ever flashing back to idle in between (per the "sticky until a new order starts" rule).
7. Start an order, then click **Clear order** *before* paying — the display tab should return to the
   idle "Daxa POS" screen (not stay showing the abandoned cart).
8. From a second terminal/device session at the same location with its own `/display` tab, confirm
   it never shows the first terminal's order (C.2's terminal-scoped authorization applies to
   `/display`'s polling exactly as it does to `/sales`/`/sales/pay`).

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
