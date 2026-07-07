# PLAN-0006 Worker Notes

## Status

Milestone A complete. See "Milestone A Implementation Report" below.

## Purpose Of This Revision

The first PLAN-0006 planning pass was MAUI-first. The current human decision is PWA-first.

This revision records:

- Blazor/PWA first.
- Blazor only for PWA surfaces.
- MAUI deferred.
- OI-0018 production-routing decisions.
- PLAN-0005 historical cleanup.

## Human Decisions Recorded

### 1. PWA First

PLAN-0006 must start with Blazor Server / Blazor PWA.

The PWA can act as a terminal on supported devices. MAUI remains part of the future product
direction, but it is not the starting point for PLAN-0006.

### 2. Blazor Only

Back Office, Terminal, Display, and KDS PWA surfaces use Blazor.

Out of scope:

- React.
- Vue.
- Angular.
- Any non-.NET frontend stack unless a future ADR explicitly changes this.

### 3. MAUI Deferred

MAUI is deferred to a future dedicated Windows terminal plan.

MAUI is still relevant for:

- Dedicated installed Windows terminal software.
- Local Windows hardware integration.
- Local USB/Windows printer support.
- Future terminal-specific installer/update mechanics.

It is not part of PLAN-0006 Milestone A.

### 4. Printer Routing Decisions

OI-0018 stays open.

Recorded decisions:

- Production route codes are strings, not enums.
- Route codes must be slug-like.
- No spaces.
- Lowercase letters/numbers plus hyphen or underscore.
- Recommended pattern: `^[a-z0-9][a-z0-9_-]{0,63}$`.
- For standard products, store production route on the product.
- If no route exists, the item does not print anywhere.
- Combo/grouped products require production components.
- Customer receipts and production dockets are separate document types.

### 5. Printing Ownership

Server owns:

- Server-controlled network receipt printing.
- Worker/outbox print processing.
- Future network production docket printing.

Future MAUI terminal owns:

- Local USB printers physically attached to the terminal.
- Locally installed Windows printers only visible to that terminal.

### 6. PLAN-0005 Historical Cleanup

PLAN-0005 is complete. Its old planning sections must be treated as historical, not current open
work.

Final PLAN-0005 closeout state:

- 1052/1052 tests passing.
- 17 migrations verified clean from empty.
- OI-0017 remains open.
- OI-0018 created during closeout.
- `printing.manage` reserved but unimplemented.

## Revised PLAN-0006 Milestone Shape

| Milestone | Scope |
|-----------|-------|
| A | Blazor/PWA shell, auth/session/device context, Staff PIN login. |
| B | Back Office PWA skeleton and device PIN generation/viewing. |
| C | POS Terminal PWA sales screen. |
| D | Payment and receipt flow in PWA. |
| E | Customer display / display mode in PWA. |
| F | Minimal KDS PWA board. |
| G | Consolidation, RBAC/UX sweep, documentation closeout. |

## Open Items Before Implementation

None block Milestone A if the implementation stays PWA-first.

Still open for later:

- Whether to show a disabled Card button in Milestone D.
- Whether to add refund UI later.
- Whether the minimal KDS board needs a narrow read endpoint.
- When to schedule OI-0018 implementation.
- When to schedule future MAUI terminal work.

## Milestone A Implementation Report

Completed 2026-07-06, in an isolated worktree (`.claude/worktrees/plan-0006-milestone-a`,
branch `worktree-plan-0006-milestone-a`, branched from `origin/main` at `aceec32`).

### Architecture decision made during kickoff

The planning docs said "Blazor only" but left Server-vs-WebAssembly-vs-Auto open (and
`docs/architecture/overview.md` still had "Blazor WASM (TBD)"). Asked the human: confirmed
**standalone Blazor WebAssembly**, not Blazor Server and not Auto render mode. Reasoning: real PWA
installability and future offline resilience need a client that keeps working without a live
connection to a host process, which rules out Interactive Server's constant-SignalR-connection
model. Recorded in the plan doc's Status section.

### What was built

- `src/DaxaPos.Web` — new project, `dotnet new blazorwasm --pwa -f net9.0`, standalone WebAssembly
  (manifest.webmanifest, service-worker.js/.published.js, icons all present from the template).
  Sample Counter/Weather pages and sample-data removed.
  - `Api/` — `DaxaApiClient` (typed `HttpClient` wrapper for device-registration,
    staff-pin/login, logout, auth/me), `AuthHeaderHandler` (`DelegatingHandler` that attaches
    `Bearer {session}` when a live session exists, else `Device {token}` when a device is
    registered, else nothing), `ApiResult`/`ApiResult<T>` (classifies responses into
    Success/Unauthorized/Forbidden/Failed so pages branch without exceptions), `Contracts.cs`
    (client-side mirrors of the PLAN-0003 Identity endpoint DTOs).
  - `Storage/` — `IBrowserStorage`/`LocalStorageBrowserStorage` (thin `localStorage` wrapper via
    JS interop's dotted-identifier resolution, no custom JS file), `JsonLocalStore<T>` (shared
    load/save/clear-with-Changed-event logic used by both stores below).
  - `State/` — `DeviceContext`/`IDeviceContextStore`/`DeviceContextStore` and
    `SessionState`/`IAuthSessionStore`/`AuthSessionStore`. Device context and session are stored
    under separate localStorage keys (`daxa.device-context.v1`, `daxa.session.v1`) and loaded once
    at startup in `Program.cs` before first render, so the shell lands on the right page instead
    of flashing an unauthenticated state, and `AuthHeaderHandler` can read `Current` synchronously
    per request.
  - `Auth/ApiAuthenticationStateProvider` — bridges `SessionState` into Blazor's
    `AuthorizeView`/`[Authorize]`. Role claims and a `daxa:permission` claim are UX hints only —
    the server remains the permission authority, nothing here re-derives access decisions.
  - `Pages/DeviceSetup.razor` (`/device-setup`) — PIN + device type + optional name form calling
    `POST /api/v1/device-registration`; already-registered state shows device info plus a reset
    action instead of the form.
  - `Pages/Login.razor` (`/login`) — staff code + PIN form calling
    `POST /api/v1/auth/staff-pin/login` using the device context's `LocationId`; redirects to
    device setup first if no device is registered.
  - `Pages/Home.razor` (`/`) — the "logged-in shell, no sales screen yet" landing page, gated by
    `AuthorizeView`.
  - `Layout/MainLayout.razor` — route guard (`EnforceRouteGuard`, run on init and on every
    `NavigationManager.LocationChanged`): no device context → `/device-setup`; device context but
    no live session → `/login`; both present → allow. `/device-setup` and `/login` are the only
    public routes. Top row shows device type and, once signed in, staff display name plus a
    Log out button (calls the API logout, clears the session store, notifies the auth state
    provider, navigates to `/login`).
- `src/DaxaPos.Api/Program.cs` — **one small, documented backend change**: added a
  configuration-driven CORS policy (`Cors:AllowedOrigins`, empty/no-cross-origin by default,
  `appsettings.Development.json` lists the Blazor dev-server origins
  `https://localhost:7025`/`http://localhost:5013`). Necessary because the WASM app is a genuine
  separate browser origin from the API and its `fetch` calls need CORS — no existing endpoint could
  substitute for this, and no endpoint contracts changed.
- `tests/DaxaPos.Web.Tests` — new project (xunit + bUnit 1.32.7), 31 tests:
  - `Storage/JsonLocalStoreTests` — load/save/clear round-trip and `Changed` event firing, against
    an in-memory `IBrowserStorage` fake.
  - `State/DeviceContextStoreTests`, `State/SessionStateTests` — device-context round-trip and
    `IsExpired` boundary cases.
  - `Api/AuthHeaderHandlerTests` — Bearer-when-session, Device-when-expired-session-but-device,
    no-header-when-neither.
  - `Api/DaxaApiClientTests` — success/401/403/network-failure classification for staff-pin login,
    device registration, and logout, against a stub `HttpMessageHandler`.
  - `Auth/ApiAuthenticationStateProviderTests` — anonymous/authenticated/expired-session states.
  - `Pages/DeviceSetupTests`, `Pages/LoginTests` (bUnit) — success path persists
    state and navigates, rejected-PIN path shows the generic error message and does not persist
    state, already-registered/no-device states show the right branch instead of the form.
  - `Layout/MainLayoutTests` (bUnit) — all four route-guard combinations (no device → device-setup;
    device only → login; device+session → no redirect; already on `/device-setup` → no redirect).

### Endpoints consumed (all pre-existing, PLAN-0003)

