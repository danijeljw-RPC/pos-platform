# PLAN-0007 — Browser/PWA Offline and Reconnect Resilience

## Status

Draft. Revised 2026-07-08 to match the current implemented architecture. **Milestone A
(Reconnect And Read Resilience) is implemented and complete** (2026-07-08) — see the worker notes'
Milestone A Implementation Report. Milestones B onward remain outline-only and not yet approved for
implementation. See `docs/plans/active/PLAN-0007-worker-notes.md` for the full revision rationale
and implementation detail.

## Revision Note (2026-07-08)

The original PLAN-0007 draft (written 2026-06-29, before any implementation existed) described a
full **Daxa Local Server + Daxa Sync engine**: a second deployable with its own PostgreSQL
instance, a `DaxaPos.Modules.Sync` project, local-to-cloud/cloud-to-local sync workers, a
`SyncConflict` table, and an admin conflict-review queue. That architecture is still valid future
product direction (ADR-0002, ADR-0007), but it does not match what exists in the repository today
and is not what this plan currently implements.

As of this revision, the solution (`src/`) contains exactly one API (`DaxaPos.Api`), one
PostgreSQL database, and one Blazor WebAssembly PWA (`DaxaPos.Web`) covering Terminal, Back
Office, Display, and KDS — no second deployable, no local server, no sync worker exists anywhere
in the codebase. PLAN-0006 (now fully complete) built the PWA's client-side state layer
(`DeviceContextStore`, `SessionState`, `BackOfficeSessionState`, `IDraftOrderStore`, all backed by
`localStorage`) and an established poll-loop pattern (`Display.razor`, `Kds.razor`).

**PLAN-0007 is therefore re-scoped**: it now covers making the existing PWA resilient to
connectivity loss against the existing single API — a browser-level concern, not a
distributed-systems one. The original local-server/sync-worker architecture is preserved as
forward-looking direction in the Handoff Notes below, for a future, separately-numbered plan.

OI-0006 (Hybrid Sync Conflict Rules), which the previous draft listed as a blocking dependency, was
closed on 2026-07-01. Its resolution — a category-based conflict model (operational data is
append-only and local-origin authoritative; configuration, reference, and master data are
cloud-authoritative; device/runtime data is local-authoritative except for security-sensitive
records) — is folded into ADR-0007's Acceptance Addendum and is treated below as **resolved
precedent**, not an open blocker.

## Goal

Make the existing Blazor WebAssembly PWA (`Sales`, `Pay`, `Display`, `Kds`) resilient to API and
network connectivity loss, without introducing any new deployable component, local server, or sync
worker. Establish clear, consistent behaviour for each terminal-facing screen when connectivity
drops and is restored, distinguishing "the API is unreachable" from "the API responded with a real
error."

## Scope

Browser/PWA-level offline and reconnect resilience for the four terminal-facing screens in
`src/DaxaPos.Web`, operating entirely against the existing single Daxa API. See the Milestone
Breakdown below — only Milestone A is scoped for implementation in this revision.

## Non-goals

These apply to PLAN-0007 as a whole, not just Milestone A, unless a future milestone revision
explicitly changes one:

- No local server / Daxa Local Server as a deployable component.
- No `DaxaPos.Modules.Sync` project or any sync worker service.
- No local-to-cloud or cloud-to-local synchronisation.
- No `SyncConflict` data model or admin conflict-review UI.
- No offline creation of orders, payments, refunds, or receipts, and no offline KDS state changes
  (Milestone A is read/poll paths only — see Milestone A's own non-goals below for the precise
  boundary).
- No MAUI work, no OI-0018 (production printer routing), no PLAN-0009 (Stripe Terminal), no KDS
  station lifecycle.
- No new EF Core migrations.
- No product/backend endpoint contract changes assumed by default — Milestone A is expected to be
  Web-only, matching PLAN-0006's established pattern of reusing existing endpoints.

## Context Read

- `CLAUDE.md`
- `docs/adr/accepted/ADR-0007-local-hybrid-sync-principles.md`
- `docs/adr/accepted/ADR-0002-cloud-local-hybrid-deployment.md`
- `docs/issues/closed/OI-0006-hybrid-sync-conflict-rules.md`
- `docs/architecture/sync.md`
- `docs/architecture/deployment-modes.md`
- `docs/modules/sync.md`
- `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md` (full milestone history —
  `Sales`/`Pay`/`Display`/`Kds` current behaviour, `IDraftOrderStore`, poll-loop precedent)
