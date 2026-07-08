# PLAN-0007 Worker Notes

## Status

**Milestone A (Reconnect And Read Resilience) is implemented and complete** (2026-07-08). See the
Milestone A Implementation Report below. **Milestone B (Offline-Safe Sales Action Retry, Option 2)
is implemented and complete** (2026-07-08) — see the Milestone B Implementation Report below.
Milestones C–D remain outline-only placeholders, not started. See the plan doc
(`docs/plans/active/PLAN-0007-sync-local-hybrid-planning.md`) for the authoritative scope.

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

## Milestone B Kickoff Report (2026-07-08)

Verification-only session, no product code touched. Confirmed the repository state matches the
plan doc's claims (`git status` clean on `main` at `ba777e9`; `dotnet test DaxaPos.sln` —
1224/1224 passing, 144 unit + 150 Web + 930 API) before reading `ADR-0007`,
`OI-0006` (closed), the Milestone A connectivity classes (`ConnectivityTracker`,
`ConnectivityHandler`, `ConnectivityBanner`, `ApiResultKind.NetworkFailure`), and the current
`Sales.razor`/`Pay.razor` write paths.

### Finding: no idempotency support on the two endpoints Milestone B would need to replay

The plan's Milestone B one-liner is "queuing add-line calls made while briefly disconnected,
replayed on reconnect with idempotency." Reading the actual client/API code shows this precondition
does not hold today:

- `RecordPaymentRequest` (`src/DaxaPos.Api/Endpoints/Payments/PaymentEndpoints.cs:14`) already
  carries a `Guid IdempotencyKey`, and `PaymentEndpoints.cs:107` looks up
  `dbContext.Payments.SingleOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey)` before
  insert, per ADR-0010 — a retry returns the existing payment instead of creating a duplicate.
  `Pay.razor:205` already supplies `Guid.NewGuid()` for this on every call.
- `CreateOrderRequest` and `AddOrderLineRequest`
  (`src/DaxaPos.Api/Endpoints/Orders/OrderEndpoints.cs:16,18`) carry **no** idempotency key field,
  and neither `Order` nor `OrderLine` has an equivalent lookup column. `Sales.razor`'s
  `AddLineAsync` (`Sales.razor:285-321`) calls `ApiClient.OpenOrderAsync` when `_order is null`,
  then `ApiClient.AddOrderLineAsync` — both plain `POST`s with no dedupe key. Each call to
  `AddLineAsync` (including `Increment`, which just calls it again) creates one new line at
  quantity 1; there is no natural merge/dedupe on the server side.

Consequence: a queued write that is replayed after a `NetworkFailure` (client never received a
response, but the server may have already processed the request — `ConnectivityHandler` cannot
distinguish "request never arrived" from "request arrived, response was lost in transit") has no
mechanism to detect it already landed. Replaying it would risk a duplicate order line (extra billed
item) or a duplicate open order (orphaned order, draft pointer race with `IDraftOrderStore`). This
is a real, financially meaningful correctness gap, not a hypothetical.

This matches the plan's own stop condition (Domain Assumptions / Milestone B outline): *"If the
existing API/domain model cannot safely support offline order replay, stop and document the
blocker instead of forcing the implementation."* That condition is met. This is reported as a
blocker, not worked around.

### Other open questions the plan already flagged, confirmed still open

- Queue lifetime: how long a queued write may survive (tab close, reload, extended offline period)
  — no answer in the plan doc, not decided by this session.
