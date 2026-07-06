# PLAN-0006 Worker Notes — Planning Pass (2026-07-06)

## Session Purpose

Turn the architecture-level PLAN-0006 draft into an implementation-ready, milestone-by-milestone
plan, following the exact process PLAN-0004's and PLAN-0005's own planning passes used. No product
code, migrations, endpoints, or UI files were created in this session — planning/documentation
only, per CLAUDE.md's planning-before-work rule and this session's explicit instruction not to
implement UI, application code, migrations, endpoints, printer routing, hardware/device
orchestration, or MAUI yet.

## What Was Read

Full pass over: `CLAUDE.md`; `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md`
and `PLAN-0005-worker-notes.md` (all six milestone reports); `docs/modules/orders.md`, `payments.md`,
`refunds.md`, `receipts.md`, `printing.md`; `docs/issues/open/OI-0017-product-archive-and-replace-
concurrency.md`, `OI-0018-location-scoped-production-printer-routing.md`; `docs/adr/accepted/
ADR-0004-windows-maui-and-pwa-device-strategy.md` (including its addendum) and `ADR-0008-device-
identity-vs-user-identity.md` (in full — this is the accepted device registration flow spec);
`docs/modules/customer-display.md`, `kds.md`; `docs/architecture/device-strategy.md`; `docs/
deployment/windows-terminal.md`; `docs/plans/active/PLAN-0009-first-payment-adapter-stripe-
terminal.md` (confirmed still Draft/unimplemented); the existing `docs/plans/active/PLAN-0006-
terminal-display-pwa-planning.md` draft (pre-existing, being rewritten by this pass, not created
from nothing); `docs/README.md`, `docs/adr/index.md`, `docs/CHANGELOG.md` (tail, to confirm the
per-milestone-not-per-planning-pass entry convention). Also grepped current source to confirm
`DeviceRegistrationPinEndpoints.cs`, `DeviceRegistrationEndpoints.cs`, and `AuthEndpoints.cs`'s
`/staff-pin/login` already exist and are mapped in `Program.cs` from PLAN-0003.

## What Was Produced

1. `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md` — rewritten from a single-pass
   architecture-level draft (Goal/Scope/Non-goals/one flat 10-step Implementation list) into 7
   named milestones (A–G) with concrete deliverables, tests/verification, docs-to-update, and
   explicit out-of-scope items per milestone, plus 6 Human Decisions Needed items, RBAC/staff-PIN
   expectations, offline/PWA considerations, and display/KDS/terminal/printer boundary sections.
2. This file.

Not touched: `docs/adr/index.md`, `docs/issues/index.md`, `docs/CHANGELOG.md`, `docs/README.md`
(PLAN-0006 was already correctly listed in README's Active Plans section — no change needed
there). No ADR or issue status changed; OI-0017/OI-0018 remain open, discussed only as risk
context.

## Design Decisions Worth Flagging to a Future Reader

- **This plan has almost no backend work of its own for Milestones A–D.** Device registration and
  staff-PIN-login endpoints already exist from PLAN-0003; order/payment/refund/receipt endpoints
  already exist from PLAN-0005. This is a genuinely different shape of plan from PLAN-0004/PLAN-0005
  (which built backend foundations milestone-by-milestone) — PLAN-0006 is thin UI over already-
  shipped APIs for most of its scope. The one place this could change is Milestone F (see below).
- **PLAN-0009 (Stripe Terminal) is still Draft, not implemented.** This is why integrated/card
  payment UI is a hard Non-goal here, not just an unstated gap — the backend has nothing for such a
  screen to call (`PaymentMethod.Integrated` is rejected 400 today). Flagged as Human Decisions
  Needed #3 whether to stub a disabled button now so Milestone C's layout survives PLAN-0009
  landing later without rework.
- **MAUI cannot be built or tested from this (macOS) environment.** This is the single largest
  practical risk to actually executing Milestones A–D and is called out as Human Decisions Needed
  #2 — a process/environment decision, not an architecture one, but it blocks all MAUI
  implementation work regardless of any other decision made. Back Office/KDS (Milestones E/F) are
  not blocked by this — they're ordinary web/PWA work — so those two tracks can proceed
  independently once each track's own blocker (environment for A–D, framework choice for E/F) is
  resolved.