- `POST /api/v1/device-registration`
- `POST /api/v1/auth/staff-pin/login`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me` (client method added; not yet called from any page — reserved for
  Milestone B+ session-refresh/whoami use)

### Verification

- `dotnet build DaxaPos.sln` — succeeded, 0 warnings, 0 errors.
- `dotnet test DaxaPos.sln` — 1083/1083 passed (`DaxaPos.UnitTests` 144, `DaxaPos.Web.Tests` 31,
  `DaxaPos.Api.Tests` 908). No regressions against PLAN-0005's 1052-test baseline.
- No EF Core migrations added or needed (`git status` shows no `Migrations/` changes).
- Confirmed Blazor-only: no React/Vue/Angular package or file added.
- Confirmed not started: MAUI, PLAN-0009, OI-0018, POS sales/order-entry UI, payment UI, receipt
  UI, customer display, KDS.

### Assumptions / follow-ups for later milestones

- Device type select list in `DeviceSetup.razor` offers every `DeviceType` enum value; no UI
  hides irrelevant types for a given surface (e.g. `PaymentTerminal`/`Printer` are not really
  something a human registers as "this browser"). Fine for Milestone A's generic shell; Milestone
  B may want to narrow this once Back Office is the place PINs are actually generated.
- Route guarding and role/permission claims in `ApiAuthenticationStateProvider` are UX
  convenience only; the server remains the authorization authority (CLAUDE.md).
- `GET /api/v1/auth/me` has a client method but no caller yet. Milestone B+ can use it for
  session-refresh/whoami if needed.
- CORS origins for non-dev environments are unset by default (fail-closed) — production/hosted
  deployment docs will need to set `Cors:AllowedOrigins` once a real deployment topology exists.
  This wasn't added to `docs/deployment/` because PLAN-0006's deployment doc scope is Milestone G's
  documentation closeout, not Milestone A.

## Recommended Next Session

Start PLAN-0006 Milestone B only: Back Office PWA skeleton and device-registration-PIN
generation/viewing, building on the `DaxaPos.Web` shell, `DaxaApiClient`, and auth/session
infrastructure from Milestone A.

Do not:

- Start MAUI.
- Start payments UI.
- Start customer display.
- Start KDS.
- Implement OI-0018.
- Start PLAN-0009.
- Add migrations unless a direct blocker is found.

## Milestone B Kickoff Notes (before implementation, 2026-07-07)

### Files likely to change

- `src/DaxaPos.Web/State/BackOfficeSessionState.cs`, `IBackOfficeSessionStore.cs`,
  `BackOfficeSessionStore.cs` — new, mirrors `SessionState`/`AuthSessionStore`, own localStorage
  key (`daxa.backoffice-session.v1`).
- `src/DaxaPos.Web/Api/Contracts.cs` — add client-side DTOs for local login, locations, devices,
  device-registration-pins, product categories, products, menus.
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — add `LocalLoginAsync` (unauthenticated) plus
  explicit-bearer-token overloads for the Back Office read/PIN endpoints.
- `src/DaxaPos.Web/Api/AuthHeaderHandler.cs` — one-line, documented change: skip implicit
  Bearer/Device resolution when the request already carries an explicit `Authorization` header.
- `src/DaxaPos.Web/Layout/BackOfficeLayout.razor`, `BackOfficeNavMenu.razor` — new, separate from
  `MainLayout`/`NavMenu`.
- `src/DaxaPos.Web/Pages/BackOffice/*.razor` — new: `BackOfficeLogin`, `BackOfficeHome`,
  `DeviceRegistrationPins`, `Devices`, `Locations`, `CatalogSetup`.
- Matching new tests under `tests/DaxaPos.Web.Tests/...`.

### Existing endpoints expected to be consumed (all pre-existing, no backend changes)

- `POST /api/v1/auth/local/login`
- `POST /api/v1/device-registration-pins`, `POST /api/v1/device-registration-pins/{id}/revoke`
- `GET /api/v1/locations`
- `GET /api/v1/devices?locationId=`
- `GET /api/v1/product-categories`, `GET /api/v1/products`
- `GET /api/v1/menus`

### Routes to be added

`/back-office/login`, `/back-office`, `/back-office/device-registration-pins`,
`/back-office/devices`, `/back-office/locations`, `/back-office/catalog`.

### Authorization assumptions

- Back Office requires a live Back Office session (issued by `local/login`); no device context is
  needed to reach it at all — ADR-0013's local admin portal login is username/password, independent
  of device registration.
- The Terminal shell (`MainLayout`, `/`, `/login`, `/device-setup`) is untouched; no cross-navigation
  is added between Terminal and Back Office (hard rule: keep them clearly separate).
- Server remains the permission authority; Back Office pages only branch on 401/403 for UX, matching
  Milestone A's convention.

### Tests expected

bUnit tests for `BackOfficeLogin`, `BackOfficeLayout`'s route guard, and `DeviceRegistrationPins`
(generate shows the raw PIN once, revoke); unit tests for the new store and the new `DaxaApiClient`
methods (including that an explicit bearer token is attached and not overwritten by
`AuthHeaderHandler`); a regression test confirming Milestone A's implicit Bearer/Device resolution
still works unchanged when no explicit header is set.

### Known gap flagged, not fixed here

No `GET`/list endpoint exists for `DeviceRegistrationPin` (only create + revoke — the raw PIN is
deliberately never persisted in retrievable form, per ADR-0015). Back Office can therefore only show
the PIN just generated in the current session, not a durable list of previously issued PINs. Adding
a list-metadata endpoint is a backend contract change outside Milestone B's scope; flagging here
rather than adding it silently. A future milestone/OI can pick this up if day-2 PIN visibility
becomes a real operational need.

## Milestone B Implementation Report

Completed 2026-07-07, directly on `main` (no worktree used for this milestone).

### What was built

No deviations from the kickoff notes above. All new code lives in `src/DaxaPos.Web`; no backend
project, schema, or migration changed.

- `src/DaxaPos.Web/State/BackOfficeSessionState.cs`, `IBackOfficeSessionStore.cs`,
  `BackOfficeSessionStore.cs` — new session type/store, own localStorage key
  (`daxa.backoffice-session.v1`), mirrors `SessionState`/`AuthSessionStore`'s shape. `Email` is
  stored client-side only (from the login form) purely for the "Signed in as…" display — the
  server's `LocalLoginResponse` doesn't return one.
- `src/DaxaPos.Web/Api/Contracts.cs` — added `LocalLoginRequest`/`LocalLoginResult`,
  `LocationResult`, `DeviceResult`, `CreateDeviceRegistrationPinRequest`,
  `DeviceRegistrationPinCreatedResult`, `DeviceRegistrationPinResult`, `ProductCategoryResult`,
  `ProductResult`, `MenuResult`.
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — added `LocalLoginAsync` (unauthenticated, matching
  `POST /api/v1/auth/local/login`'s own lack of a `RequireAuthorization()` gate) plus
  explicit-bearer-token methods (`CreateDeviceRegistrationPinAsync`, `RevokeDeviceRegistrationPinAsync`,
  `ListLocationsAsync`, `ListDevicesAsync`, `ListProductCategoriesAsync`, `ListProductsAsync`,
  `ListMenusAsync`, `LogoutBackOfficeAsync`) backed by two new private helpers
  (`PostAuthorizedAsync`/`GetAuthorizedAsync`) that build an `HttpRequestMessage` and set
  `Authorization` directly, rather than relying on `AuthHeaderHandler`'s implicit resolution.
- `src/DaxaPos.Web/Api/AuthHeaderHandler.cs` — one added guard clause: if
  `request.Headers.Authorization` is already set, return immediately without touching it. This is
  the only change to Milestone A's auth plumbing; a regression test
  (`AuthHeaderHandlerTests.SendAsync_WithExplicitAuthorizationHeaderAndLiveStaffSession_DoesNotOverwriteExplicitHeader`)
  locks in that a live Terminal staff session still doesn't leak onto an explicit Back Office call.
- `src/DaxaPos.Web/Layout/BackOfficeLayout.razor`, `BackOfficeNavMenu.razor` — new, structurally
  separate from `MainLayout`/`NavMenu`. Own route guard: only `/back-office/login` is public: no
  device-context requirement at all (ADR-0013 local admin login needs no device).
- `src/DaxaPos.Web/Pages/BackOffice/BackOfficeLogin.razor` (`/back-office/login`),
  `BackOfficeHome.razor` (`/back-office`), `DeviceRegistrationPins.razor`
  (`/back-office/device-registration-pins`), `Devices.razor` (`/back-office/devices`, optional
  location filter), `Locations.razor` (`/back-office/locations`), `CatalogSetup.razor`
  (`/back-office/catalog`, read-only products joined with category name + menus).
- `src/DaxaPos.Web/Program.cs` — registered `IBackOfficeSessionStore`/`BackOfficeSessionStore` and
  added it to the startup `EnsureLoadedAsync()` sequence alongside the existing two stores.
- Tests: `tests/DaxaPos.Web.Tests/State/BackOfficeSessionStoreTests.cs`,
  `Api/BackOfficeApiClientTests.cs`, `Layout/BackOfficeLayoutTests.cs`,
  `Pages/BackOffice/BackOfficeLoginTests.cs`, `Pages/BackOffice/DeviceRegistrationPinsTests.cs`,
  plus the `AuthHeaderHandlerTests` regression case above and a store-equality note (see
  Verification below).

### Deviations from the kickoff plan

None. The route list, file list, and architecture matched what was recorded before implementation.

### Verification

- `dotnet build DaxaPos.sln` — succeeded, 0 errors (pre-existing `NU1510` warning on
  `DaxaPos.Infrastructure`, unrelated to this work).
- `dotnet test DaxaPos.sln` — 1105/1105 passed (`DaxaPos.UnitTests` 144, `DaxaPos.Web.Tests` 53,
  `DaxaPos.Api.Tests` 908). No regressions against Milestone A's 1083-test baseline; 22 new Web
  tests added.
- One test-writing gotcha worth recording: a `BackOfficeSessionState` JSON round-trip test using
  `Assert.Equal` failed even though the round-trip was correct — `List<string>` (from
  deserialization) has no value equality against the original array-literal collections. Fixed by
  using `Assert.Equivalent` for that one assertion (deep/structural, order-insensitive). Worth
  remembering for any future record with collection properties round-tripped through
  `JsonLocalStore<T>`.
- No EF Core migrations added or needed (`git status` shows no `Migrations/` changes; confirmed no
  files outside `src/DaxaPos.Web`, `tests/DaxaPos.Web.Tests`, and these two docs changed).
- Backend wire-format verification: an already-running local demo stack was found via `docker ps`
  (`pos-platform-api-1`/`pos-platform-web-1`/`pos-platform-db-1`, up ~9h, healthy) — not started by
  this session. Used `curl` against it with the existing bootstrap admin credentials
  (`docs/testing/local-smoke-test.md`'s flow) to exercise every endpoint/field this milestone's
  client DTOs assume: `local/login`, `auth/me`, `locations` (list), `product-categories`/`products`/
  `menus` (list, all empty on this instance — no PLAN-0004 demo data seeded, but confirmed
  200 + correct empty-array shape), `devices` (list, with and without `locationId` filter),
  `device-registration-pins` (create, then revoke, using a real location). Every JSON field name and
  shape returned matched `Contracts.cs` exactly (System.Text.Json's Web defaults handle the
  camelCase-server/PascalCase-client mismatch, same as Milestone A already relied on).
- What was not verified: the Blazor UI itself was not driven in an actual browser. bUnit component
  tests exercise real component rendering, data binding, and click handling, but not a real DOM/JS
  runtime — no browser-automation tool was available in this session. The running
  `pos-platform-web-1` container (a docker image build of `DaxaPos.Web` from ~9h before these
  changes) was deliberately left untouched/un-rebuilt rather than mutating the user's existing demo
  environment without being asked. If a genuine browser walkthrough is wanted, the next step is
  `docker compose build web && docker compose up -d web` (or `dotnet run --project src/DaxaPos.Web`
  against the running API) followed by a manual pass through `/back-office/login` →
  `/back-office/device-registration-pins` → `/back-office/devices`/`locations`/`catalog`.

### Remaining gaps / follow-ups

- No durable list of previously issued device registration PINs (see "Known gap" above) — only the
  one just generated in the current browser session can be viewed/revoked. A future OI can pick
  this up if it becomes a real operational need; it would require a new (metadata-only, never
  raw-PIN) `GET` endpoint.
- `CatalogSetup.razor`'s products/menus lists were verified against an instance with no PLAN-0004
  demo data (both returned `[]`); the non-empty rendering path is covered by bUnit-level assumptions
  about the response shape but wasn't exercised against real seeded product/menu rows.
- Device rotate/revoke actions were deliberately left out of `Devices.razor` (read-only, per
  Milestone B's "basic device/location context views" framing) even though the backend endpoints
  exist; a later milestone can add them if Back Office needs to manage devices, not just view them.

## Recommended Next Session (updated)

Start PLAN-0006 Milestone C: POS Terminal PWA sales screen, building on the `DaxaPos.Web` Terminal
shell from Milestone A (not the Back Office code from Milestone B — they are deliberately separate
route trees/sessions).

Do not:

- Start MAUI.
- Start payments UI (Milestone D).
- Start customer display (Milestone E).
- Start KDS (Milestone F).
- Implement OI-0018.
- Start PLAN-0009.
- Add migrations unless a direct blocker is found.

## Milestone C Kickoff Notes (before implementation, 2026-07-07)

See the plan doc's "Milestone C kickoff decision" section for the full reasoning. Summary below.

### Files likely to change

- `src/DaxaPos.Web/Api/Contracts.cs` — add `ResolvedMenuItemResult`, `ResolvedMenuSectionResult`,
  `ResolvedMenuResult` (client mirrors of `ResolvedMenuEndpoints`' response DTOs; `TaxTreatment` is
  omitted — it's an unconfigured-default numeric enum server-side and unused by this milestone's
  UI).
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — add `GetResolvedMenuAsync(locationId)` via the existing
  implicit-auth `GetAsync<T>` helper (no explicit bearer token needed; this is a Terminal-shell
  staff-session call).
- `src/DaxaPos.Web/Pages/Sales.razor` — new, `/sales` route. Fetches the resolved menu for
  `DeviceContextStore.Current.LocationId`; renders sections/tiles; local in-memory cart
  (add/increment/decrement/remove, per-line notes); "Clear order" (local equivalent of
  void/cancel, since there is no server-side order to void).
- `src/DaxaPos.Web/Pages/Home.razor` — add a link to `/sales`.
- `src/DaxaPos.Web/Layout/NavMenu.razor` — add a "Sales" nav link.
- New tests under `tests/DaxaPos.Web.Tests/Pages/SalesTests.cs` (and a
  `DaxaApiClientTests`/`Contracts` addition for `GetResolvedMenuAsync`).

### Existing endpoints expected to be consumed (all pre-existing, no backend changes)

- `GET /api/v1/menus/resolved?locationId=`

### Routes/components

`/sales` inside the existing Terminal shell (`MainLayout`), not Back Office. No new layout, no new
session/store.

### Authorization/session assumptions

- Uses the existing Milestone A staff/device session and `MainLayout`'s route guard unchanged — no
  new auth plumbing (unlike Milestone B, which needed a whole second session type).
- `ResolvedMenuEndpoints` independently checks that a location-bound staff session's
  `AuthContext.LocationId` matches the requested `locationId` — the client always requests its own
  `DeviceContext.LocationId`, so this should never surface as a mismatch in normal use, but a 404 is
  handled as a normal empty/error state, not a crash.

### DTO assumptions

- `ResolvedMenuItemResponse`: `ProductId`, `ProductName`, `DisplayOrder`, `Price`, `IsTaxInclusive`,
  `TaxCategoryCode` (all consumed); `TaxTreatment` (enum, not consumed — see gap note above).
- `ResolvedMenuSectionResponse`: `MenuId`, `MenuSectionId`, `SectionName`, `DisplayOrder`, `Items`.
- `ResolvedMenuResponse`: `LocationId`, `Sections`.
- No modifier data anywhere in this DTO (see gap note above) — no modifier UI built.

### Tests expected

- `Api/DaxaApiClientTests` (or a new file) — `GetResolvedMenuAsync` success/401/403/network-failure
  classification, matching the existing `DaxaApiClientTests` pattern.
- `Pages/SalesTests.cs` (bUnit) — menu sections/tiles render from a fake resolved-menu response;
  tapping a tile adds a line to the order panel; tapping again increments quantity; decrementing to
  zero removes the line; a notes field updates line state; "Clear order" empties the draft;
  no-device/no-location and load-failure states render without crashing.

### Explicit out-of-scope items (Milestone C)

- Real order create/add-line/void-line/hold/resume/void/cancel against `POST /api/v1/orders` (see
  the TerminalId gap above).
- Payments, customer display, KDS, printing.
- Modifier selection UI.
- Any Back Office expansion.
- Any backend schema/endpoint change.

## Milestone C Implementation Report

Completed 2026-07-07, directly on `main`.

### What was built

No deviations from the kickoff notes above.

- `src/DaxaPos.Web/Api/Contracts.cs` — added `ResolvedMenuItemResult`, `ResolvedMenuSectionResult`,
  `ResolvedMenuResult` (mirrors `ResolvedMenuEndpoints`' response DTOs; `TaxTreatment` omitted, see
  kickoff notes).
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — added `GetResolvedMenuAsync(locationId)` via the existing
  implicit-auth `GetAsync<T>` helper (no explicit bearer — this is a Terminal-shell call, unlike
  Milestone B's Back Office methods).
- `src/DaxaPos.Web/Pages/Sales.razor` — new, `/sales` route, default layout (`MainLayout`, no
  `@layout` override — it's a Terminal-shell page like `Home`/`Login`/`DeviceSetup`, so Milestone
  A's route guard already protects it with zero new code). Fetches
  `GET /api/v1/menus/resolved?locationId={DeviceContextStore.Current.LocationId}` on init; renders
  sections ordered by `DisplayOrder`, each as a heading plus a wrapping row of tile buttons (name +
  formatted price). Tapping a tile adds a line to a local, in-memory `List<OrderDraftLine>` (a
  private nested class, matching the `LoginModel`/`DeviceSetupModel` convention in other pages) or
  increments its quantity if already present. Each cart line has -/+ buttons (decrementing to zero
  removes the line), a "Remove" link, and a free-text notes input. A "Draft subtotal (estimate)"
  total sums `UnitPrice × Quantity` across lines, labelled explicitly as an estimate with a note
  that no server order exists and payment isn't part of this screen — this is arithmetic
  aggregation of already-server-resolved unit prices, not a client-side tax/pricing recalculation
  (CLAUDE.md's rule is about not re-deriving tax/discount/pricing logic, which this doesn't do).
  "Clear order" empties the draft (the local equivalent of void/cancel — there is no server order
  to void). No device/no menu/load-error/empty-menu states are all handled without crashing.
  Responsive layout uses Bootstrap's grid only (`col-12 col-lg-8`/`col-12 col-lg-4`, `d-flex
  flex-wrap`) — no new CSS, matching "follow existing styling."
- `src/DaxaPos.Web/Pages/Home.razor` — replaced the "sales screen is not part of Milestone A" stub
  line with a link to `/sales`.
- `src/DaxaPos.Web/Layout/NavMenu.razor` — added a "Sales" nav link (Terminal shell's `NavMenu`,
  not Back Office's).
- Tests: `tests/DaxaPos.Web.Tests/Api/DaxaApiClientTests.cs` — two new cases for
  `GetResolvedMenuAsync` (success + location-id query-string assertion; not-found → Failed kind).
  `tests/DaxaPos.Web.Tests/Pages/SalesTests.cs` — new, 8 bUnit tests: no-device prompt, load-error
  message, empty-menu message, tap-to-add, tap-twice-increments (not duplicate lines), decrement-
  to-zero removes the line, clear-order empties the draft, notes-input updates line state.

### Deviations from the kickoff plan

None.

### Verification

- `dotnet build DaxaPos.sln` — succeeded, 0 errors (same pre-existing `NU1510` warning, unrelated).
- `dotnet test DaxaPos.sln` — 1115/1115 passed (`DaxaPos.UnitTests` 144, `DaxaPos.Web.Tests` 63,
  `DaxaPos.Api.Tests` 908). No regressions against Milestone B's 1105-test baseline; 10 new Web
  tests added.
- No EF Core migrations added or needed; confirmed no files outside `src/DaxaPos.Web`,
  `tests/DaxaPos.Web.Tests`, and these two docs changed (`git status`).
- Backend contract verification, live, against the same already-running local demo stack Milestone
  B found (not started by this session): seeded real data via `curl` — a tax category (`AU GST 10%`,
  `taxTreatment: 0`), a tax definition (10%, `Australia`/`Country`), a tax-category-definition link,
  a venue tax configuration for the demo location (none existed yet), a product ("Flat White",
  $5.50), an org-wide menu/section/section-item — then called
  `GET /api/v1/menus/resolved?locationId=` and got back exactly the shape
  `ResolvedMenuResult`/`ResolvedMenuSectionResult`/`ResolvedMenuItemResult` assume: `locationId`,
  `sections[].menuId/menuSectionId/sectionName/displayOrder/items[]`,
  `items[].productId/productName/displayOrder/price/isTaxInclusive/taxCategoryCode` (plus
  `taxTreatment`, deliberately unconsumed). Then went one step further than Milestone B's
  verification: created a device-registration PIN, registered a device, created a brand-new staff
  member (zero roles/permissions — the realistic default), staff-PIN-logged-in, and re-ran the
  identical resolved-menu call **as that staff session** — same exact shape, confirming
  `ResolvedMenuEndpoints`'s "no permission gate" design works from a genuine Terminal-shell caller,
  not just an admin session. That same staff session's `roles: []`/`permissions: []` also
  independently reconfirmed the kickoff decision's `TerminalId`/`orders.manage` gap: even setting
  the `TerminalId` problem aside, a freshly-provisioned staff member has no `orders.manage` grant by
  default, so `POST /api/v1/orders` would 403 regardless — real-world evidence the local-draft
  scope was the right call, not just a theoretical one.
- What was not verified: the Blazor UI was not driven in an actual browser — no browser-automation
  tool was available this session (same as Milestone B). bUnit component tests exercise real
  rendering, event binding, and click handling, but not a real DOM/JS runtime. The running
  `pos-platform-web-1` container (a docker image build from before these changes) was deliberately
  left untouched/un-rebuilt.

### Remaining gaps / follow-ups

- **The `TerminalId` gap is the main blocker for a real Milestone C→D upgrade path.** Before any
  future milestone wires the sales screen to `POST /api/v1/orders` for real, one of the following
  needs to happen (not decided here — flagging for a future ADR/plan refresh):
  - a staff-accessible `GET /api/v1/terminals?locationId=` (scoped to the caller's own location,
    mirroring `ResolvedMenuEndpoints`'s own `AuthContext.LocationId` check), or
  - populating `Terminal.DeviceId` at device-registration time so a device can resolve "its"
    terminal (the column exists; no endpoint writes it today), or
  - some other session-level default-terminal resolution.
- Relatedly, a freshly-provisioned staff member has zero permissions by default (confirmed live) —
  granting `orders.manage` (and deciding whether to also grant `payments.record` up front) is a
  Back Office/role-management question, not something this milestone's read-only Back Office
  scope covers.
- The local draft never persists (no localStorage, unlike `DeviceContext`/`SessionState`) — a page
  refresh or navigation away loses it. This was a deliberate simplicity choice ("thin, working
  skeleton"), not an oversight; flagging in case a future milestone wants cart persistence before
  wiring real order submission.
- No modifier UI (see kickoff decision) — would need either modifiers embedded in
  `ResolvedMenuItemResponse` or a staff-accessible modifier-lookup endpoint.
- Quantity has no direct-entry option (only +/-), and there's no fast item search — both are in the
  `docs/modules/01-core-pos-sales-screen.md` wishlist but explicitly out of scope for this "thin
  skeleton" milestone.

## Milestone C.1 Kickoff Notes (before implementation, 2026-07-07)

See the plan doc's "Milestone C.1 kickoff decision" for full reasoning. Triggered by explicit owner
product decisions (2026-07-07): TerminalId must be genuinely resolvable (no fake/null IDs),
modifiers are mandatory on the sales screen, the draft must survive refresh via the safest
mechanism available, and backend contract changes are authorised when justified.

### Confirmed current-state facts (read, not assumed)

- `Terminal.DeviceId` (nullable Guid, FK `SetNull`) is already a real migrated column
  (`TerminalConfiguration.cs`) — no migration needed for TerminalId resolution.
- `AuthSession.TerminalId` and `SessionAuthenticationHandler`'s read of it already exist; only the
  write (at staff-PIN-login time) is missing.
- `GET /api/v1/auth/me` already returns `AuthContextResponse.TerminalId`; the Web client's
  `GetMeAsync()` already exists end-to-end (Milestone A) but has no caller yet.
- `ModifierGroup`/`Modifier`/`ProductModifierGroup` (with `SelectionMin`/`SelectionMax`/
  `IsRequired`) already exist in full (PLAN-0004). `POST /api/v1/orders/{id}/lines` already accepts
  `ModifierIds` and validates linkage/active-status, but not group cardinality.
- `orders.manage` is already staff-PIN-eligible (`rejectStaffPin` defaults false) — confirmed live
  in Milestone C's own verification with a zero-permission staff member (would 403 without the
  permission grant, which is a Back Office role-configuration step, not a code gap).
- `OrderLine` has no update endpoint (append/void only) — quantity changes must add/void whole
  `Quantity: 1` lines, never mutate one.

### Files likely to change

Backend:

- `src/DaxaPos.Api/Endpoints/Identity/TerminalEndpoints.cs` — `UpdateTerminalRequest` gains
  `Guid? DeviceId`; `UpdateAsync` validates and assigns/unassigns/rejects-conflict.
- `src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs` — `StaffPinLoginAsync` resolves
  `Terminal` by `DeviceId` and sets `AuthSession.TerminalId`.
- `src/DaxaPos.Api/Endpoints/Menus/ResolvedMenuEndpoints.cs` — add `ResolvedModifierResponse`,
  `ResolvedModifierGroupResponse`; extend `ResolvedMenuItemResponse` with `ModifierGroups`; batch
  query in the handler.
- `src/DaxaPos.Api/Endpoints/Orders/OrderEndpoints.cs` — `AddLineAsync` gains
  required/min/max group-cardinality enforcement.
- Matching new/extended tests under `tests/DaxaPos.Api.Tests/`.

Web (Terminal shell):

- `src/DaxaPos.Web/State/SessionState.cs` — add `Guid? TerminalId`.
- `src/DaxaPos.Web/Pages/Login.razor` — call `GetMeAsync()` post-login, persist `TerminalId`.
- `src/DaxaPos.Web/Api/Contracts.cs` — add `ResolvedModifierResult`/`ResolvedModifierGroupResult`
  (extend `ResolvedMenuItemResult`); add order client DTOs (`CreateOrderRequest`, `OrderResult`,
  `OrderLineResult`, `OrderLineModifierResult`, `AddOrderLineRequest`).
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — add `OpenOrderAsync`, `GetOrderAsync`,
  `AddOrderLineAsync`, `VoidOrderLineAsync`, `VoidOrderAsync` (implicit-auth, Terminal-shell
  pattern, matching `GetResolvedMenuAsync`).
- `src/DaxaPos.Web/State/IDraftOrderStore.cs`, `DraftOrderStore.cs` — new, device-scoped
  localStorage `OrderId` pointer (`daxa.sales-draft.v1.{deviceId}`).
- `src/DaxaPos.Web/Pages/Sales.razor` — rewritten: no-terminal state, modifier modal, real
  order/line/void calls, grouped-line display, server-computed totals, draft restore.
- Matching new/extended tests under `tests/DaxaPos.Web.Tests/`.

Back Office:

- `src/DaxaPos.Web/Pages/BackOffice/Terminals.razor` — new: list/create terminals, assign/unassign
  device.
- `src/DaxaPos.Web/Api/Contracts.cs`/`DaxaApiClient.cs` — add `TerminalResult`,
  `CreateTerminalRequest`, `UpdateTerminalRequest` client DTOs and explicit-bearer methods
  (`ListTerminalsAsync`, `CreateTerminalAsync`, `UpdateTerminalAsync`).
- `src/DaxaPos.Web/Layout/BackOfficeNavMenu.razor` — add "Terminals" link.
- Matching new tests under `tests/DaxaPos.Web.Tests/Pages/BackOffice/`.

### Existing endpoints reused (no new endpoints added)

`POST /api/v1/orders`, `GET /api/v1/orders/{id}`, `POST /api/v1/orders/{id}/lines`,
`DELETE /api/v1/orders/{id}/lines/{lineId}`, `POST /api/v1/orders/{id}/void`,
`GET /api/v1/menus/resolved`, `GET /api/v1/auth/me`, `PATCH /api/v1/terminals/{id}` (extended,
not new), `POST /api/v1/terminals`, `GET /api/v1/terminals`, `GET /api/v1/devices`.

### Tests expected

- `TerminalEndpointsTests` — assign device (success), assign device from a different location
  (404), assign device already linked to another active terminal (409), unassign (`DeviceId: null`),
  regression on plain name update.
- `AuthEndpointsTests`/staff-pin-login tests — `AuthSession.TerminalId` set when the device's
  terminal link exists (verified via a follow-up `/auth/me` call in-test); stays null when no link
  exists (regression).
- `ResolvedMenuEndpointsTests` — modifier groups/options present for a product with assigned
  groups; empty list (not null) for a product with none; inactive group/modifier excluded.
- `OrderEndpointsTests` — add-line rejects a missing required-group selection, rejects
  under/over `SelectionMin`/`SelectionMax`, accepts a valid combination (regression on the
  existing not-linked/inactive checks).
- `SessionStateTests`, `LoginTests` — `TerminalId` captured from `/auth/me` and persisted;
  login still succeeds if `/auth/me` fails (degrades to "not linked" UX, not a login failure).
- `DraftOrderStoreTests` — round-trip, device-scoped key isolation, clear-on-void/404.
- `SalesTests` — no-terminal blocking state; required-modifier modal blocks "Add" until satisfied;
  optional modifiers selectable within min/max; first tap opens a real order; repeat taps add
  further `Quantity: 1` lines grouped for display; `-`/"Remove" void the right line(s); "Clear
  order" voids the whole order server-side; refresh restores the draft from `GetOrderAsync`; a
  404'd/closed stored order clears the pointer and starts empty.
- `Pages/BackOffice/TerminalsTests` — list/create/assign/unassign, including the 409-conflict and
  cross-location-404 paths surfaced as Back Office error states.

### Explicitly out of scope for Milestone C.1

Hold/Resume UI, payments, customer display, KDS, printing, split bills, quantity direct-entry,
item search, any Back Office expansion beyond the one Terminals page.

### Browser automation

No browser-automation tool has been available in any PLAN-0006 session so far (Milestones A-C all
documented this same constraint). Checked again for this milestone — still none available in this
session. Continuing the established fallback: bUnit component tests for real rendering/event
behaviour, `curl` against the already-running local demo stack for live backend-contract
verification, and explicit manual click-through steps recorded at closeout.

## Milestone C.1 Implementation Report

Completed 2026-07-07, directly on `main`, in six scoped commits (backend TerminalId, backend
modifiers, Web client plumbing, Sales.razor rewrite, Back Office Terminals page, this closeout).

### Deviation from the kickoff plan

`AssignTerminalDeviceRequest`/`POST /api/v1/terminals/{id}/assign-device` was added as its own
endpoint instead of extending `UpdateTerminalRequest` with `DeviceId` as originally sketched — see
the plan doc's closeout section for the reasoning (avoids "omitted field" ambiguity on a plain
rename call). Everything else matched the kickoff notes with no other deviations.

### Verification

- `dotnet test DaxaPos.sln` — 1140/1140 passing (`DaxaPos.UnitTests` 144, `DaxaPos.Web.Tests` 78,
  `DaxaPos.Api.Tests` 918). No regressions against Milestone C's 1115-test baseline.
- No EF Core migrations added; confirmed live (see below) — the `migrations` container logged "No
  migrations were applied" against the rebuilt image.
- Full live verification against the local demo stack, **with the human's explicit confirmation
  to rebuild/restart the `api`/`web` containers first** (they were built before this milestone's
  changes, unlike Milestones B/C which could verify against the stale containers because they
  didn't touch existing endpoint behaviour). See the plan doc's closeout section for the exact
  sequence: device→terminal assignment, staff-PIN login resolving a real `TerminalId` in
  `/auth/me` (previously always `null`), resolved-menu modifier data (empty for a plain product,
  populated for one with an assigned required group), a 400 when a required modifier is omitted
  from add-line, a successful add-line with the correct server-computed totals, and a working
  void-line/void-order.
- No browser-automation tool was available this session (same constraint as every prior PLAN-0006
  milestone — checked again, still absent). bUnit exercises real Blazor rendering/event handling
  for the modifier modal (required-selection blocking the Add button, optional-selection limits),
  the grouped cart display, and the stored-`OrderId` restore/clear-on-404 paths.

### Remaining gaps / follow-ups

- Line notes free-text entry was dropped from the real-order sales screen (no order-line update
  endpoint exists to support editing it afterward) — flagged in the plan doc's closeout, not fixed
  silently.
- Hold/Resume UI is unwired (the endpoints exist and are staff-PIN-eligible; a future milestone can
  wire "Hold" to free up a terminal for another order and "Resume" to return to it).
- A freshly-provisioned staff member still has zero permissions by default — granting
  `orders.manage` (this milestone's live verification did so via the `Staff` role) remains a Back
  Office/role-management concern outside this milestone's scope, same as prior milestones noted.
- `Terminals.razor`'s per-terminal device dropdown only lists devices already registered at that
  terminal's location (via the existing `GET /api/v1/devices?locationId=`); there's no in-page way
  to register a new device — admins use the existing Device Registration PINs page for that, then
  come here to assign it.

## Manual UI Test Instructions

The `api`/`web` docker images were rebuilt and restarted this session, so they now serve
Milestone C.1's code. Local run commands (from repo root):

```bash
docker compose up -d --build   # or: docker compose build api web && docker compose up -d api web
```

Then open `http://localhost:8080` in a browser and walk through:

1. **Back Office** (`http://localhost:8080/back-office/login`) — sign in with the bootstrap admin
   (`admin@daxapos.local` / the local dev password in `deploy/.env`).
2. Go to **Locations** and note (or create) a location; go to **Device Registration PINs** and
   generate a PIN for it.
3. Go to **Terminals** — create a terminal for that location if none exists.
4. In a **separate browser/incognito window** (this is the Terminal shell, a different session
   type from Back Office): open `http://localhost:8080/device-setup`, enter the PIN from step 2,
   pick a device type, and register.
5. Back in Back Office → **Terminals**: the new device should now appear in the dropdown next to
   the terminal from step 3 — select it and click **Assign**. "Unassigned" should change to the
   device's name.
6. Back Office → **Staff members** is not yet built in this plan; use the API directly (or a prior
   session's staff member) to ensure a staff code/PIN exists with the `Staff` role, or grant
   `orders.manage` via `POST /api/v1/staff-members/{id}/roles`.
7. In the Terminal shell window: go to `/login`, sign in with the staff code/PIN.
8. Go to `/sales`. If the device isn't assigned to a terminal, you should see the "isn't linked to
   a POS terminal yet" message instead of the menu — confirm this by testing with an unassigned
   device first, then retry after step 5.
9. Tap a product with a required modifier group (e.g. one with a "Doneness" group configured via
   the API, as in this session's live verification) — a modal should appear; the **Add** button
   should stay disabled until a required selection is made.
10. Tap a plain product with no modifier groups — it should add directly, no modal.
11. Tap the same product tile again — the cart row's quantity should increase (a second real order
    line was added and grouped for display, not a client-side counter).
12. Use **-**/**Remove** on a cart row — confirm the row's quantity decreases or the row disappears.
13. **Refresh the page** — the cart should still show the same items (rebuilt from the real order
    on the server), not an empty draft.
