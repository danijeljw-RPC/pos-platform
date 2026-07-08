# PLAN-0007 Worker Notes

## Status

PLAN-0007 rescoped (docs only) 2026-07-08. No implementation has started. Milestone A
(Reconnect And Read Resilience) is defined and awaiting approval; Milestones B–D are outline-only
placeholders. See the plan doc
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

## Recommended Next Session

Get explicit human approval of Milestone A's scope as written in the plan doc, then start Milestone
A implementation:

- Read `src/DaxaPos.Web/Api/DaxaApiClient.cs` and `ApiResult.cs` first to confirm the current
  network-failure-vs-HTTP-error classification before designing the connectivity indicator.
- Decide whether a shared connectivity-state type is justified by real duplication across
  `Sales`/`Pay`/`Display`/`Kds`, or whether per-page state is sufficient — do not build shared
  infrastructure speculatively.
- Follow PLAN-0006's established practice: a kickoff-decision note in the plan doc before writing
  code, TDD per CLAUDE.md's testing rules, and a closeout report with verification detail once done.

Do not:

- Start Milestone B, C, or D — each needs its own kickoff-decision pass first.
- Start the future Daxa Local Server / Daxa Sync plan — it is not numbered or scheduled yet.
- Add offline writes, a write queue, local server components, or migrations under Milestone A.
- Start PLAN-0009, MAUI, OI-0018, or KDS station lifecycle work.
