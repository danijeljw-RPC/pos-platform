# OI-0016 — Define Completed-Plan Archival Convention

## Status

Open

## Area

Documentation / Process

## Summary

There is no decided convention for what happens to a plan document once its work is finished. `docs/plans/completed/` exists but is empty, and `docs/README.md`'s "Active Plans" list currently makes no distinction between a plan still being worked on and one that is fully complete.

## Context

PLAN-0002 (Platform Skeleton) has been complete and committed for some time but was never moved out of `docs/plans/active/`. PLAN-0003 (Identity, Tenancy, Locations, Devices) reached completion at Milestone H (2026-07-03); the same question was raised explicitly during Milestone H closeout planning. Human decision at that time: keep PLAN-0003 in `active/` rather than move it, specifically to avoid (a) creating a one-off convention applied to only one of two equally-finished plans, and (b) unnecessary churn updating the 17 files across `docs/` that link to PLAN-0003's `active/` path by relative reference. This issue exists to hold the underlying convention question open until it is deliberately decided — for PLAN-0002 and PLAN-0003 together, not piecemeal.

## Impact

- A reader of `docs/README.md`'s Active Plans list cannot currently tell which listed plans are still in progress and which are finished but simply never relocated.
- Every future plan that reaches completion faces the same ambiguity until this is resolved, and the gap between "finished" plans widens each time (currently PLAN-0002 and PLAN-0003).
- Any convention chosen has a one-time migration cost (moving files, fixing inbound links) that grows the longer it's deferred.

## Options

1. **Move completed plans to `docs/plans/completed/`**, updating every inbound link in the same commit as the move.
2. **Keep completed plans in `active/`** but add an explicit status marker in `docs/README.md` (e.g. an "Active" vs "Complete" annotation per plan) — no file moves, no link churn.
3. **Introduce a stable index/redirect layer** (e.g. a thin stub left at the old path, or referencing plans by number rather than full relative path in cross-links) so a future move doesn't break existing links.
4. **Status quo indefinitely** — rejected as drift; the ambiguity only compounds.

## Recommendation

Decide once, for PLAN-0002 and PLAN-0003 together, rather than moving PLAN-0003 alone and leaving PLAN-0002 as a lingering inconsistency. No option is favoured here — this is a process/documentation-structure decision, not a technical one.

## Decision Needed

- Whether completed plans move to `docs/plans/completed/` at all.
- Whether a plan's worker-notes file moves with it.
- Whether inbound links are updated in place at move time, or a stable redirect/index layer is used instead so moves don't break links.
- Whether PLAN-0002 and PLAN-0003 should be migrated together, later, in one pass.
- Whether `docs/README.md` should distinguish active / complete-but-retained / archived plans explicitly.

## Related Documents

- [PLAN-0002 — Platform Skeleton](../../plans/active/PLAN-0002-platform-skeleton.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
- [PLAN-0003 worker notes — Milestone H planning pass](../../plans/active/PLAN-0003-worker-notes.md)
- [Documentation Index](../../README.md)