14. Click **Clear order** — the cart should empty; refreshing again should not bring it back (the
    server-side order was voided).

## Milestone C.2 Kickoff Notes (before implementation, 2026-07-07)

A post-C.1 review (owner-requested, answering a structured list of questions about the actual
current implementation) surfaced two gaps serious enough that the owner chose to fix them before
Milestone D rather than file open issues — see the plan doc's "Milestone C.2 kickoff decision" for
the full reasoning. Summary below.

### Confirmed current-state facts (read, not assumed)

- `TerminalConfiguration.cs` had no unique index on `DeviceId` — only the index EF Core
  auto-creates for the FK. `AssignDeviceAsync`'s 409 pre-check was the only protection, and it's
  check-then-act.
- `AuthEndpoints.StaffPinLoginAsync`'s terminal lookup used `.SingleOrDefaultAsync()`, which throws
  if more than one `Terminal` ever matched the same `DeviceId`.
- `LoadAuthorizedOrderAsync` is duplicated verbatim in `OrderEndpoints.cs`, `PaymentEndpoints.cs`,
  and `ReceiptEndpoints.cs` (each with its own copy, cross-referencing the others in a doc comment)
  — none of the three checked `TerminalId`, only `OrganisationId`/`LocationId`.
  `RefundEndpoints.LoadAuthorizedPaymentAsync` has the same shape but is unreachable from any
  location-bound session (`payments.refund` is `AdminSensitive` + `rejectStaffPin: true`, and
  `StaffPinLoginAsync` independently refuses to issue a session carrying any `AdminSensitive`
  permission) — confirmed, not changed.