- `docs/plans/active/PLAN-0006-worker-notes.md`
- `docs/plans/active/PLAN-0010-docker-compose-local-dev-stack.md` (confirms current single-API,
  single-database topology)
- `docs/issues/index.md`, `docs/issues/open/OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md`
  (tangential — session validity across a dropped/restored connection)

## Files Likely To Change (Milestone A only)

```text
src/DaxaPos.Web/Pages/Sales.razor
src/DaxaPos.Web/Pages/Pay.razor
src/DaxaPos.Web/Pages/Display.razor
src/DaxaPos.Web/Pages/Kds.razor
src/DaxaPos.Web/Api/DaxaApiClient.cs        (only if network-vs-HTTP-error classification needs work)
src/DaxaPos.Web/Api/ApiResult.cs            (only if network-vs-HTTP-error classification needs work)
src/DaxaPos.Web/State/                       (new shared connectivity-state type, only if it reduces
                                              duplication across the four pages — not mandated)
tests/DaxaPos.Web.Tests/Pages/*Tests.cs
tests/DaxaPos.Web.Tests/Api/DaxaApiClientTests.cs (if touched)
```

No backend (`DaxaPos.Api`/`.Application`/`.Domain`/`.Infrastructure`/`.Persistence`) files are
expected to change for Milestone A. If Milestone A kickoff finds a genuine backend gap, it must be
flagged and decided explicitly, not folded in silently — matching this project's established
PLAN-0006 practice.

## Architecture Assumptions

- The current deployed architecture is one Daxa API plus one PostgreSQL database plus one PWA.
  There is no separate Daxa Local Server, no `DaxaPos.Modules.Sync` project, and no second
  deployable anywhere in the solution today — confirmed by reading `src/` directly, not assumed.
- Daxa Cloud/Local/Hybrid (ADR-0002) remain the target deployment-mode architecture, but this plan
  in its current form operates entirely within a single-API topology and does not build or assume
  a second server.
- The PWA already holds meaningful client-side state in `localStorage`: `DeviceContextStore`,
  `SessionState`, `BackOfficeSessionState`, and `IDraftOrderStore` (a device-scoped pointer to a
  real server-side `Order`). This existing state layer is what Milestone A makes more resilient —
  it is not a new concept being introduced.
- The server remains authoritative for all order/payment/tax/pricing state (CLAUDE.md; ADR-0007's
  "server state is authoritative" principle, applied at the browser layer rather than a local-server
  layer). Milestone A does not compute or infer financial state client-side beyond what already
  exists (`Pay.razor`'s balance-due arithmetic over server-supplied totals).
- `Display.razor`'s existing cancellable poll loop (`[Parameter] TimeSpan PollInterval`) is the
  established precedent for "read state on a timer, degrade gracefully on failure" and should be
  reused/generalised rather than replaced with something new.

## Domain Assumptions

- Applying ADR-0007's local-vs-cloud conflict-authority model to a browser-vs-API split is treated
  as an **extension of the same principle** (append-only operational data; server-authoritative
  configuration and totals), not a new architecture decision requiring its own ADR. This is noted
  explicitly so it is not silently equated with actually standing up a local server.
- "Offline," for Milestone A, means the browser cannot currently reach the Daxa API (network
  failure, DNS failure, timeout), as distinct from the API being reachable but returning a genuine
  401/403/404/5xx. Where today's `DaxaApiClient`/`ApiResult` classification cannot cleanly tell
  these apart, that is a Milestone A implementation question to resolve at kickoff by reading the
  code, not a product decision to pre-empt here.
- OI-0006's resolved category rules (operational = local/append-only, configuration = server/cloud
  master) are the reference model for any state Milestone A caches or retains client-side, even
  though nothing in Milestone A writes offline.

## Risks

- Browser `HttpClient` failures (network unreachable, timeout, CORS failure) may not currently be
  cleanly distinguishable from a legitimate 4xx/5xx in `ApiResult`'s existing classification —
  Milestone A kickoff must confirm this by reading `DaxaApiClient.cs`/`ApiResult.cs` before
  designing the connectivity-state UI around it, not assume a fix is needed.
- "Preserve last-known-good state" (already partly true for `Display.razor`/`Kds.razor`) must not
  silently mask a real state change that happened while disconnected (e.g. an order voided from
  another terminal). Reconnect must actively revalidate against the server, not just keep showing
  stale data indefinitely.
- Reconnect-resilience UX sits close to "why can't I just add a line while offline" — Milestone A's
  non-goals (no offline writes) must be enforced deliberately during implementation, not just
  documented here.

## Milestone Breakdown

