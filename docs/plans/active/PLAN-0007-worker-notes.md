# PLAN-0007 Worker Notes

## Status

**Milestone A (Reconnect And Read Resilience) is implemented and complete** (2026-07-08). See the
Milestone A Implementation Report below. Milestones B–D remain outline-only placeholders, not
started. See the plan doc (`docs/plans/active/PLAN-0007-sync-local-hybrid-planning.md`) for the
authoritative scope.

## Purpose Of This Revision

The original PLAN-0007 (written 2026-06-29, during the initial documentation-skeleton session,
before any product code existed) described a full Daxa Local Server + Daxa Sync engine: a second
deployable with its own PostgreSQL instance, a `DaxaPos.Modules.Sync` project, local-to-cloud/
cloud-to-local sync workers, and a `SyncConflict` review model. A 2026-07-08 planning review (a
prior session in this conversation) found that draft stale against the now-complete PLAN-0006 PWA
work and the actual repository structure, and reported findings back to the human for direction.
This revision applies the human's confirmed direction from that report.

## Findings From The 2026-07-08 Planning Review

- `src/` contains exactly one API (`DaxaPos.Api`), one PostgreSQL database, and one Blazor
  WebAssembly PWA (`DaxaPos.Web`). No local-server project, no `DaxaPos.Modules.Sync`, and no sync
  worker exist anywhere in the solution — confirmed by directory listing, not assumed.
- OI-0006 (Hybrid Sync Conflict Rules), which the original PLAN-0007 draft listed as a blocking
  dependency ("Resolve OI-0006... or create proposed ADR"), was closed on 2026-07-01 with a
  category-based conflict model folded into ADR-0007's Acceptance Addendum. The original draft
  predates that closure and still describes it as open.
- PLAN-0006 (complete as of 2026-07-07, Milestone G) built the PWA's actual client-side state layer
  — `DeviceContextStore`, `SessionState`, `BackOfficeSessionState`, `IDraftOrderStore` (all
  `localStorage`-backed) — and an established poll-loop precedent (`Display.razor`'s
  `[Parameter] TimeSpan PollInterval` pattern, reused by `Kds.razor`). None of this existed when
  the original PLAN-0007 draft was written, so it does not reference any of it.
- Docker/local-dev work to date (PLAN-0010, PLAN-0011, PLAN-0012) stood up one single-API stack
  (`db`, `keycloak`, `api`, `worker`, `web`) for local development and demo purposes — this is not a
  second "Daxa Local Server" deployable, and none of that work implies one exists.

## Human Decisions Recorded

- **2026-07-08**: PLAN-0007 is rescoped from "build the Daxa Sync local-server engine" to
  "browser/PWA-level offline and reconnect resilience over the existing single API." The original
  local-server/sync-worker architecture is preserved as forward-looking direction in the plan
  doc's Handoff Notes, for a future, separately-numbered plan — it is explicitly not current
  PLAN-0007 scope.
- **2026-07-08**: OI-0006 is treated as closed/resolved precedent (its category-based conflict
  model), not a blocking dependency.
- **2026-07-08**: ADR-0007 remains the guiding ADR. Applying its local-vs-cloud conflict-authority
  principle to a browser-vs-API split is recorded as an extension of that existing principle, not a
  new deployable-architecture decision — no new ADR was created for this revision.
- **2026-07-08**: First implementation slice defined as **Milestone A — Reconnect And Read
  Resilience**: `Sales`/`Pay`/`Display`/`Kds` read/poll behaviour under connectivity loss, a
  consistent online/offline/reconnecting indicator, and reconnect revalidation. Explicitly no
  offline writes (no orders/payments/refunds/receipts/KDS state changes while offline), no offline
  write queue, no local server components, no migrations, no PLAN-0009/MAUI/OI-0018/printer
  routing/KDS station lifecycle.

## What Changed In This Revision

- `docs/plans/active/PLAN-0007-sync-local-hybrid-planning.md` — fully rewritten. Title changed from
  "Sync, Local, and Hybrid" to "Browser/PWA Offline and Reconnect Resilience" to match the new
  scope. Added a "Revision Note" section explaining the rescoping. Added a Milestone Breakdown
  table (A detailed, B–D outline-only). Rewrote Architecture/Domain Assumptions, Risks, Files
  Likely To Change, ADRs/Open Issues Required sections against the current real architecture.
  Moved the original local-server/sync-engine content into a "Future Plan — Daxa Local Server /
  Daxa Sync" subsection of Handoff Notes.