- `OrderEndpoints.OpenAsync` validated the requested `Terminal` belongs to the caller's
  organisation/location but never cross-checked it against `authContext.TerminalId` — a
  location-bound session could open an order for *any* terminal at its own location by supplying
  that terminal's id in the request body.

### Files changed

See the plan doc's kickoff decision for the full list (`TerminalConfiguration.cs` + migration,
`TerminalEndpoints.cs`, `AuthEndpoints.cs`, `OrderEndpoints.cs`, `PaymentEndpoints.cs`,
`ReceiptEndpoints.cs`, plus test files).

### Tests expected

- `TerminalEndpointsTests` — a new test proving the DB constraint itself rejects a duplicate
  assignment even when the pre-check is bypassed (direct `DbContext` write), and that the endpoint
  surfaces it as 409, not 500.
- `StaffPinLoginTests` — login does not throw and fails cleanly (audited
  `DuplicateTerminalAssignment`) when two terminals are seeded (directly via `DbContext`, bypassing
  the endpoint) with the same `DeviceId`.
- `OrderEndpointsTests`/`PaymentEndpointsTests`/`ReceiptEndpointsTests` — new
  cross-terminal-isolation tests: Terminal A's staff session cannot `GET`/add-line/void-line/
  void-order/hold/resume Terminal B's order, cannot record a payment against it, cannot view or
  reprint its receipt; Terminal A can still do all of the above to its own order. A no-TerminalId
  (device not yet assigned) staff session cannot open an order at all.