- Voided-elsewhere race: what happens if a queued add-line's target order was voided/completed by
  another terminal before replay — ADR-0007's "server authoritative, revalidate on reconnect"
  principle applies in spirit (same as Milestone A's `Display`/`Kds` revalidation), but no concrete
  rule exists for a *queued write* against a since-changed order.
- `IDraftOrderStore` today persists only an `OrderId` pointer (`IDraftOrderStore.cs:9-16`), not
  order content or pending actions — a write queue would be new persisted (or in-memory, per
  `ConnectivityTracker`'s deliberate choice) client state with its own design question, not an
  extension of an existing store.

### Human Decision (2026-07-08): Option 2 chosen — no idempotency work, no auto-replay

Option 2 is confirmed. Reasoning recorded verbatim from the decision:

- The current order-create/add-line endpoints have no idempotency support.
- Auto-replaying queued order writes would risk duplicate orders or duplicate lines if the request
  reached the server but the response was lost.
- Payment idempotency exists (`RecordPaymentRequest.IdempotencyKey`, ADR-0010); order-line
  idempotency does not.
- The safe first slice is manual, staff-initiated retry — not automatic offline write replay.

Milestone B is reframed accordingly. It is no longer "offline-safe local drafts / a bounded write
queue... replayed on reconnect." It is now:

### Milestone B — Offline-Safe Sales Action Retry

**Scope:**

- `Sales.razor` only.
- When `AddLineAsync` fails because of `ApiResultKind.NetworkFailure`, preserve the staff member's
  attempted add-line action (product + modifier selections) as a single pending retry — not a
  queue.
- Show a message distinct from the generic action-failure string, making clear the item was not
  confirmed by the server.
- Require an explicit staff tap on a "Retry" button to reattempt — no automatic replay triggered by
  a connectivity-state change alone.
- No persistence of the pending retry to `localStorage`; it does not survive a browser refresh (the
  component field resets on remount, with no code needed to enforce this).
- `Increment`/`Decrement`/`Remove`/`ClearOrder` are out of scope for this slice.
- If `AddLineAsync` needed to open the order first (`_order is null`) and the network failure
  happened opening the order (before any line existed), retrying re-attempts both as the one
  pending action — this is accepted as the one case retried without a landed/not-landed check, per
  the approved scope ("if the current Sales flow creates the order and adds the line in one path,
  treat that as one pending staff-confirmed retry action, but only if the UI makes clear it was not
  committed"). It is an explicit, staff-visible, staff-initiated action, not silent background
  replay — the residual "did the original request actually land" ambiguity is accepted here under
  human review rather than solved.
- Server remains authoritative for order/line state; no client-side computation of what "should"
  exist beyond the one pending UI hint.

**Non-goals (Milestone B specifically):**

- No backend idempotency-key work.
- No EF Core migrations.
- No automatic write queue or auto-replay of any kind.
- No offline payments, refunds, or receipts.
- No local server.
- No PLAN-0009, MAUI, OI-0018, printer routing, or KDS station lifecycle.

**Tests:**

- Network failure during add-line: shows the "not confirmed" message and a Retry affordance, order
  state unchanged.
- Retry (once connectivity is restored) reattempts and completes the add.
- HTTP 401/403/404 during add-line: existing rejection messages shown, no Retry affordance offered
  (not treated as retryable).
- Connectivity restoring without a Retry click does not, by itself, trigger any request.

### Proposed Narrow Milestone B Checklist (approved — Option 2)

1. Add a distinct `ApiErrorMessages` message for "item not confirmed by the server" (not the
   existing `ConnectionLost`/"we'll keep trying" wording, since Milestone B does not auto-retry).
2. `Sales.razor`: on `NetworkFailure` from either `OpenOrderAsync` or `AddOrderLineAsync` inside
   `AddLineAsync`, store the attempted `(productId, modifierIds)` as a single pending-retry field
   and show the new message plus a Retry button; any other failure kind (success, rejection) clears
   it.
3. Retry button calls `AddLineAsync` again with the stored arguments; disabled while `_isBusy`,
   matching the existing action-button convention.
4. bUnit coverage per the Tests list above.
5. No `IDraftOrderStore` schema change, no new `localStorage` key, no migration, no backend file
   changes.
6. Update `docs/modules/sync.md` and this plan's Milestone B closeout once implemented.

## Milestone B Implementation Report (2026-07-08)

Implemented directly on `main` per the approved checklist above, no deviations.

### What was built

- `src/DaxaPos.Web/Api/ApiErrorMessages.cs` — new `AddLineNotConfirmed` constant, deliberately
  worded to not imply automatic retry (unlike `ConnectionLost`'s "we'll keep trying").
- `src/DaxaPos.Web/Pages/Sales.razor` — new `_pendingRetry` field
  (`(Guid ProductId, IReadOnlyList<Guid> ModifierIds)?`, single slot, not a queue). `AddLineAsync`
  now routes both its failure branches (the implicit `OpenOrderAsync` and the `AddOrderLineAsync`
  call) through a new `SetAddLineFailure` helper: a `NetworkFailure` sets `_pendingRetry` and shows
  `AddLineNotConfirmed`; any other kind (a genuine 401/403/404/Failed rejection) uses the existing
  `ApiErrorMessages.ForLoadFailure` path and leaves `_pendingRetry` null, so a real rejection is
  never offered a Retry button. `_pendingRetry` is cleared unconditionally at the top of every
  `AddLineAsync` call (including a Retry itself), so the "latest attempt wins" — no queueing, no
  stale pending state left behind by a later, different tile tap. A new `#retry-add-line` button,
  shown only when `_pendingRetry is not null`, calls `RetryPendingAddLineAsync`, which re-invokes
  `AddLineAsync` with the stored arguments.
- Tests (`tests/DaxaPos.Web.Tests/Pages/SalesTests.cs`): renamed and rewrote
  `NetworkFailureAddingALine_ShowsConnectionLostMessage_NotAGenericRejection` (old `ConnectionLost`
  wording) to `NetworkFailureAddingALine_ShowsNotConfirmedMessage_AndOffersRetry`; added
  `RetryPendingAddLine_OnceConnectivityRestored_CompletesTheAdd`,
  `ConnectivityRestoring_WithoutRetryClick_DoesNotAutoReplayTheAdd` (calls
  `IConnectivityTracker.ReportOnline()` directly with no further HTTP call, confirming nothing in
  `Sales.razor` reacts to a connectivity-state change alone), and a `[Theory]`
  `HttpRejectionAddingALine_IsNotOfferedAsRetryable` covering 401/403/404 — each asserts the
  existing rejection message and confirms `#retry-add-line` is absent.

### Deviations from the approved checklist

None.

### Verification

- `dotnet build DaxaPos.sln` — 0 errors.
- `dotnet test DaxaPos.sln` — **1229/1229 passing** (144 unit + 155 Web + 930 API). Baseline before
  this milestone was 1224; 5 new Web tests added (1 renamed/rewritten, 4 net new — 2 facts + a
  3-case theory), 0 regressions, 0 API/unit changes.
- `git diff --stat` confirmed the entire change is contained to
  `src/DaxaPos.Web/Api/ApiErrorMessages.cs`, `src/DaxaPos.Web/Pages/Sales.razor`, and
  `tests/DaxaPos.Web.Tests/Pages/SalesTests.cs`, plus the `docs/plans/active/PLAN-0007-*.md` doc
  updates — no migrations, no other backend/frontend files, no `IDraftOrderStore`/`localStorage`
  changes.
- `npx markdownlint-cli2 "**/*.md"` — 0 errors.

### Known simplification, not fixed here

See the plan doc's Milestone B Closeout for the accepted residual risk: a Retry that re-opens an
order (when the original `NetworkFailure` happened before any order existed) could duplicate the
order if the original open-order request actually reached the server. This is accepted under
explicit staff review, not solved — matches the approved Human Decision's reasoning exactly, and is
scoped narrowly to the first-item-on-a-new-order case only.

## Milestone C Kickoff Report (2026-07-08)

Verification-only session, no product code touched. Confirmed repository state before reading:
`git status --short --branch` — clean, `main` is 1 commit ahead of `origin/main` (`584b739`,
Milestone B; `ba777e9`/Milestone A is already on `origin/main`). `dotnet test DaxaPos.sln` —
**1229/1229 passing** (144 unit + 155 Web + 930 API), matching the Milestone B closeout baseline
exactly, no drift. `npx markdownlint-cli2 "**/*.md"` — 0 errors across 167 files. Read the plan doc,
these worker notes, `ADR-0007`, `ADR-0010`, `Pay.razor`, `DaxaApiClient.cs`, `ApiResult.cs`,
`ApiErrorMessages.cs`, `ConnectivityTracker.cs`, `PaymentEndpoints.cs`, `ReceiptEndpoints.cs`, and
`PayTests.cs`. OI-0006 is closed (see Findings section above); not re-read in full, only its
resolution (already folded into ADR-0007's Acceptance Addendum) was reconfirmed as still current.

### 1. What the plan says Milestone C should cover

The Milestone Breakdown table: *"Payment/receipt behaviour policy under intermittent
connectivity... Likely requires an explicit product decision on whether any payment method may
ever be recorded while offline (cash is the obvious candidate; integrated/manual EFTPOS plausibly
should never be) — flagged as a genuine open product question, not decided here."* Note this
question sits in tension with PLAN-0007's own top-level Non-goals, which still apply to Milestone C
unless explicitly revised: *"No offline creation of orders, payments, refunds, or receipts."* This
kickoff does not attempt to resolve that tension by picking a side — it reports the two candidate
framings under Question 7/8 below and leaves the choice to the human, consistent with the session
instructions ("do not implement offline payments unless explicitly approved").

### 2. Existing payment/receipt behaviour after PLAN-0005/0006/0007 A/B

- `Pay.razor` loads the order (`LoadAsync`, Milestone A's retryable extraction) and its payments,
  then offers Cash and Manual EFTPOS recording (`RecordPaymentAsync`). `PaymentMethod.Integrated` is
  rejected server-side (`PaymentEndpoints.cs:93-96`) — no terminal adapter exists (PLAN-0009 not
  started), matching CLAUDE.md's provider-agnostic-adapter direction.
- On success, `Pay.razor` refetches the order and payments; if the order is now `Completed`, it
  calls `EnterReceiptStateAsync`, which fetches the receipt (`GetReceiptAsync`) and clears the
  device's draft-order pointer (`IDraftOrderStore.ClearAsync`).
- `ReprintAsync` calls `ReprintReceiptAsync` (server-side audited per ADR-0010/CLAUDE.md) and
  updates `_receipt` on success.
- Milestone A added: the `ConnectivityBanner`, a retryable `LoadAsync` for the initial order load,
  and a `NetworkFailure` case in `RecordPaymentAsync`'s failure switch (previously a network drop
  showed the misleading "may exceed the amount owing" text).
- No offline write path of any kind exists today for payments — `RecordPaymentAsync` is a single
  awaited POST with no queue, no persistence, no retry affordance specific to payments (unlike
  `Sales.razor`'s Milestone B `_pendingRetry`/Retry-button pattern for add-line).

### 3. Does `RecordPayment` already have idempotency support?

**Yes, server-side, and it is more capable than what Milestone B found for orders/lines** — but
**no, not safely usable across a client retry today**, which is the central finding of this report.

- `RecordPaymentRequest` (`PaymentEndpoints.cs:14`) carries a `Guid IdempotencyKey`.
  `PaymentEndpoints.RecordAsync` (`PaymentEndpoints.cs:107-116`) looks up an existing payment by
  that key **before** the order-state check and, if found, returns the existing payment (200) rather
  than creating a duplicate — checked first specifically so a retry of a payment that already
  settled and closed the order still succeeds instead of hitting a 409. This is exactly the
  precondition Milestone B's kickoff found missing for `CreateOrderRequest`/`AddOrderLineRequest`.
- **However**, `Pay.razor:205` generates the key inline on every call:
  `new RecordPaymentRequest(method, amount, Guid.NewGuid())`. Nothing stores or reuses it. If staff
  press the payment button twice for what they intend as "the same attempt" (including a
  hypothetical naive Retry button that just re-invokes `RecordPaymentAsync`), each call gets a
  **different** idempotency key, so the server-side dedupe never fires — two distinct `Payment` rows
  get created. The overpayment guard (`PaymentSettlement.WouldExceedOrderTotal`) only catches this
  by accident, when the duplicate would push the running total past `GrandTotalAmount`; a duplicate
  that fits under the total (e.g. an early split payment) would silently succeed as a second real
  payment record.
- The fix is narrow and entirely client-side: generate the `IdempotencyKey` once per logical payment
  attempt, store it in component state, and reuse the identical value on an explicit staff Retry —
  mirroring `Sales.razor`'s Milestone B pattern of storing the attempted arguments, not the call
  outcome. This requires no backend or schema change; the dedupe path already exists and is already
  tested (`PaymentEndpoints` idempotency is exercised by the 930-test API suite, not newly added
  here).

### 4. What happens today on payment-recording failure

- **Network failure**: `DaxaApiClient`'s `HttpRequestException` catch returns
  `ApiResult.NetworkFailure`; `Pay.razor` shows `ApiErrorMessages.ConnectionLost` ("...we'll keep
  trying"). Nothing actually retries — the wording is inherited from the read-path message and is
  misleading here, matching the exact problem Milestone B solved for `AddLineNotConfirmed`. Staff
  can press the button again, but per Question 3 that generates a fresh idempotency key each time —
  today, a manual re-click after a network failure is **not** provably safe.
- **401/403**: shown as a single hardcoded string, "You don't have permission to record payments."
  (`Pay.razor:210`), for both kinds. This does not distinguish session-expired (401) from
  genuinely-forbidden (403) the way `LoadAsync`'s `ApiErrorMessages.ForLoadFailure` already does on
  the same page — an inconsistency worth flagging, not necessarily worth fixing in this milestone
  (Question 8).
- **Validation/server rejection** (400 overpayment, 409 order-not-open/held): both fall into
  `ApiResultKind.Failed` and show one generic string, "This payment could not be recorded. It may
  exceed the amount owing." Reasonably accurate for the 400 case; presumptive for a 409 caused by the
  order having been completed/voided from another terminal — the message doesn't prompt staff to
  refresh and see the order's real current state, which is the ADR-0007 "revalidate against server"
  principle applied to a rejection rather than a reconnect.
- **Response lost after the server accepted the payment (ack loss)**: this is the hard case, and it
  is indistinguishable from "the request never arrived" in today's code. `DaxaApiClient` only
  branches on `HttpRequestException` vs. a received HTTP response — if the connection drops after
  the server processes the request but before the client reads the response, the client still sees
  an `HttpRequestException` → `NetworkFailure`, identical to the never-sent case. Today, staff get no
  signal that the payment might already exist; a re-click (with today's fresh-key-per-call behaviour)
  risks a real duplicate `Payment` row. This is the most financially significant gap PLAN-0007 has
  found so far — more consequential than Milestone B's order/line risk, because it is money, but also
  more tractable, because (per Question 3) the safe mechanism already exists server-side and only
  needs correct client wiring.

### 5. What happens today when receipt retrieval/reprint fails due to network loss

- `EnterReceiptStateAsync` (called right after a payment completes the order) fetches the receipt
  and **silently swallows any failure** — on `NetworkFailure` or any other non-success kind,
  `_receipt` simply stays `null`, and `DraftOrderStore.ClearAsync` still runs unconditionally
  regardless. Because `_receipt` is then `null` and `_loadError` was never set, `Pay.razor`'s render
  logic falls through to its final `else` branch — **the payment-entry UI (Cash/Manual EFTPOS
  buttons) re-renders for an order that is already `Completed` and fully paid.** This is a
  pre-existing defect, not something Milestone C introduces; it happens whenever a receipt fetch
  fails right after a payment lands, and equally on a fresh page load of an already-completed order
  if `GetReceiptAsync` has a transient network blip. Staff could be invited to attempt a second
  payment against a closed order, which the server would then reject with a 409 — surfaced through
  the generic "may exceed the amount owing" message from Question 4, not anything that says "your
  payment already went through, the receipt just didn't load." This is directly in Milestone C's
  stated theme (receipt behaviour under intermittent connectivity) and is recommended for inclusion
  in scope (Question 8), not left for a separate fix.
- `ReprintAsync` shows one generic message, "Could not reprint the receipt.", for any failure kind,
  with no distinction for `NetworkFailure`. Functionally, retrying is already safe today — the
  existing "Reprint receipt" button remains clickable and reprinting is idempotent-by-design (always
  regenerated from the immutable source record, and each reprint is deliberately audited as its own
  event per ADR-0010/CLAUDE.md, which is correct, expected behaviour, not a bug to avoid). The gap is
  purely message clarity: staff can't tell "can't reach the server, try again" from "something is
  actually wrong."

### 6. Is Milestone C safe to implement without backend/schema changes?

**Yes, for the narrow recovery-and-recheck scope** this report recommends:

- Payment retry/recheck: uses the already-existing `RecordPaymentRequest.IdempotencyKey` dedupe path
  server-side (Question 3) plus the already-existing `GetOrderAsync`/`GetPaymentsAsync` read
  endpoints for a "check status instead of retrying blind" affordance. Zero backend changes.
- Receipt retry: uses the already-existing `GetReceiptAsync`/`ReprintReceiptAsync` endpoints. Zero
  backend changes. Fixing the `EnterReceiptStateAsync` fallback defect (Question 5) is a `Pay.razor`
  render-logic fix only.

**No, for true offline payment creation** — recording a payment while the browser has no network
path to the API at all requires a local write queue and deferred replay, which is explicitly out of
this session's approved scope ("Do not implement offline payments unless explicitly approved... Do
not create a local server"). This report does not propose it. If the human wants that door opened,
it needs its own explicit decision and is a materially larger change (queue persistence, replay
ordering against `IDraftOrderStore`, and a product decision on which payment methods, if any, may
ever be attempted offline) — flagged, not designed here.

### 7. Main risks

- The idempotency-key generation site must move from "inline `Guid.NewGuid()` per call" to
  "generated once per logical attempt, reused verbatim on Retry." Getting this subtly wrong (e.g. a
  Retry path that re-derives a fresh key) silently defeats the entire safety property this milestone
  depends on — this is the single highest-attention implementation detail.
- The `EnterReceiptStateAsync` fallback-to-payment-UI defect (Question 5) pre-dates this milestone
  but overlaps its scope; leaving it unfixed means any new receipt-retry UI sits on top of an
  existing broken state transition.
- `RecordPaymentAsync`'s 401/403 handling is already inconsistent with `LoadAsync`'s
  `ForLoadFailure` pattern on the same page — in scope to align, but not explicitly requested; a
  judgment call for the checklist (Question 8).
- A generic `Failed` rejection currently doesn't distinguish "this specific payment amount is wrong"
  from "the order changed underneath you" (completed/voided elsewhere) — extending ADR-0007's
  revalidate-on-reconnect principle to cover rejections (not just `NetworkFailure`) is a bigger
  behavioural change than Milestone A/B made and needs an explicit decision, not an assumption.
- UI/state complexity: representing "not submitted / submitted-but-unconfirmed / rejected /
  recorded-and-confirmed" for payments, plus "receipt temporarily unavailable" as a distinct
  non-payment-failure state, is more states than Milestone B's single pending-retry needed to model —
  risk of over-building `Pay.razor` beyond what staff actually need to see.
- Staff may treat a "Retry" affordance as safe to press repeatedly without reading the message; a
  "Check status" action (refetch, no POST) is strictly safer than "Retry" (re-POST with the same
  key) and is recommended as a second, distinct affordance rather than folding both into one button.

### 8. Decisions needed before implementation

- Confirm Milestone C's scope is "make the existing `RecordPaymentAsync`/`GetReceiptAsync`/
  `ReprintReceiptAsync` flows resilient and honest about ambiguous outcomes, reusing existing
  payment idempotency" — **not** offline payment capability. (This matches the session's explicit
  constraints; stated here for an explicit sign-off, not assumed.)
- Confirm the idempotency-key-reuse-on-retry mechanism (Question 3/6) as the approved technical
  approach, since it is the load-bearing safety property.
- Decide: does Milestone C add a distinct "Check status" action (recheck via `GetPaymentsAsync`/
  `GetOrderAsync`, no POST) alongside "Retry" (re-POST with the stored key), or Retry only, matching
  Milestone B's exact one-button shape? Recommended: add both — "Check status" is strictly safer and
  cheap to add.
- Decide: is fixing the pre-existing `EnterReceiptStateAsync` fallback defect (Question 5) in scope
  for Milestone C, or should it land as a small separate fix first? Recommended: in scope, since it
  is literally "receipt retrieval behaviour under intermittent connectivity."
- Decide: is aligning `RecordPaymentAsync`'s 401/403 messaging with `ForLoadFailure` in scope, or
  deferred? Recommended: in scope, small and directly touches the method being modified anyway.
- Decide: does a generic rejection (`Failed`, not `NetworkFailure`) trigger a server-state
  revalidation/refresh, or is that a larger behavioural change deferred to a later milestone?
  Recommended: defer — keep Milestone C's blast radius matched to Milestone A/B's (network-failure
  and ack-loss handling only, not a general rejection-revalidation feature).
- Sign off on the message copy proposed in the checklist below.

### 9. Proposed narrow Milestone C checklist (not yet approved)

1. `ApiErrorMessages`: add `PaymentNotConfirmed` ("This payment wasn't confirmed by the server.
   Check your connection, then tap Retry or Check status.") and `ReceiptUnavailable` ("Receipt
   temporarily unavailable. Your payment was recorded — try again to view or print it."). Neither
   reuses `ConnectionLost` (implies auto-retry) or the existing overpayment string.
2. `Pay.razor`: replace the inline `Guid.NewGuid()` in `RecordPaymentAsync` with a per-attempt field
   (e.g. `_pendingPayment` holding `(PaymentMethodResult Method, decimal Amount, Guid
   IdempotencyKey)?`), generated fresh only when a payment button (Cash/EFTPOS) is first pressed —
   never regenerated on Retry.
3. On `ApiResultKind.NetworkFailure` from `RecordPaymentAsync`, keep `_pendingPayment` set, show
   `PaymentNotConfirmed`, and offer two actions: "Retry" (re-invokes `RecordPaymentAsync` with the
   same stored `Method`/`Amount`/`IdempotencyKey`) and "Check status" (calls `GetPaymentsAsync`/
   `GetOrderAsync` only, no POST, to detect whether the payment already landed).
4. On any other outcome (success, 401/403/`Failed` rejection), clear `_pendingPayment` — mirrors
   `Sales.razor`'s `SetAddLineFailure`: a genuine rejection is never offered Retry.
5. Fix `EnterReceiptStateAsync`: on receipt-fetch failure for a `Completed` order, show a distinct
   "payment recorded, receipt temporarily unavailable" state with its own Retry
   (re-calls `GetReceiptAsync`) instead of falling through to the payment-entry UI.
6. `ReprintAsync`: route `NetworkFailure` through `ReceiptUnavailable`, distinct from a genuine
   rejection message.
7. Align `RecordPaymentAsync`'s 401/403 branch with `ApiErrorMessages.ForLoadFailure`
   (`SessionExpired`/`Forbidden`), matching `LoadAsync` on the same page.
8. bUnit coverage: network failure recording payment shows the unconfirmed state with Retry and
   Check-status affordances, order/payment state unchanged; Retry with the stored key succeeds once
   connectivity returns and creates exactly one payment row (assert via the fake backend, not just
   the resulting UI); Check status detects an already-recorded payment (simulating ack loss — backend
   already holds a payment under that key) and transitions to the confirmed/receipt state without
   issuing a second POST; 401/403/`Failed` rejections are never offered Retry; a receipt-fetch
   failure after a completed order shows the new unavailable-state, not payment-entry buttons; Reprint
   network failure vs. rejection show distinct messages.
9. No `IDraftOrderStore` schema change, no new `localStorage` key (pending-payment state stays
   in-memory/component-scoped, matching Milestone A/B), no migration, no backend file changes.
10. Update `docs/modules/sync.md`/payments module doc and this plan's Milestone C closeout once
    implemented.

**Not proposed, and out of scope unless separately approved:** offline payment creation of any kind,
a persisted write queue, auto-replay triggered by connectivity change alone, offline refunds, and
any change to `RecordPaymentRequest`/`PaymentEndpoints` (server-side idempotency already covers this
milestone's needs).

## Human Decision (2026-07-08): Milestone C approved — narrow safe scope

All six Question 8 decisions were confirmed as recommended, with the checklist's message-copy
proposals accepted:

1. **Scope**: `Pay.razor` payment/receipt resilience only. No offline payments, no automatic
   background payment replay, no refunds, no backend/schema/migration work.
2. **Idempotency reuse**: the existing `RecordPaymentRequest.IdempotencyKey` is reused verbatim for
   an explicit staff Retry of a `NetworkFailure` attempt. A new key is generated only for a
   genuinely new attempt, after the previous one is resolved/cleared.
3. **"Check status"**: added as a distinct, non-POST affordance alongside Retry — re-fetches order,
   payments, and receipt state via existing read endpoints; must not assume failure just because the
   response was lost.
4. **Receipt fallback bug**: in scope. `EnterReceiptStateAsync` must no longer silently swallow a
   receipt-fetch failure and fall through to the payment-entry UI for an already-`Completed` order.
5. **401/403 alignment**: `RecordPaymentAsync` aligned with the existing `ApiErrorMessages` pattern
   (`SessionExpired`/`Forbidden`, matching `LoadAsync` on the same page); not treated as retryable.
6. **Rejections and revalidation**: a server rejection (`ApiResultKind.Failed`) shows the rejection
   message and triggers a revalidation refresh of order/payment state (ADR-0007's "revalidate
   against the server" principle applied to a rejection, not just a reconnect) — never an automatic
   retry.

## Milestone C — Payment/Receipt Resilience

**Scope:**

- `Pay.razor` only, plus `ApiErrorMessages` (two new constants) and one test-infrastructure addition
  (`StubHttpMessageHandler.FailingPathSuffix`, needed to simulate a receipt-specific network failure
  independent of the payment POST in tests).
- On `ApiResultKind.NetworkFailure` from `RecordPaymentAsync`, preserve the attempted payment
  (method, amount, and the exact `IdempotencyKey` used) as a single pending payment — not a queue —
  and show `ApiErrorMessages.PaymentNotConfirmed`.
- Two explicit staff actions for a pending payment: **Retry** (resubmits with the same stored
  idempotency key) and **Check status** (re-fetches order/payments/receipt only, never re-POSTs;
  resolves the pending state if a payment under that key is found).
- While a payment is pending, the Cash/Manual EFTPOS buttons are disabled (both in markup and
  guarded in `RecordPaymentAsync` itself) — a new payment attempt is not offered until the pending
  one is explicitly resolved, since payments are more consequential than Sales.razor's order lines.
- `EnterReceiptStateAsync` now sets a `_receiptError` on any non-success `GetReceiptAsync` result
  instead of swallowing it; a `Completed` order with no receipt yet shows a distinct
  loading/recoverable state (with its own Retry) rather than falling through to payment-entry
  markup.
- A genuine rejection (401/403/validation/conflict) is never offered Retry/Check status; 401/403 use
  the existing `SessionExpired`/`Forbidden` messages, and a `Failed` rejection triggers a
  revalidation refresh so a since-completed/voided order is reflected immediately.

**Non-goals (Milestone C specifically):**

- No offline payment creation, no persisted/`localStorage` write queue, no automatic replay
  triggered by connectivity change alone, no refunds.
- No backend, schema, or migration changes — `RecordPaymentRequest`/`PaymentEndpoints`' existing
  idempotency check is reused as-is.
- No local server, no PLAN-0009, MAUI, OI-0018, printer routing, or KDS station lifecycle.

**Implementation / Documentation Steps:**

1. Add `ApiErrorMessages.PaymentNotConfirmed` and `ApiErrorMessages.ReceiptUnavailable`.
2. `StubHttpMessageHandler`: add `FailingPathSuffix` for path-scoped simulated network failures
   (test infrastructure only).
3. `Pay.razor`: add `_pendingPayment`/`_receiptError` fields; introduce `SubmitPaymentAsync` (shared
   by a fresh `RecordPaymentAsync` attempt and `RetryPendingPaymentAsync`), `CheckPaymentStatusAsync`,
   `RefreshOrderAndPaymentsAsync`/`ApplyCompletionStateAsync` helpers, and fix `EnterReceiptStateAsync`.
4. bUnit coverage per the kickoff report's Question 9 test list.
5. Update this plan's Milestone C closeout (below) and the Milestone Breakdown table.

### Milestone C Implementation Report (2026-07-08)

Implemented directly on `main`, TDD throughout (each test written first and watched fail for the
expected reason before implementing — see Verification).

**What was built:**

- `src/DaxaPos.Web/Api/ApiErrorMessages.cs` — `PaymentNotConfirmed` and `ReceiptUnavailable`
  constants, each documented with why they're distinct from `ConnectionLost`/the old hardcoded
  strings.
- `src/DaxaPos.Web/Pages/Pay.razor`:
  - New `_pendingPayment` field (`(PaymentMethodResult Method, decimal Amount, Guid IdempotencyKey)?`,
    single slot) and `_receiptError` field.
  - `RecordPaymentAsync` now only ever generates a fresh `Guid.NewGuid()` for a genuinely new
    attempt, and refuses to start one while `_pendingPayment` is set (defence in depth alongside the
    markup's `disabled` binding on `#record-cash`/`#record-eftpos`).
  - New `SubmitPaymentAsync(method, amount, idempotencyKey)` — the single place that calls
    `RecordPaymentAsync` on the API client — used by both a fresh attempt and
    `RetryPendingPaymentAsync` (which reuses `_pendingPayment.Value.IdempotencyKey` verbatim). On
    `NetworkFailure` it sets `_pendingPayment`/`PaymentNotConfirmed`; on any other non-success outcome
    it clears `_pendingPayment` (never retryable) and, specifically for `ApiResultKind.Failed`,
    revalidates order/payment state before returning control to the staff member.
  - New `CheckPaymentStatusAsync` — refreshes order/payments (and receipt, if now `Completed`), then
    resolves `_pendingPayment` only if a payment with a matching `IdempotencyKey` is found in the
    refreshed list. Never calls `RecordPaymentAsync`.
  - New `RefreshOrderAndPaymentsAsync`/`ApplyCompletionStateAsync` helpers, extracted to avoid
    duplicating the "refresh, then branch on `Completed`" sequence across the success path, the
    `Failed`-rejection revalidation path, and `CheckPaymentStatusAsync`.
  - `EnterReceiptStateAsync` now sets `_receiptError` (mapped like `RecordPaymentAsync`'s own
    401/403/generic switch) on any non-success `GetReceiptAsync` result instead of leaving `_receipt`
    null with no signal; a new `RetryReceiptAsync` re-invokes it.
  - Markup: a new branch for `_order is { Status: OrderStatusResult.Completed }` with `_receipt`
    still null, showing either "Loading receipt…" or the new `_receiptError` with a `#retry-receipt`
    button — inserted between the existing `_receipt is { }` branch and the `_order is null` branch,
    so it only intercepts the specific "payment confirmed, receipt not yet shown" window. The
    existing payment-entry branch gained a `#retry-payment`/`#check-payment-status` button pair
    (shown only when `_pendingPayment is not null`, mirroring `Sales.razor`'s Milestone B
    `_pendingRetry` markup pattern) and disabled the Cash/EFTPOS buttons while pending.
- `tests/DaxaPos.Web.Tests/Fakes/StubHttpMessageHandler.cs` — added `FailingPathSuffix` (fails only
  requests whose path ends with the given suffix), needed to simulate a receipt-fetch-specific
  network failure without also failing the payment POST that must precede it in the same test.
- `tests/DaxaPos.Web.Tests/Pages/PayTests.cs` — renamed/rewrote
  `NetworkFailureRecordingPayment_ShowsConnectionLostMessage_NotTheOverpaymentMessage` to
  `NetworkFailureRecordingPayment_ShowsPendingPaymentState_WithRetryAndCheckStatusActions`; added
  `Retry_AfterNetworkFailure_ReusesTheSameIdempotencyKey_AndSucceeds` (asserts the retried request's
  `IdempotencyKey` matches the failed attempt's, read from `StubHttpMessageHandler.LastRequest`, and
  that exactly one payment row results), `ConnectivityRestoring_WithoutRetryClick_DoesNotAutoSubmitPayment`,
  `CheckStatus_WhenPaymentAlreadyLandedServerSide_ResolvesPendingState_WithoutASecondPost` (seeds
  `FakeOrderBackend.Payments` directly under the failed attempt's captured idempotency key to
  simulate ack loss, then asserts Check status resolves to the receipt view with no second POST),
  `ReceiptFetchFailure_AfterPaymentCompletesTheOrder_ShowsRecoverableState_NotPaymentButtons` and
  `RetryReceipt_OnceTheEndpointRecovers_ShowsTheReceipt` (using the new `FailingPathSuffix`),
  `ServerRejectsPayment_WhenOrderCompletedElsewhere_RevalidatesAndShowsReceipt`, and a
  `[Theory] AuthFailureRecordingPayment_ShowsAlignedMessage_WithNoRetryOrCheckStatus` covering
  401/403. Extended the existing `OverpaymentAttempt_ShowsServerRejectionMessage_AndOrderStaysOpen`
  test with assertions that no retry/check-status buttons are offered for a genuine rejection.

**Deviations from the approved checklist:** None. One addition beyond the checklist's explicit
items, within its spirit: `RecordPaymentAsync` also guards against starting a new attempt while
`_pendingPayment` is set (not just disabling the buttons in markup), and the Cash/EFTPOS buttons are
disabled while a payment is pending — the checklist didn't spell this out item-by-item, but it
follows directly from "payments are more dangerous than order lines, so be conservative" and from
Decision 2's "generate a new key only after the previous attempt is resolved/cleared."

**Verification:**

- `dotnet build DaxaPos.sln` — 0 errors.
- TDD: all 8 new/modified `PayTests.cs` tests were run before implementation and failed for the
  expected reason (feature not yet built — either the new markup/messages didn't exist, or the old
  behaviour was still in place); one test (`AuthFailureRecordingPayment_ShowsAlignedMessage...`,
  Forbidden case) incidentally passed pre-implementation because the old hardcoded message already
  contained the word "permission," which is expected and not a TDD violation (the paired
  Unauthorized case failed correctly, proving the alignment gap was real).
- `dotnet test tests/DaxaPos.Web.Tests --filter "FullyQualifiedName~PayTests"` — 21/21 passing after
  implementation.
- `dotnet test DaxaPos.sln` — see the report footer below for the full-suite count and
  `git diff --stat` confirmation of scope.

## Recommended Next Session

Milestones A, B, and C are done. Milestone D (multi-tab/multi-device consistency, KDS resilience
under sustained reconnect cycling) is the only remaining outline-only placeholder and requires its
own kickoff-decision pass before implementation.

- Do not start Milestone D without its own kickoff pass and explicit approval.
- Do not start the future Daxa Local Server / Daxa Sync plan — it is not numbered or scheduled yet.
- Do not start PLAN-0009, MAUI, OI-0018, printer routing, or KDS station lifecycle work.
- `main` is 1 commit ahead of `origin/main` (`584b739`) — not pushed this session; push only if the
  human explicitly asks.
