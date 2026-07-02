# OI-0014 — Tenant-less Unauthenticated Security-Event Auditing

## Status

Open

## Area

Audit / Security

## Summary

Security-relevant events that occur before any tenant can be resolved cannot be written to `audit_events`, because `AuditEvent.TenantId` is non-nullable by design (it is a tenant-owned, tenant-filtered table). Today two such event classes are deliberately unaudited: local login attempts with an unknown email (Milestone C) and device registration attempts with an unknown PIN (Milestone E).

## Context

Both gaps were explicit, recorded decisions rather than oversights: with no matched row there is no tenant to attach the audit row to, and making `TenantId` nullable would weaken the tenant-isolation model for every audited event to accommodate a rare pre-auth case. Rate limiting (registration) and server logs currently cover the gap. The Milestone E approval recorded that "unauthenticated/global security events may need a separate, tenant-less table later" — this issue is that record.

## Impact

- Credential-stuffing or PIN-guessing campaigns against unknown identifiers are invisible to any future audit-read API — only to infrastructure logs.
- Affects the audit module's eventual read/reporting surface and any SIEM-style export.
- The same question recurs for every future pre-auth surface (e.g. future kiosk/self-order bootstrap flows).

## Options

1. **Separate tenant-less `security_events` table** (unfiltered, append-only, minimal columns: event type, reason, remote IP, attempted identifier hash, timestamp) written by the pre-auth paths.
2. **Structured logging only** (status quo, formalised): declare infrastructure logs the system of record for pre-tenant events and document retention expectations.
3. **Nullable `AuditEvent.TenantId`** — rejected in principle already; it erodes the fail-closed filter guarantees for the entire audit table to serve a rare case.

## Recommendation

Option 1, scheduled when the audit read API is built (no consumer exists yet — building the table before anything can read it delivers nothing). Option 3 should stay rejected.

## Decision Needed

- Whether a tenant-less security-event store is required for MVP or deferred to the audit/reporting plan.
- Retention and access rules for whichever store is chosen (these events may contain attacker-supplied identifiers).

## Related Documents

- [ADR-0010 — Financial Records, Ledger, and Audit](../../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
- [ADR-0015 — Tenant Isolation and Session Token Mechanism](../../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md)
- [Module: Audit](../../modules/audit.md) (records the gap)