- Fix (not new tests, but required): three existing staff-PIN-session-succeeds tests across those
  three files needed a `POST .../assign-device` call added to their setup, since they previously
  relied on the now-closed gap (TerminalId always null, never checked).

### Explicitly out of scope for Milestone C.2

Payments UI, receipts UI, customer display, KDS, MAUI, OI-0018, `RefundEndpoints` changes (confirmed
unreachable from any session the new check would affect).

## Milestone C.2 Implementation Report

Completed 2026-07-07, directly on `main`.

### What was built

- Unique filtered index (`CREATE UNIQUE INDEX ... WHERE "DeviceId" IS NOT NULL`), migration
  `20260707023452_AddUniqueTerminalDeviceIdIndex`. Checked the dev DB for existing duplicates
  before applying (`SELECT "DeviceId", COUNT(*) FROM terminals WHERE "DeviceId" IS NOT NULL GROUP
  BY "DeviceId" HAVING COUNT(*) > 1` — zero rows), so no cleanup path was needed; applied cleanly.
- `TerminalEndpoints.AssignDeviceAsync` wraps `SaveChangesAsync()` in a `try/catch` for
  `DbUpdateException` matched against the specific `PostgresException` (SQLSTATE `23505`,
  `ConstraintName == "IX_terminals_DeviceId"`) and returns the same 409 message the pre-check
  already returns, rather than letting a race surface as an unhandled 500.
- `AuthEndpoints.StaffPinLoginAsync` loads matching terminals into a list instead of
  `.SingleOrDefaultAsync()`; more than one match fails the login the same way every other rejection
  in that method does (generic 401, audited — new reason `DuplicateTerminalAssignment`).