- `docs/plans/active/PLAN-0007-worker-notes.md` — created (this file). No prior worker-notes file
  existed for PLAN-0007.

No other files were changed. No product code, tests, or migrations were touched.

## Milestone A Implementation Report (2026-07-08)

Implemented directly on `main`, TDD throughout (each new behaviour written test-first, watched fail
for the expected reason, then implemented minimally — see individual commits).

### What was built

Confirmed by reading `DaxaApiClient.cs`/`ApiResult.cs` at kickoff (not assumed): `ApiResult` already
carried enough information to distinguish a network failure from a real HTTP response
(`StatusCode is null` vs. not), but it wasn't surfaced as a distinct `Kind`, so every page lumped a
dropped connection in with generic HTTP failures. That gap, plus `Display.razor` resetting to idle
on any failure (unlike `Kds.razor`, which already preserved its last board correctly), were the two
concrete defects this milestone fixed.

- `src/DaxaPos.Web/Api/ApiResult.cs` — `ApiResultKind` gained a distinct `NetworkFailure` case;
  both `ApiResult`/`ApiResult<T>.NetworkFailure()` factories updated. Purely additive — confirmed via
  `grep` that every existing consumer only ever checks `== Success`/`!= Success` or matches
  `Unauthorized`/`Forbidden` explicitly, so no exhaustive switch broke. Only one existing test
  (`DaxaApiClientTests.RegisterDeviceAsync_OnNetworkFailure_ReturnsFailedKind`) actually exercised a
  network failure; renamed and updated to expect the new kind. Every other `ApiResultKind.Failed`
  assertion in the test suite is a real HTTP error code (404/400), unaffected.
- `src/DaxaPos.Web/Api/ApiErrorMessages.cs` — added `ConnectionLost` message + switch case.
- `src/DaxaPos.Web/State/ConnectivityTracker.cs` (new) — `ConnectivityStatus`
  (Online/Reconnecting/Offline) + `IConnectivityTracker`/`ConnectivityTracker`. Deliberately
  in-memory only, not `localStorage`-backed like `DeviceContextStore`/`SessionState` — connectivity
  is a transient per-tab runtime signal, not app state to persist or share across tabs. A single
  network failure moves `Online → Reconnecting`; a second consecutive one escalates to `Offline`, so
  one transient blip doesn't immediately read as a full outage.
- `src/DaxaPos.Web/Api/ConnectivityHandler.cs` (new) — a `DelegatingHandler`, same idiom as the
  existing `AuthHeaderHandler`, added after it in the `AddHttpMessageHandler` chain (`Program.cs`)
  so it sits directly around the real transport call. Reports to the tracker automatically for
  every API call (any HTTP response, even 401/403/404/500, means "online"; only a transport-level
  `HttpRequestException` means "network failure"), then rethrows so `DaxaApiClient`'s own catch
  blocks are unaffected. This design means `DaxaApiClient`'s public constructor/signature never
  changed, so none of the ~15 existing places across the test suite that construct it directly
  needed to change.
- `src/DaxaPos.Web/Shared/ConnectivityBanner.razor` (new) — resolves `IConnectivityTracker` via
  `IServiceProvider.GetService` (nullable), deliberately **not** `@inject` (which uses
  `GetRequiredService` and throws). This was the key decision that kept the change small: it means
  none of the ~40 pre-existing Sales/Pay/Display/Kds tests that don't register a tracker had to be
  touched — the banner just renders nothing for them, exactly as before. Added to `_Imports.razor`
  (`@using DaxaPos.Web.Shared`) and to all four pages' markup.
- `src/DaxaPos.Web/Pages/Display.razor` — `LoadOrderAsync` fixed: a `NetworkFailure` now returns
  early without calling `ResetToIdle()`, preserving whatever was last successfully shown; the
  existing poll loop already retries next tick with no further change needed. Genuine failures
  (404/403/etc.) still reset to idle, unchanged.
- `src/DaxaPos.Web/Pages/Kds.razor` — banner only; its failure handling already kept the last board
  and its message already flows through `ApiErrorMessages.ForLoadFailure`, which now automatically
  picks up the new `NetworkFailure` case with zero code change there.