| Milestone | Scope | Status |
|-----------|-------|--------|
| A | Reconnect and read resilience — connectivity state, degrade/recover behaviour for `Sales`/`Pay`/`Display`/`Kds` reads. No offline writes. | **Implemented and complete (2026-07-08).** See worker notes. |
| B | Offline-safe local drafts / a bounded write queue for `Sales` (e.g. queuing add-line calls made while briefly disconnected, replayed on reconnect with idempotency). | Outline only — scope, risk, and product questions (e.g. how long a queue may live, what happens if the underlying order was voided elsewhere) to be defined at B kickoff. Not started. |
| C | Payment/receipt behaviour policy under intermittent connectivity. | Outline only. Likely requires an explicit product decision on whether any payment method may ever be recorded while offline (cash is the obvious candidate; integrated/manual EFTPOS plausibly should never be) — flagged as a genuine open product question, not decided here. Not started. |
| D | Multi-tab/multi-device consistency and KDS resilience under sustained reconnect cycling (e.g. a KDS board that has been offline for an extended period). | Outline only. Not started. |

Milestones B–D are placeholders for sequencing only. Each requires its own kickoff-decision pass
(matching PLAN-0006's established per-milestone pattern) before implementation, and none should be
started without a plan-doc update first.

### Milestone A — Reconnect And Read Resilience

**Scope:**

- `Sales`, `Pay`, `Display`, and `Kds` read/poll/fetch behaviour when API/network connectivity
  drops.
- Distinguish network/reconnect failures from true 401/403/404/server responses where practically
  possible given the current `DaxaApiClient`/`ApiResult` shape.
- Add a small shared connectivity/reconnect state pattern only if it reduces duplication across the
  four pages and fits the existing Blazor codebase's conventions — not mandated up front.
- Surface a consistent online/offline/reconnecting indicator across the terminal-facing screens.
- Preserve last-known-good state where that is already the intended behaviour (`Display.razor`'s
  sticky-completed rule, `Kds.razor`'s "keep showing the last-successfully-loaded board" rule), with
  explicit revalidation on reconnect rather than indefinite staleness.

**Non-goals (Milestone A specifically):**

- No creation of orders, payments, refunds, or receipts while offline.
- No KDS state changes while offline.
- No offline write queue of any kind.
- No local server components.
- No EF Core migrations.
- No start of PLAN-0009, MAUI, OI-0018, printer routing, or KDS station lifecycle.

**Implementation / Documentation Steps (plan-level, not a task-by-task breakdown — that is
Milestone A's own kickoff-decision work, not this planning revision's):**

1. Kickoff review: read `DaxaApiClient.cs`/`ApiResult.cs` to confirm exactly what is returned today
   for a genuine network failure versus an HTTP error response, before designing around an assumed
   gap.
2. Decide, at kickoff, whether a shared connectivity-state type is justified by actual duplication
   across `Sales`/`Pay`/`Display`/`Kds`, or whether each page's existing per-page state is
   sufficient.
3. Implement the online/offline/reconnecting indicator across the four screens.
4. Implement reconnect revalidation so a restored connection re-checks server state rather than
   trusting a stale last-known-good snapshot indefinitely.
5. Add bUnit coverage for each screen's degraded/reconnecting/recovered states.
6. Update `docs/modules/sync.md` and this plan's Status/closeout to reflect what Milestone A
   actually built.

### Milestone A Closeout (2026-07-08)

Implemented as planned above, no deviations from the non-goals. Summary — full detail in the
worker notes' Milestone A Implementation Report:

- `ApiResultKind` gained a distinct `NetworkFailure` case (previously indistinguishable from a real
  HTTP error, both mapped to `Failed`); `ApiErrorMessages` gained a matching `ConnectionLost` message.
- New `IConnectivityTracker`/`ConnectivityTracker` (in-memory, per-tab, not persisted) and a
  `ConnectivityHandler` (`DelegatingHandler`, mirroring the existing `AuthHeaderHandler` pattern) —
  reports connectivity automatically for every API call with zero changes to `DaxaApiClient`'s
  public surface.
- New `ConnectivityBanner.razor` — resolves the tracker defensively (`IServiceProvider.GetService`,
  not `@inject`), so it renders nothing wherever a tracker isn't wired, rather than throwing.
  Added to `Sales`/`Pay`/`Display`/`Kds`.
- `Display.razor`'s `LoadOrderAsync` fixed: a network failure now preserves the last-shown
  order/receipt instead of resetting to idle (`Kds.razor` already did the equivalent correctly and
  needed no logic change, only the banner).
- `Sales.razor`/`Pay.razor`: initial load extracted into a retryable `LoadAsync()` with a manual
  "Retry" button; action-failure messages (add-line, void, clear, record payment) now route through
  `ApiErrorMessages` so a dropped connection reads as "Can't reach the server," not a misleading
  server-rejection message (`Pay.razor`'s payment-failure message previously said "may exceed the
  amount owing" even on a pure network failure).
- No backend/API/schema changes — confirmed via `git diff --stat`, entirely `src/DaxaPos.Web` and
  `tests/DaxaPos.Web.Tests`.
- Full solution suite: **1224/1224 passing** (144 unit + 150 Web + 930 API — up from the
  pre-Milestone-A baseline of 1196; 28 new Web tests, 0 API changes). No regressions.

## Tests To Run Later

- Full solution suite: `dotnet test DaxaPos.sln` (regression baseline, currently 1178/1178 passing
  per PLAN-0006's Milestone G closeout).
- New/targeted bUnit suites for `Sales`/`Pay`/`Display`/`Kds` connectivity-state behaviour.

## Documentation To Update

- `docs/modules/sync.md` — currently describes only the local-server sync model; needs a note
  distinguishing that from this plan's browser-resilience scope.
- This plan document — Milestone A closeout, once implemented.
- `docs/plans/active/PLAN-0007-worker-notes.md` — Milestone A kickoff/closeout detail.

## ADRs Required

None required to start Milestone A. ADR-0007 remains the guiding ADR; applying its local-vs-cloud
conflict-authority principle to a browser-vs-API split (see Domain Assumptions) is treated as an
extension of that ADR's existing principle, not a new deployable-architecture decision requiring
its own ADR. If Milestone A's connectivity-state pattern surfaces a genuinely new client-side
principle worth recording at closeout, a short ADR addendum can be proposed then — not pre-empted
here.

## Open Issues Required

None blocking. OI-0006 is closed and its resolution is treated as reference precedent (see Revision
Note above). OI-0012 (inactive parent lifecycle vs. device/staff authentication) is tangentially
related — a dropped-and-restored connection may reconnect into a session that was invalidated while
offline — but does not block Milestone A; a 401 on reconnect is handled the same way
`Display.razor` already treats any 401/403/404 today (degrade, don't crash).

## Commit Sequence

```text
docs: revise PLAN-0007 scope to browser/PWA offline resilience
docs: add PLAN-0007 worker notes
feat(web): add connectivity tracking and reconnect resilience for Sales/Pay/Display/Kds
```

## Handoff Notes

### Immediate

Milestone A is implemented and complete (2026-07-08; see Milestone A Closeout above and the worker
notes' Implementation Report). Milestone B (offline-safe local drafts / bounded write queue) remains
an outline-only placeholder and requires its own kickoff-decision pass before any implementation
starts — not begun.

### Future Plan — Daxa Local Server / Daxa Sync

The original PLAN-0007 draft's architecture — a second deployable Daxa Local Server with its own
PostgreSQL instance, a `DaxaPos.Modules.Sync` project, local-to-cloud/cloud-to-local sync workers, a
`SyncConflict` table, an admin conflict-review queue, and a full-state-reload API — remains valid
future product direction per ADR-0002 and ADR-0007, and per `docs/architecture/sync.md`/
`docs/modules/sync.md`. It is **not** part of PLAN-0007 in its current, revised form.

That architecture should become its own, separately-numbered plan once there is a product decision
to actually build a second deployable. Before that plan can start, it should resolve:

- Local-server hosting/packaging story (how a venue actually runs it — bare metal, Docker, a
  managed appliance).
- How a local PostgreSQL instance relates to the cloud instance (schema parity, migration
  strategy, backup/restore).
- The sync transport and protocol (the original draft assumed HTTPS push/pull on port 443; this
  should be re-confirmed, not carried over unexamined).
- Whether ADR-0007's local-vs-cloud conflict rules (written before any implementation existed)
  still hold once a real local server is being built, or need revision based on what's learned
  building PLAN-0007's browser-level resilience first.

PLAN-0007's Milestone A–D work (browser/PWA resilience against the single existing API) is a
reasonable, lower-risk precursor to that future plan: the connectivity-state patterns, the
"what's safe to cache client-side" boundaries, and the reconnect-revalidation discipline built here
will still be relevant once a local server exists — the local server would sit *behind* the PWA's
existing API calls, largely transparent to the browser layer, rather than requiring the PWA's
resilience logic to be rebuilt.