- `OrderEndpoints.LoadAuthorizedOrderAsync`, `PaymentEndpoints.LoadAuthorizedOrderAsync`,
  `ReceiptEndpoints.LoadAuthorizedOrderAsync` each gained the identical
  `authContext.TerminalId is null || authContext.TerminalId != order.TerminalId` check, scoped to
  fire only for location-bound sessions (admin/Back-Office sessions, `LocationId: null`, are
  unaffected). `OrderEndpoints.OpenAsync` gained the matching check against the *requested*
  terminal (there being no existing order yet to compare against).

### Tests added

- `TerminalEndpointsTests`: `TerminalModel_DeviceId_HasAUniqueIndex` (EF model metadata, no DB
  round-trip); `Database_Rejects_TwoTerminalsWithTheSameDeviceId_EvenBypassingTheAppCheck` (two
  independent `DbContext`s each set a different terminal's `DeviceId` to the same value — the first
  `SaveChangesAsync()` succeeds, the second throws `DbUpdateException`, deterministically proving
  the DB constraint holds regardless of the app-level pre-check);
  `AssignDevice_ConcurrentConflictingAssignments_NeverCrash_ExactlyOneSucceeds` (two real concurrent
  HTTP calls via `Task.WhenAll`; asserts neither ever returns 500 and exactly one 200/one 409 come
  back, without asserting which layer — pre-check or DB constraint — caught the loser).
- `OrderEndpointsTests`: `GetById_Blocked_ForDifferentTerminal_SameLocation`,
  `AddLine_Blocked_ForDifferentTerminal_SameLocation`, `VoidLine_Blocked_ForDifferentTerminal_SameLocation`,
  `VoidOrder_Blocked_ForDifferentTerminal_SameLocation`, `HoldAndResume_Blocked_ForDifferentTerminal_SameLocation`,
  `Open_Rejects_WhenSessionHasNoResolvedTerminalId`,
  `Open_Rejects_WhenRequestedTerminalDiffersFromSessionsResolvedTerminal` — each proves Terminal B's
  session is rejected (404) and Terminal A's session still succeeds against its own order.
- `PaymentEndpointsTests.RecordPayment_Blocked_ForDifferentTerminal_SameLocation`,
  `ReceiptEndpointsTests.GetReceipt_And_Reprint_Blocked_ForDifferentTerminal_SameLocation` — same
  shape, for payment recording and receipt view/reprint.
- Fixed (not new tests, required by the new correct behaviour):
  `OrderEndpointsTests.Open_Succeeds_ForStaffPinSession`,
  `PaymentEndpointsTests.RecordPayment_Succeeds_ForStaffPinSession`,
  `ReceiptEndpointsTests.Reprint_Succeeds_ForStaffPinSession` each needed a
  `POST .../assign-device` call added to their setup.

### Deviations from the kickoff plan

One, discovered while writing tests, not anticipated in the kickoff notes: **the kickoff plan's
"seed two terminals with the same `DeviceId` directly via `DbContext`, bypassing the endpoint" test
strategy for both the DB-constraint test and the login-hardening test turned out to be impossible
once the unique index existed** — that's the fix working correctly, but it means:

- The DB-constraint test (`Database_Rejects_TwoTerminalsWithTheSameDeviceId_EvenBypassingTheAppCheck`)
  had to use two independent `DbContext`s racing a real commit instead of one context writing two
  rows outright (a single context's `SaveChangesAsync()` would just throw immediately on the second
  tracked entity, which is a weaker proof than two genuinely independent commits).
- **The login-hardening path (`StaffPinLoginAsync` failing cleanly on more-than-one match) has no
  integration test.** Once the unique index exists, no two `Terminal` rows can ever share a non-null
  `DeviceId` in a migrated database — there is no way to construct the precondition without
  dropping the constraint, which risked destabilising other tests running concurrently against the
  same shared Postgres instance (xUnit runs test classes in parallel by default in this repo). This
  is documented in-place as a code comment in `StaffPinLoginTests.cs` rather than silently omitted:
  the hardening remains defense-in-depth for pre-migration/legacy data, proven correct by the same
  "ambiguous match → generic 401, audited" pattern this file's `DeviceRegistrationTests` already
  established for a structurally analogous case, not by a live duplicate-row fixture.

### Verification