- `src/DaxaPos.Web/Pages/Sales.razor` — `OnInitializedAsync`'s body extracted into a `LoadAsync()`
  reused by a new "Retry" button shown alongside `_loadError`; all four action-failure assignments
  (open/add-line, decrement, remove, clear-order) now route through `ApiErrorMessages.ForLoadFailure`
  instead of a hardcoded generic string.
- `src/DaxaPos.Web/Pages/Pay.razor` — same `LoadAsync()`/"Retry" extraction for the initial order
  load. `RecordPaymentAsync`'s failure switch gained an explicit `NetworkFailure` case — before this
  fix, a dropped connection showed "This payment could not be recorded. It may exceed the amount
  owing," which is actively wrong (no payment was attempted at all).
- Tests: `tests/DaxaPos.Web.Tests/State/ConnectivityTrackerTests.cs`,
  `Api/ConnectivityHandlerTests.cs`, `Shared/ConnectivityBannerTests.cs` (all new, 14 tests) plus 14
  new page-level tests across `DisplayTests.cs` (+2), `KdsTests.cs` (+3), `SalesTests.cs` (+4),
  `PayTests.cs` (+4), each file's new tests using a separate, self-contained
  `RegisterServicesWithConnectivity` helper so the ~40 pre-existing tests' own setup helpers were
  never modified.

### Deviations from the kickoff plan

None. The connectivity-state pattern was judged justified (real duplication across four pages) and
built as a shared tracker + handler + banner, per the plan's own conditional ("only if it reduces
duplication").

### Verification

- `dotnet build DaxaPos.sln` — 0 errors.
- `dotnet test DaxaPos.sln` — **1224/1224 passing** (144 unit + 150 Web + 930 API). Baseline before
  this milestone was 1196; 28 new Web tests added, 0 regressions, 0 API/unit test changes (confirmed
  no backend file touched).
- `git diff --stat` confirmed the entire change is contained to `src/DaxaPos.Web` and
  `tests/DaxaPos.Web.Tests` — no migrations, no backend endpoints, no schema.
- This session resumed after a connection interruption mid-implementation. Before continuing, the
  partial state was independently re-verified from disk (`git status`, `git diff` on every touched
  file, `dotnet build`) rather than trusted from prior conversation memory — the foundational layer
  (tracker, handler, banner, `ApiResultKind` change, and their tests) was already complete and
  correct; only the four pages' wiring (banner + Display's fix + Sales/Pay retry) remained, and was
  completed test-first from there.
- No browser-automation tool was available this session (consistent with every PLAN-0006 milestone).
  bUnit component tests exercise real Blazor rendering/event-handling, including the poll-loop-driven
  reconnect paths (`Display`/`Kds`, using their existing fast-poll test seam) and the click-driven
  retry paths (`Sales`/`Pay`).

### Known simplifications, not fixed here

- The `ConnectivityHandler`/tracker only distinguishes "reached the server" from "didn't" — it does
  not attempt to classify *why* a request failed to reach the server (DNS vs. timeout vs. CORS),
  since no page needs that distinction for Milestone A's scope.
- `Pay.razor`'s "Retry" button is shown for every `_loadError`, including a genuinely voided/missing
  order (retrying won't change that) — matching `Sales.razor`'s same unconditional treatment.
  Harmless (retry just re-fetches and shows the same terminal state again) and kept deliberately
  simple rather than adding a NetworkFailure-only conditional.
- No automatic retry/polling was added to `Sales`/`Pay`'s initial load (only `Display`/`Kds` already
  had poll loops to reuse) — recovery there is the manual "Retry" button. Continuous polling for
  Sales/Pay was considered and deliberately not built: it's not what either page's architecture
  already does, and inventing it now would be new behaviour beyond "preserve/extend what's already
  intended," which the plan doc's Domain Assumptions section flagged as the boundary to respect.

## Recommended Next Session

Milestone A is done. Any further PLAN-0007 work requires its own kickoff-decision pass:

- Milestone B (offline-safe local drafts / bounded write queue) is the natural next candidate, but
  needs a kickoff note in the plan doc first — in particular, how long a queued write may live, and
  what happens if the underlying order was voided elsewhere while queued.
- Do not start Milestone B, C, or D implementation without that kickoff note and explicit approval.
- Do not start the future Daxa Local Server / Daxa Sync plan — it is not numbered or scheduled yet.
- Do not start PLAN-0009, MAUI, OI-0018, printer routing, or KDS station lifecycle work.
