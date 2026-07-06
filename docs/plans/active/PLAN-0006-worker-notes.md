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