- `dotnet test DaxaPos.sln` — **1152/1152 passing** (144 unit + 78 Web + 930 API — up from
  Milestone C.1's 1140; 12 new API tests, no Web changes this milestone). No regressions.
- Fixed, not a regression: `OrderEndpointsTests.Open_Succeeds_ForStaffPinSession`,
  `PaymentEndpointsTests.RecordPayment_Succeeds_ForStaffPinSession`,
  `ReceiptEndpointsTests.Reprint_Succeeds_ForStaffPinSession` each needed a
  `POST .../assign-device` call added to their setup — see the plan doc's kickoff decision for why
  this is a fix, not a weakening.
- Live-verified against the (already-rebuilt, from Milestone C.1) local demo stack via `curl`: see
  the plan doc's closeout section for the exact sequence.
- No browser-automation tool was available this session (same constraint as every PLAN-0006
  session so far); no Web/UI code changed this milestone, so no bUnit changes were needed either.

### Milestone D is now unblocked

The two ownership gaps that made payments/receipts unsafe to build on top of are closed. Start
PLAN-0006 Milestone D: Payment and receipt flow in PWA.

## Recommended Next Session (post-Milestone-C.2)

Start PLAN-0006 Milestone D: Payment and receipt flow in PWA — both the `TerminalId` gap (closed
in C.1) and the terminal-scoped-authorization gap (closed in C.2) that would otherwise have let
the wrong terminal session pay or receipt another terminal's order are now closed.

Do not:

- Start MAUI.
- Start customer display (Milestone E).
- Start KDS (Milestone F).
- Implement OI-0018.
- Start PLAN-0009.
- Add migrations unless a direct blocker is found.

## Milestone D Kickoff Notes (before implementation, 2026-07-07)

See the plan doc's "Milestone D kickoff decision" for the full reasoning. Summary below.

### Confirmed current-state facts (read, not assumed)

- `PaymentEndpoints.cs`/`ReceiptEndpoints.cs` — full files read directly. No backend change needed;
  every route Milestone D consumes already exists and already carries the C.2 terminal-scoped
  `LoadAuthorizedOrderAsync` gate.
- `RecordPaymentRequest(PaymentMethod Method, decimal AmountRequested, Guid IdempotencyKey, string? ProviderReference = null, Guid? TenantId = null)`
  and `PaymentResponse(Guid Id, Guid OrderId, Guid LocationId, PaymentMethod Method, PaymentStatus Status, decimal AmountRequested, decimal? AmountApproved, Guid IdempotencyKey, Guid? TakenByUserId, Guid? TakenByStaffMemberId, DateTimeOffset RecordedAtUtc, string? ProviderReference)`
  confirmed field-for-field from the endpoint file and `PaymentEndpointsTests.cs`.
- `ReceiptResponse` confirmed field-for-field from `ReceiptEndpoints.cs` and
  `ReceiptEndpointsTests.cs` — `Lines`, `SubtotalAmount`, `TotalLabel`, `GrandTotalAmount`,
  `TaxSummary`, `TaxInclusiveSummaryLabel`, `TotalTaxAmount`, `MarkerLegend`, `Payments`, `Refunds`.
  Note `ReceiptPaymentResponse.Method` is a `string` (`ToString()`), unlike `PaymentResponse.Method`
  which is the typed enum — the two client DTOs must not share a `Method` type.
- `Order`/`OrderResponse` has no balance/amount-due field. `OrderStatus` is
  `Open = 0, Held = 1, Completed = 2, Voided = 3, Cancelled = 4`. Completion happens only as a side
  effect of `PaymentEndpoints.RecordAsync` reaching exact settlement — there is no separate
  "complete order" endpoint.
- `PaymentMethod` enum in code is `Cash = 0, ManualEftpos = 1, Integrated = 2` (`Integrated` 400s,
  no adapter yet — PLAN-0009 scope). `PaymentStatus` enum is
  `Created = 0, Approved = 1, Declined = 2, Cancelled = 3, TimedOut = 4, Recorded = 5`.
- `Sales.razor` (current, post-C.2): `_order` (type `OrderResult`) is the only order-related state;
  no separate `OrderId` field. `ClearOrder()` is the reference pattern for a busy-guarded
  API-call-then-clear-store handler. The "Clear order" button (and where "Pay" will sit next to it)
  is only rendered `@if (_order is { Lines.Count: > 0 })`.
- `DraftOrderStore`/`IDraftOrderStore` — device-scoped localStorage key `daxa.sales-draft.v1.{deviceId}`,
  `Guid?` round-tripped via `Guid.TryParse`. No changes needed to this class itself.
- `Contracts.cs` has no `Payment*`/`Receipt*` client DTOs today (grepped, confirmed empty). Existing
  order DTOs (`OrderResult`, `OrderLineResult`, `CreateOrderRequest`, `AddOrderLineRequest`,
  `OrderStatusResult`) are ordinal-matching mirrors with no `JsonStringEnumConverter` configured —
  new `PaymentMethodResult`/`PaymentStatusResult` mirrors must preserve the same ordinals.
- `DaxaApiClient.cs`'s implicit-auth `GetAsync<T>`/`PostAsync<TReq,TRes>`/`PostNoBodyAsync<T>`/
  `DeleteAsync<T>` private helpers are the exact pattern new payment/receipt client methods reuse —
  no explicit-bearer (`*Authorized`) helpers, since this is Terminal-shell, not Back Office.

### Files likely to change

- `src/DaxaPos.Web/Api/Contracts.cs` — add `PaymentMethodResult`, `PaymentStatusResult`,
  `RecordPaymentRequest`, `PaymentResult`, `ReceiptLineResult`, `ReceiptTaxSummaryResult`,
  `ReceiptPaymentResult`, `ReceiptRefundResult`, `ReceiptResult`.
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — add `RecordPaymentAsync`, `GetPaymentsAsync`,
  `GetReceiptAsync`, `ReprintReceiptAsync` (implicit-auth pattern, matching the order methods).
- `src/DaxaPos.Web/Pages/Sales.razor` — add a "Pay" button next to "Clear order"; inject
  `NavigationManager` (not currently injected in this page).
- `src/DaxaPos.Web/Pages/Pay.razor` — new, `/sales/pay/{OrderId:guid}`, paying/paid-receipt states as
  described in the plan doc.
- `tests/DaxaPos.Web.Tests/Api/DaxaApiClientTests.cs` — extend with the 4 new methods.
- `tests/DaxaPos.Web.Tests/Pages/PayTests.cs` — new.
- `tests/DaxaPos.Web.Tests/Pages/SalesTests.cs` — extend with the "Pay" button case.

### Existing endpoints reused (no new endpoints, no backend changes)

`GET /api/v1/orders/{orderId}`, `POST /api/v1/orders/{orderId}/payments`,
`GET /api/v1/orders/{orderId}/payments`, `GET /api/v1/orders/{orderId}/receipt`,
`POST /api/v1/orders/{orderId}/receipt/reprint`.

### Tests expected

See the plan doc's kickoff decision "Tests expected" paragraph for the full list.

### Explicitly out of scope for Milestone D

Stripe/Square/Tap to Pay/integrated card, provider/device pairing, refund UI, local/USB printer
integration, customer display, KDS, any Back Office expansion, any backend/schema change,
retry-with-same-idempotency-key UI.

### Browser automation

No browser-automation tool has been available in any PLAN-0006 session so far. Continuing the
established fallback: bUnit component tests for rendering/event behaviour, `curl` against the
already-running local demo stack for live backend-contract verification (no backend changes this
milestone, so the API container does not need rebuilding — the `web` container does, to serve the
new Blazor code, and per the "docker demo stack" feedback memory this requires asking the human
first), and explicit manual click-through steps recorded at closeout.

## Milestone D Implementation Report

Completed 2026-07-07, directly on `main`, test-driven throughout (each behaviour's test written
first, watched fail for the expected reason, then implemented minimally).

### What was built

No deviations from the kickoff notes above. All new/changed code lives in `src/DaxaPos.Web` and
`tests/DaxaPos.Web.Tests` — no backend project, schema, or migration changed (confirmed via
`git diff --stat`).

- `src/DaxaPos.Web/Api/Contracts.cs` — added `PaymentMethodResult`, `PaymentStatusResult`
  (ordinal-matching mirrors of the server enums, same convention as `OrderStatusResult`),
  `RecordPaymentRequest` (TenantId omitted entirely — never supplied by the client),
  `PaymentResult`, `ReceiptLineResult`, `ReceiptTaxSummaryResult`, `ReceiptPaymentResult` (note:
  `Method` is a plain `string` here, matching the server's `payment.Method.ToString()`, not the
  `PaymentMethodResult` enum used by `PaymentResult`), `ReceiptRefundResult`, `ReceiptResult` — all
  field-for-field mirrors confirmed by reading `PaymentEndpoints.cs`/`ReceiptEndpoints.cs` and their
  tests directly.
- `src/DaxaPos.Web/Api/DaxaApiClient.cs` — added `RecordPaymentAsync`, `GetPaymentsAsync`,
  `GetReceiptAsync`, `ReprintReceiptAsync`, all one-liners via the existing implicit-auth
  `PostAsync`/`GetAsync`/`PostNoBodyAsync` private helpers — identical pattern to the order methods,
  no new plumbing.
- `src/DaxaPos.Web/Pages/Pay.razor` — new, `/sales/pay/{OrderId:guid}`, default layout (`MainLayout`,
  no `@layout` override, same posture as `Sales.razor`/`Home.razor`). Two states:
  - **Paying** (`Open`/`Held`): a balance-due banner (`order.GrandTotalAmount` minus the sum of
    `Recorded` payments' `AmountApproved`, computed client-side from two already-server-computed
    numbers — not a tax/pricing recalculation), a payment-history list, and Cash/Manual-EFTPOS
    amount inputs (prefilled to the current balance due) each with their own "Record ... payment"
    button. Each submit generates a fresh `Guid.NewGuid()` idempotency key, calls
    `RecordPaymentAsync`, then re-fetches the order and payment list; if the order is now
    `Completed`, transitions to the receipt state, otherwise refreshes the prefilled amounts to the
    new balance due.
  - **Paid/receipt** (`Completed`, reached either immediately after settlement or by loading the URL
    for an already-completed order): renders the `ReceiptResult` verbatim (lines with tax marker,
    totals, tax summary, marker legend, payments, refunds — no client-side math beyond what's already
    in the DTO), a "Reprint receipt" button, and a "New sale" link back to `/sales`.
  - A `Voided`/`Cancelled` order or a 401/403/404 on the initial `GetOrderAsync` shows a plain
    "This order is no longer payable." / "This order could not be found." message with a link back
    to `/sales` — not a crash, not a payment form.
  - On reaching the `Completed` state, calls `IDraftOrderStore.ClearAsync(device.DeviceId)`
    (belt-and-braces alongside `Sales.razor`'s existing on-mount defensive clear).
- `src/DaxaPos.Web/Pages/Sales.razor` — added `@inject NavigationManager Navigation`; a "Pay" button
  next to "Clear order" (same `_order is { Lines.Count: > 0 }` guard) calling a new `GoToPay()`
  method that navigates to `/sales/pay/{_order.Id}`. (Note: an inline lambda with a `$"..."`
  interpolated string directly inside the `@onclick` attribute hit a Razor parser error — `RZ1030`/
  `CS1056` — because of the nested quotes; extracting to a named `GoToPay()` method fixed it. Worth
  remembering for any future inline-lambda-with-interpolated-string attribute in this codebase.)
- `tests/DaxaPos.Web.Tests/Fakes/FakeOrderBackend.cs` — extended with a `Payments` list and
  `ReprintCount`, plus `Respond` branches for `POST/GET .../payments`, `GET .../receipt`, and
  `POST .../receipt/reprint`, mirroring the real `PaymentSettlement` rule (rejects a payment that
  would push the running `Recorded` total over `GrandTotalAmount`; flips `Order.Status` to
  `Completed` on exact settlement) so `PayTests.cs` exercises the same state machine the real API
  enforces.
- Tests: `tests/DaxaPos.Web.Tests/Api/DaxaApiClientTests.cs` — 7 new cases (success + one
  representative failure/edge case per method: overpayment rejection, list, not-found, reprint
  success/forbidden). `tests/DaxaPos.Web.Tests/Pages/PayTests.cs` — new, 9 bUnit tests: balance-due
  calculation and prefill, full cash payment reaching the receipt state and clearing the draft,
  partial cash payment staying in the paying state with an updated balance and history row, manual
  EFTPOS payment, server-rejected overpayment shown as an inline error, reprint success, a
  `Completed`-status initial load going straight to the receipt state (no payment form), a `Voided`
  order showing "no longer payable" (no payment form), a nonexistent order showing "could not be
  found". `tests/DaxaPos.Web.Tests/Pages/SalesTests.cs` — one new case: the "Pay" button is absent
  with an empty cart and navigates to `/sales/pay/{id}` once the order has lines.

### Deviations from the kickoff plan

None.

### Verification

- `dotnet test DaxaPos.sln` — **1169/1169 passing** (144 unit + 95 Web + 930 API — up from Milestone
  C.2's 1152; 17 new Web tests, 0 new/changed API tests since no backend code changed anywhere in
  this milestone). No regressions.
- No EF Core migrations added or needed; confirmed via `git diff --stat` that only
  `src/DaxaPos.Web`, `tests/DaxaPos.Web.Tests`, and these two docs changed.
- No browser-automation tool was available this session (same constraint as every PLAN-0006 session
  so far). bUnit component tests exercise real Blazor rendering/event handling for both of `Pay`'s
  states and `Sales`'s new button; `FakeOrderBackend`'s extended settlement/receipt simulation proves
  the UI wiring against the same state machine the real `PaymentSettlement`/`ReceiptRenderer` enforce,
  but no live `curl` walkthrough against the real API and no rebuilt `pos-platform-web-1` container
  were exercised this session specifically — flagged as a next step, not done unprompted, since
  rebuilding/restarting the `web` container needs the human's confirmation first (see
  "Docker demo stack rebuild" feedback memory).

### Remaining gaps / follow-ups

- The payment-record failure path shows one generic message for every non-401/403 failure rather
  than surfacing the server's actual plain-string error text (overpayment vs. wrong-order-state are
  both worded the same in the UI today). Flagged in the plan doc's closeout, not fixed silently — a
  future milestone could read `ApiResult<T>.Error` directly or add a structured error response
  server-side.
- No retry-with-same-idempotency-key UI — a dropped response after the server already committed a
  payment could lead to a second real payment on manual retry. Documented in the kickoff decision as
  an accepted simplification for this milestone.
- Hold/Resume UI remains unwired (carried over from Milestone C.1, unaffected by this milestone).
- Live/browser verification against the rebuilt demo stack has not been performed for this milestone
  specifically — see the plan doc's closeout for the exact next steps once the human confirms a
  `web` container rebuild.

## Recommended Next Session (post-Milestone-D)

Start PLAN-0006 Milestone E: Customer display / display mode in PWA — or, if the human wants
Milestone D's live/browser verification closed out first, rebuild/restart the `web` container (after
confirming with the human) and run through the "Manual UI Test Instructions (Milestone D addendum)"
steps in the plan doc.

Do not:

- Start MAUI.
- Start KDS (Milestone F).
- Implement OI-0018.
- Start PLAN-0009.
- Add migrations unless a direct blocker is found.

## Milestone E Kickoff Notes (before implementation, 2026-07-07)

See the plan doc's "Milestone E kickoff decision" for the full reasoning. Summary below.

### Confirmed current-state facts (read, not assumed)

