# OI-0017 — Product Archive-and-Replace Concurrency Race

## Status

Open

## Area

Catalog / Data Integrity

## Summary

Two simultaneous `PATCH` requests that both change the same `Product`'s `TaxCategoryId` can both read the same "current" row before either archives it, producing two superseding rows (`SupersededByProductId` pointing at two different new `Product` ids) instead of one. Nothing serialises the read-then-archive-then-create sequence in `ProductEndpoints.ArchiveAndReplaceAsync` (no row lock, no optimistic concurrency token, no atomic conditional update).

## Context

Accepted as a deferred MVP risk at PLAN-0004 planning approval (2026-07-03, Human Decision #4) and re-confirmed at each milestone since (Milestone D's report explicitly reserved filing this issue for Milestone H rather than pre-empting it). Same class of race as OI-0013's `DeviceRegistrationPin.MaxUses` check-then-update pattern — accepted there under the same reasoning (narrow exposure window, fully audited, individually correctable).

## Impact

- Worst case: two `Product` rows both claim to supersede the same archived row, and/or the archived row's own state is inconsistent with which "latest" row callers should actually be looking at.
- Every write in the race is still individually audited (`ProductLifecycleDomainEvent`, `"Archived"`/`"CreatedFromReplace"`) — the sequence of events is fully reconstructable after the fact, so detection is possible even though prevention is not yet built.
- Low real-world likelihood: requires two admin/owner-level users concurrently changing the same product's tax category within the same request-processing window — an unusual operational pattern, not a routine sales-floor action (tax category changes are configuration, not order-time activity).

## Options

1. **Optimistic concurrency token** on `Product` (EF `IsConcurrencyToken` on a `RowVersion`/`xmin`-backed column), retry-or-fail on `DbUpdateConcurrencyException` — smallest, most idiomatic EF fix, consistent with OI-0013's own recommended option for the same race shape.
2. **Atomic conditional update** (`UPDATE ... SET is_archived = true WHERE id = @id AND is_archived = false` via `ExecuteUpdateAsync`), archive-and-replace proceeds only if a row was actually affected; a no-op means another request already archived it, and the caller should re-fetch and retry against the new row.
3. **Row lock** (`SELECT ... FOR UPDATE`) around the read-archive-create sequence.

## Recommendation

Option 1 — matches OI-0013's already-accepted recommendation for the identical race pattern (check-then-update without serialisation), giving the codebase one consistent mechanism for this class of problem rather than two different fixes for two structurally identical bugs. Schedule alongside PLAN-0005 (Orders/Payments), which is the next plan to read `Product` under real concurrent load from order-entry traffic, or as a dedicated hardening pass if PLAN-0005 doesn't naturally touch this code path.

## Decision Needed

- Whether to use the same mechanism as OI-0013 (for consistency) or evaluate independently, since `Product` archive-and-replace additionally *creates* a row (not just increments a counter), which the atomic-`ExecuteUpdateAsync` option handles differently than a pure conditional increment.
- When to schedule it (piggyback on PLAN-0005 vs. a dedicated hardening pass covering both this and OI-0013 together).

## Related Documents

- [OI-0007 — Tax Configuration Editing Permissions](../closed/OI-0007-tax-configuration-editing-permissions.md) (closed) — authorizes the archive-and-replace behaviour this race affects.
- [OI-0013 — DeviceRegistrationPin MaxUses Concurrency Race](OI-0013-device-registration-pin-maxuses-concurrency.md) — same race shape, same accepted-risk precedent.
- [PLAN-0004 worker notes — Milestone D report, archive-and-replace behaviour](../../plans/active/PLAN-0004-worker-notes.md)
- [Module: Product Catalogue](../../modules/catalog.md)
