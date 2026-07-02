# OI-0013 — DeviceRegistrationPin MaxUses Concurrency Race

## Status

Open

## Area

Devices / Security

## Summary

Two device registrations racing the last remaining use of a `DeviceRegistrationPin` can both pass the in-memory `UsedCount < MaxUses` check and both register, because nothing serialises the check-and-increment (no row lock, no optimistic concurrency token, no atomic conditional `UPDATE`).

## Context

Accepted as a deferred risk at Milestone E approval (2026-07-02). The exposure window is milliseconds, on a secret that is 6 digits, single-venue, expires in 15 minutes, and defaults to `MaxUses = 1` (max 10) — correct for normal MVP usage, but the invariant "a PIN registers at most `MaxUses` devices" is not actually guaranteed under concurrency.

## Impact

- Worst case: more devices registered against a venue than the issuing admin intended — each still fully audited (`DeviceRegistered`) and individually revocable, so detection/remediation paths exist.
- The same check-then-update pattern will tempt future bootstrap-ish flows (gift-card redemption, voucher use); fixing it here establishes the pattern.

## Options

1. **Optimistic concurrency token** on `UsedCount` (EF `IsConcurrencyToken`), retry-or-fail on `DbUpdateConcurrencyException` — smallest, most idiomatic EF fix.
2. **Atomic conditional update** (`UPDATE ... SET used_count = used_count + 1 WHERE id = @id AND used_count < max_uses` via `ExecuteUpdateAsync`), registration proceeds only if a row was affected.
3. **Row lock** (`SELECT ... FOR UPDATE`) around the validation+increment.

## Recommendation

Option 2 — race-proof without retry loops or transaction-scope widening, and expressible with EF Core's `ExecuteUpdateAsync`. Schedule alongside the next milestone that touches device registration; not urgent on its own.

## Decision Needed

- Which mechanism, and when to schedule it (piggyback vs. dedicated hardening pass).

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](../../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)
- [PLAN-0003 worker notes — Milestone E report, deferred risks](../../plans/active/PLAN-0003-worker-notes.md)