- **No PWA framework has been chosen anywhere in this repository's existing decisions.** CLAUDE.md
  says "web application/PWA" without naming one; ADR-0004's addendum mentions a "local Blazor SSR
  application" but that's a different app (MAUI-update management on the Daxa Local server, not
  Back Office/KDS). This is a real, un-made architecture decision blocking Milestone E/F start —
  Human Decisions Needed #1. Deliberately not defaulted to Blazor here despite that adjacent
  mention, since the addendum's Blazor usage is for a different application with different
  requirements (a local-server-hosted deployment/update tool, not a customer/staff-facing PWA).
- **Milestone F (KDS) was deliberately scoped down, not deleted.** The original draft's step 9
  ("Scaffold `DaxaPos.KdsPwa`... order display, status updates, reconnect/rebuild state") reads as
  full KDS scope, but `docs/modules/kds.md`'s own Status section says "KDS is Phase 2 scope (not
  MVP)" while also saying "API endpoints for kitchen order routing must be designed in MVP for
  future use" — an internal tension the original draft didn't resolve. Resolved here by scoping
  Milestone F to a minimal read-only open-orders board over the *existing* order-list endpoint (no
  new kitchen-ticket/station-routing backend), explicitly deferring the full lifecycle and flagging
  (Human Decisions Needed #6) that even this minimal board might reveal a need for one narrowly-
  scoped new read endpoint once someone actually tries to build it.
- **A refund screen's placement (Milestone C vs. deferred) was deliberately left as an open
  decision (#4) rather than assumed either way.** The backend (`payments.refund`) is fully built and
  working; nothing technically blocks adding a refund UI now. The only reason to defer would be to
  keep Milestone C's scope tight, which is a product/sequencing call, not an architecture one — so
  it's posed as a question rather than silently included or silently excluded.
- **Device-registration-PIN generation is called out as a hard dependency of Milestone A**, not
  just a nice-to-have of Milestone E — ADR-0008 explicitly requires an admin-portal screen for
  generating/viewing the PIN. Milestone A can still be built and manually tested against the raw
  API before that screen exists (the endpoint doesn't require a UI to call it), but the plan is
  explicit that production use of Milestone A needs Milestone E's PIN screen to exist too.
- **No CHANGELOG entry was added for this planning pass**, matching the confirmed repo convention:
  every existing CHANGELOG entry corresponds to a milestone's actual implementation (PLAN-0004 and
  PLAN-0005's own initial planning passes have no standalone CHANGELOG entries either — their first
  entries are each plan's Milestone A). This planning pass follows that same precedent rather than
  introducing a new one.

## Open Items Requiring the User's Explicit Sign-Off

See "Human Decisions Needed" in the plan itself — summarized: (1) choose a PWA framework for Back
Office/KDS; (2) confirm the Windows/MAUI development environment plan before Milestone A's
implementation session begins; (3) confirm whether to stub a disabled "Card" payment button in
Milestone C; (4) confirm whether a refund screen belongs in Milestone C or is deferred; (5) confirm
Milestone E's write-surface is limited to device-PIN generation (read-mostly otherwise) rather than
a full admin CRUD sweep; (6) acknowledge Milestone F's backend touch is currently assumed to be
zero and may need revisiting once actually attempted.

## Recommended Next Session

1. Human reviews and (dis)approves the six Human Decisions Needed items above.
2. On approval/acknowledgement of #2 specifically (even if the answer is "MAUI work is blocked
   until a Windows environment is available"), Milestone A can start. Milestones E/F can start
   independently once #1 is resolved, regardless of #2's outcome.
3. Update this plan's Status section with milestone checkboxes as work proceeds — no more than 3
   commits without a plan refresh, per CLAUDE.md's plan-refresh rule, exactly as PLAN-0003/0004/0005
   did throughout their own milestones.
