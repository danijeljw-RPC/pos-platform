# ADR-0007 — Local/Hybrid Sync Principles

## Status

Accepted

## Context

Daxa Local and Daxa Hybrid deployments require data to flow between the local server and the cloud. Orders created locally must eventually reach the cloud for reporting and backup. Configuration changes made in the cloud must reach local servers to update pricing, menus, and tax settings.

This sync must be reliable, auditable, and resilient to partial failures and conflicts.

## Decision

Local-to-cloud and cloud-to-local sync in Daxa POS follows these principles:

1. **Idempotency keys** — Every sync operation includes an idempotency key so that retries do not create duplicate records.
2. **Append-only event log** — Sync events are logged with timestamps, direction, status, and entity references. Sync history is not deleted.
3. **Server state is authoritative** — For Daxa Hybrid, the cloud is the master of configuration (menus, pricing, tax). The local server is the master of operational data (orders, payments) created during local operation.
4. **Conflicts are explicit** — When a sync conflict occurs (e.g. same record modified locally and in cloud before sync), the conflict is surfaced explicitly for review rather than silently overwritten.
5. **Sync failure must not stop trading** — If the sync service fails or the network is unavailable, local operations continue unaffected. Sync retries when connectivity is restored.
6. **Full-state reload** — After reconnect, devices must be able to rebuild their full operational state by requesting a full reload from the authoritative server, not by replaying missed events.
7. **Audit sync events** — All sync activity is written to the audit log.

## Consequences

**Positive:**
- Internet loss does not stop trading.
- Conflict handling is visible and auditable.
- Retry logic is safe via idempotency.
- Historical sync records support debugging and reporting.

**Negative:**
- Sync engine is a significant engineering investment.
- Conflict resolution rules must be explicitly designed.
- Full-state reload can be expensive for large datasets.

## Alternatives Considered

1. **Event sourcing only** — Considered. Useful but adds complexity; hybrid approach with idempotent operations preferred.
2. **Last-write-wins conflict resolution** — Rejected. Risk of silent data loss on financial records.

## Open Questions

- See [OI-0006 — Hybrid Sync Conflict Rules](../../issues/open/OI-0006-hybrid-sync-conflict-rules.md)

## Related Documents

- [ADR-0002 — Cloud, Local, Hybrid Deployment](ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0010 — Financial Records Ledger and Audit](ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Sync](../../architecture/sync.md)
- [Module: Sync](../../modules/sync.md)
- [PLAN-0007 — Sync, Local, Hybrid](../../plans/active/PLAN-0007-sync-local-hybrid-planning.md)

---

## Acceptance Addendum

ADR-0007 is accepted.

Local/hybrid sync will follow an explicit, auditable, category-based conflict model.

The accepted design preserves local trading during internet or cloud outages while keeping configuration, reporting, and support visibility aligned through cloud sync.

## Accepted Sync Authority Model

Daxa Hybrid deployments use different authorities for different data categories.

### Operational Data

Operational data is local-origin authoritative.

This includes:

- Orders.
- Order lines.
- Payments.
- Refunds.
- Voids.
- Cash drawer events.
- Staff actions.
- Receipts.
- Ledger entries.
- Audit events.

Operational records are append-only. They should not be edited in-place after being committed.

Corrections must be represented by new records such as refunds, voids, reversals, or adjustment entries.

The cloud receives operational data from the local server and stores it for backup, reporting, multi-location visibility, and support.

### Configuration Data

Configuration data is cloud-authoritative.

This includes:

- Menus.
- Products.
- Categories.
- Modifiers.
- Prices.
- Taxes.
- Product tax assignments.
- Surcharges.
- Discounts.
- Staff permissions.
- Device configuration.
- Location settings.
- Kitchen routing.
- Receipt templates.

Configuration changes should flow from cloud to local servers.

If local and cloud configuration changes conflict, the cloud version wins by default, but the conflict must be recorded and surfaced where appropriate.

### Reference and Master Data

Reference and master data is cloud-authoritative.

This includes:

- Client records.
- Location records.
- Region records.
- Tenant settings.
- Licence data.
- Identity provider mappings.

Local servers may cache this data, but the cloud remains the source of truth.

### Device and Runtime Data

Device and local runtime data is local-origin authoritative where it represents actual local state.

This includes:

- Printer discovery state.
- Payment terminal pairing status.
- Kitchen display station status.
- Local server health.
- Sync agent status.

The cloud may store reported copies for monitoring and support.

Security-sensitive device identity conflicts must be reviewed rather than automatically overwritten.

## Conflict Handling

The accepted conflict strategy is:

1. Prevent conflicts by design where possible.
2. Use append-only operational records so trading history is not overwritten.
3. Use cloud-authoritative configuration for menu, price, tax, and permission data.
4. Use idempotency keys to make sync retries safe.
5. Record all conflicts in a sync conflict log.
6. Automatically resolve safe conflicts using known authority rules.
7. Send unsafe or ambiguous conflicts to an admin review queue.
8. Surface conflicts in the admin UI at both client and location level.

Last-write-wins is rejected as a general strategy because it can silently lose data and is unsafe for financial records.

## Admin Review Queue

An admin review queue is required.

The review queue is used for conflicts that cannot be safely resolved by the standard authority rules.

Examples include:

- Financial records that do not match expected idempotency behaviour.
- Tax configuration conflicts.
- Device identity conflicts.
- Payment terminal identity conflicts.
- Security-sensitive configuration conflicts.
- Any conflict where automatic resolution may cause data loss.

The review queue must allow authorised admin users to compare local and cloud versions, apply a permitted resolution, mark a conflict as reviewed, and leave an admin note.

All review actions must be audit logged.

## Admin UI Requirements

Conflicts must be visible in the admin UI.

Because Daxa POS is multi-location by default, the admin UI must support:

- A client-level sync/conflict dashboard across all locations.
- A location-level sync/conflict dashboard for a specific location and local server.

The dashboard should show:

- Current sync status.
- Last successful sync time.
- Open conflict count.
- Conflicts by location.
- Conflicts by entity type.
- Conflicts by severity.
- Conflicts by age.
- Conflicts requiring admin action.

Conflict severity should support:

- Info.
- Warning.
- Action Required.
- Critical.

## Relationship to OI-0006

OI-0006 is resolved by adopting the conflict handling rules described in this addendum.

The resolved answers are:

- Operational data is append-only and local-origin authoritative.
- Configuration data is cloud-authoritative.
- Reference/master data is cloud-authoritative.
- Device/runtime data is local-origin authoritative except for security-sensitive records.
- An admin review queue is required.
- Conflicts are surfaced through client-level and location-level admin dashboards.

## Consequence

This design keeps trading resilient while preserving data integrity.

Local venues can continue trading during outages.

The cloud can still provide backup, reporting, multi-location visibility, and configuration distribution.

Financial and tax history is protected by append-only records and immutable snapshots.

Admin users have a clear review path when a conflict cannot be safely resolved automatically.

## Status Update

Status: **Accepted**