- `DaxaApiClient.GetOrderAsync`, `GetPaymentsAsync`, `GetReceiptAsync` already exist from Milestone D
  — all implicit-auth, all hit endpoints already terminal-scoped by Milestone C.2's
  `LoadAuthorizedOrderAsync` fix. No new client DTOs needed (`OrderResult`, `PaymentResult`,
  `ReceiptResult` and friends already cover everything a display needs).
- `IDraftOrderStore`/`DraftOrderStore` (Milestone C.1) is a thin pass-through over
  `IBrowserStorage.GetItemAsync`/`SetItemAsync`/`RemoveItemAsync` — not `JsonLocalStore<T>`, so it
  has no in-memory cache and no `Changed` event. Each read genuinely re-reads localStorage, which is
  what makes cross-tab polling work: two browser tabs of the same WASM app are two separate DI
  containers/app instances (a singleton in one tab's container is invisible to another tab's), but
  both tabs' `IBrowserStorage` implementations read/write the same underlying browser-level
  `localStorage`, which is genuinely shared storage independent of either tab's JS heap.
- `Pay.razor`'s `EnterReceiptStateAsync` calls `IDraftOrderStore.ClearAsync(device.DeviceId)` the
  moment `order.Status == Completed` — confirmed by re-reading `Pay.razor` directly. This is why the
  display cannot rely solely on the store's current pointer to keep showing a receipt.
- `MainLayout.razor`'s `EnforceRouteGuard` redirects to `/login` for any route it wraps without a
  live device+session — confirmed by reading the method directly. A customer-facing page must not
  use this layout unmodified (redirecting a customer screen into the staff PIN login form would be
  wrong), so `Display.razor` needs its own layout, and deliberately does not reimplement the
  redirect-to-login guard — the server's own 401/403/404 responses are the actual authorization
  boundary, matching CLAUDE.md's "UI permission checks are only UX hints."
- `DeviceContext` is a device-scoped, not staff-scoped, record — already available without any staff
  session (Milestone A). `SessionState` is the staff-PIN session; the display needs no explicit
  read of it at all, since an absent/expired session simply makes every polled call fail
  401/403/404, which the display already treats as "nothing to show" (idle).

### Files likely to change

- `src/DaxaPos.Web/Pages/Display.razor` — new, `/display`, `@layout DisplayLayout`.
- `src/DaxaPos.Web/Layout/DisplayLayout.razor`, `DisplayLayout.razor.css` — new, minimal full-screen
  layout with no staff sidebar/nav, visually distinct from `MainLayout`.
- `src/DaxaPos.Web/Layout/NavMenu.razor` — one added link (`target="_blank"`) to open `/display`.
- `tests/DaxaPos.Web.Tests/Pages/DisplayTests.cs` — new, bUnit, reusing `FakeOrderBackend`/
  `InMemoryBrowserStorage` from Milestone D's test infrastructure.

### Existing endpoints reused (no new endpoints, no backend changes)

`GET /api/v1/orders/{orderId}`, `GET /api/v1/orders/{orderId}/payments`,
`GET /api/v1/orders/{orderId}/receipt`.

### Tests expected

No device registered → idle; device registered but no tracked order → idle; an `Open` order with
lines → grouped line items and server-computed totals render; a partial payment → balance-due
reflects the remainder and a payment-history entry is visible; a `Completed` order → receipt view
(reusing the same fields `Pay.razor` renders); draft cleared right after completion (simulating
`Pay.razor`'s belt-and-braces clear) → the receipt keeps showing rather than reverting to idle; an
order voided and its draft cleared (simulating `Sales.razor`'s `ClearOrder`) → resets to idle; a new
order starting while an old receipt is still showing → switches to the new order's building state;
an order id the backend 404s on (simulating a different terminal's order) → degrades to idle, not an
error state.

### Explicitly out of scope for Milestone E

MAUI second window, customer input, loyalty/tip prompts, KDS, printing, any Back Office expansion,
any backend/schema change.

### Browser automation

No browser-automation tool has been available in any PLAN-0006 session so far. Continuing the
established fallback: bUnit component tests for rendering/event/timer-driven behaviour, `curl`
against the already-running local demo stack for live backend-contract verification (no backend
changes this milestone, so only the `web` container would need rebuilding to serve this code — asked
for, not done unprompted, per the "docker demo stack" feedback memory), and explicit manual
click-through steps recorded at closeout.

## Milestone E Implementation Report

Completed 2026-07-07, directly on `main`, test-driven (the full `DisplayTests.cs` suite was written
and watched to fail on a missing `Display` type before `Display.razor`/`DisplayLayout.razor` existed,
then implemented to green in one pass with no further test changes needed).

### What was built

No deviations from the kickoff notes above. All new/changed code lives in `src/DaxaPos.Web` and
`tests/DaxaPos.Web.Tests` — no backend project, schema, or migration changed (confirmed via
`git diff --stat`, and cross-checked that only `Display.razor`, `DisplayLayout.razor`/`.css`,
`DisplayTests.cs`, and one `NavMenu.razor` link are new/changed for this milestone specifically —
the other pending `git status` entries, e.g. `Contracts.cs`/`DaxaApiClient.cs`/`Sales.razor`, were
already uncommitted Milestone D changes present at the start of this session, not touched again
here).

- `src/DaxaPos.Web/Pages/Display.razor` — new, `/display`, `@layout DisplayLayout`. Polls
  `IDraftOrderStore.GetOrderIdAsync(device.DeviceId)` (the same key `Sales.razor`/`Pay.razor` use) on
  a cancellable loop (`[Parameter] TimeSpan PollInterval`, default 4s, overridden to 20ms in tests).
  Keeps its own in-memory `_trackedOrderId`/`_order`/`_payments`/`_receipt` state, independent of the
  store: a new/different stored order id always switches to it immediately; the stored pointer
  disappearing triggers a **fresh** re-fetch of the last-tracked order (not a trust of the possibly-
  stale in-memory status) before deciding whether to keep showing a completed receipt (sticky) or
  reset to idle (voided/cleared-without-paying). Any non-success API result (401/403/404) resets to
  idle rather than showing an error. Three render states: idle ("Daxa POS" / "Welcome — please wait
  to be served"), order-building/payment (grouped-by-product-name line items, server total,
  balance-due once any payment is recorded), completed/receipt (renders `ReceiptResult` verbatim,
  "Payment approved — Thank you!").
- `src/DaxaPos.Web/Layout/DisplayLayout.razor`, `DisplayLayout.razor.css` — new, minimal full-screen
  dark layout with no staff sidebar/nav/log-out chrome, deliberately not wrapping `Display.razor` in
  `MainLayout`'s device/session redirect-to-login guard (a customer-facing screen should not be
  bounced into the staff PIN login form; the same 401/403/404-degrades-to-idle handling in
  `Display.razor` is the actual authorization backstop, matching CLAUDE.md's "UI permission checks
  are only UX hints — security is enforced server-side").
- `src/DaxaPos.Web/Layout/NavMenu.razor` — one added link, `target="_blank" rel="noopener"`, opening
  `/display` as a second browser tab/window. A plain `<a>`, not `<NavLink>`, since `NavLink`'s
  active-route highlighting doesn't apply to a link that's meant to open a separate tab.
- Tests: `tests/DaxaPos.Web.Tests/Pages/DisplayTests.cs` — new, 9 bUnit tests reusing Milestone D's
  `FakeOrderBackend`/`InMemoryBrowserStorage` fakes: no device → idle; device but no tracked order →
  idle; an `Open` order with lines → line items and server total render; a partial payment → balance
  due reflects the remainder; a `Completed` order → receipt with "Thank you"; the draft pointer
  cleared right after completion (simulating `Pay.razor`'s exact sequence) → the receipt keeps
  showing rather than reverting to idle; an order voided and its draft cleared (simulating
  `Sales.razor`'s `ClearOrder`) → resets to idle; a new order starting while an old receipt is still
  showing → switches to the new order and the old "Thank you" text disappears; a stored order id the
  backend 404s on (simulating a different terminal's order via C.2's authorization) → degrades to
  idle, not an error.

### Deviations from the kickoff plan

None.

### Verification

- `dotnet test DaxaPos.sln` — **1178/1178 passing** (144 unit + 104 Web + 930 API — up from
  Milestone D's 1169; 9 new Web tests, 0 new/changed API tests since no backend code changed
  anywhere in this milestone). No regressions.
- No EF Core migrations added or needed; confirmed via `git status`/`git diff --stat` that only
  `src/DaxaPos.Web`, `tests/DaxaPos.Web.Tests`, and these two docs changed for this milestone.
- No browser-automation tool was available this session (same constraint as every PLAN-0006 session
  so far). bUnit component tests exercise real Blazor rendering, timer-driven polling (via the short
  `PollInterval` test seam), and event handling for all three display states and the
  sticky-completed/reset-to-idle/new-order-switch transitions; the `pos-platform-web-1` container has
  not been rebuilt to serve this code — flagged as a next step, not done unprompted, per the "docker
  demo stack" feedback memory.

### Remaining gaps / follow-ups

- Line-item grouping on the display merges by product name only, not product+modifier combination
  like `Sales.razor`'s cart — see the plan doc's closeout for the reasoning (deliberate
  simplification for a read-only overview, not a correctness bug — totals are always server-computed
  regardless of how rows are grouped).
- Live/browser verification against the rebuilt demo stack has not been performed for this milestone
  specifically — same as Milestone D, needs the human's confirmation to rebuild/restart the `web`
  container first.
- No loyalty/tip prompts, discount/surcharge line items, or receipt-options (QR/email/SMS) UI — all
  explicitly out of scope per the task's hard rules and the client `OrderResult`/`ReceiptResult` DTOs
  not carrying that data yet in any case.

## Recommended Next Session (post-Milestone-E)

Start PLAN-0006 Milestone F: Minimal KDS PWA board — or, if the human wants Milestone D/E's
live/browser verification closed out first, rebuild/restart the `web` container (after confirming
with the human) and run through both milestones' "Manual UI Test Instructions" addenda in the plan
doc.

Do not:

- Start MAUI.
- Implement OI-0018.
- Start PLAN-0009.
- Add migrations unless a direct blocker is found.
