# OI-0006 — Hybrid Sync Conflict Rules

## Status

Closed

## Area

Sync

## Summary

What are the rules for resolving conflicts when data is modified both locally (on a Daxa Local server) and in the cloud before sync occurs?

## Context

In Daxa Hybrid deployments, data flows in both directions: orders and operational data flow from local to cloud; configuration (menus, pricing, tax) flows from cloud to local. If the same record is modified in both places before sync completes, a conflict occurs.

ADR-0007 states conflicts must be explicit — not silently overwritten. The specific rules for how conflicts are detected, surfaced, and resolved are not yet defined.

## Impact

- Determines the conflict detection and resolution implementation in Daxa Sync.
- Affects the data model (`SyncConflict` table structure).
- Affects the admin UI (conflict review and resolution workflow).
- Affects financial record integrity.

## Options

1. **Last-write-wins** — Simplest. High risk of silent data loss, especially for financial records. Rejected.
2. **Cloud wins for config, local wins for operational data** — Reasonable default. Configuration (menus, pricing) always defers to cloud master. Operational data (orders, payments) always comes from local.
3. **Explicit conflict queue, human review** — All conflicts go into a `SyncConflict` review queue. Requires admin action to resolve.
4. **Operational data never conflicts (append-only)** — Orders and payments are append-only and never conflict by nature. Config conflicts use cloud-wins rule. Most conflicts are therefore self-resolving.

## Recommendation

Option 4 with Option 2 for config: define operational records (orders, payments, refunds, audit events) as append-only (no conflicts possible). Define configuration records as cloud-master (cloud wins). Any residual conflicts go to explicit review queue with alert.

## Decision Needed

- Conflict resolution rules per data category (operational vs config).
- Whether an admin review queue is required.
- How conflicts are surfaced to admins.

## Related ADRs

- [ADR-0007 — Local/Hybrid Sync Principles](../../adr/accepted/ADR-0007-local-hybrid-sync-principles.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)

## Related Documents

- [Architecture: Sync](../../architecture/sync.md)
- [Module: Sync](../../modules/sync.md)
- [PLAN-0007 — Sync, Local, Hybrid](../../plans/active/PLAN-0007-sync-local-hybrid-planning.md)

---

## Decision

Hybrid sync conflict handling will use a **category-based conflict resolution model**.

The default rules are:

- **Operational data is append-only and local-origin authoritative.**
- **Configuration data is cloud-authoritative.**
- **Residual or unsafe conflicts are sent to an admin review queue.**
- **Conflicts are surfaced in the admin UI at both client and location level.**

This follows the recommendation in ADR-0007 that conflicts must be explicit and must not be silently overwritten.

## Conflict Resolution Rules

### Operational Data

Operational data includes records created by day-to-day trading activity, such as:

- Orders.
- Order lines.
- Payments.
- Refunds.
- Voids.
- Cash drawer events.
- Staff actions.
- Audit events.
- Receipt records.
- Ledger entries.

Operational records should be treated as **append-only**.

Operational records should not be updated in-place after they have been committed. If a correction is required, the system should create a new correction, reversal, refund, void, or adjustment record rather than mutating the original record.

For hybrid sync, the local server is authoritative for operational data created while trading locally.

The cloud should accept local operational records using idempotency keys to prevent duplicate creation during retries.

If the same operational event is received more than once, the cloud must treat it as a retry and not create a duplicate record.

Operational data conflicts should be rare by design because operational records are not edited in-place.

If an operational conflict is detected, it must not be automatically resolved unless the conflict is clearly an idempotent retry. Unsafe operational conflicts must go to the admin review queue.

### Configuration Data

Configuration data includes records that control how the POS operates, such as:

- Menus.
- Products.
- Categories.
- Modifiers.
- Prices.
- Tax definitions.
- Product tax assignments.
- Surcharges.
- Discounts.
- Staff permissions.
- Device configuration.
- Location settings.
- Kitchen routing.
- Receipt templates.

For Daxa Hybrid deployments, the cloud is authoritative for configuration data.

Configuration changes should normally be made in the cloud admin system and then synced down to each local server.

If the same configuration record is modified in both cloud and local before sync completes, the cloud version wins by default.

The local change must not be silently discarded. The conflict should be recorded in the sync conflict log, including the local value, cloud value, timestamps, user/device details, and the rule used to resolve the conflict.

If a configuration change affects trading-critical data, such as tax, price, or product availability, the system should preserve historical correctness by using versioned or archived records rather than mutating records used by historical transactions.

### Reference and Master Data

Reference and master data includes records such as:

- Client records.
- Location records.
- Region records.
- Tenant configuration.
- Licence data.
- Identity provider mappings.

Reference and master data is cloud-authoritative.

Local servers may cache this data, but they must not become the source of truth for it.

If a conflict occurs, the cloud version wins and the conflict is recorded.

### Device and Local Runtime Data

Device and local runtime data includes records such as:

- Local device registration.
- Terminal pairing state.
- Printer discovery state.
- Kitchen display station state.
- Local server health.
- Sync agent status.

This data is local-origin authoritative where it represents local hardware or runtime state.

The cloud may store a reported copy for monitoring and support, but it should not overwrite the actual local runtime state unless the action is an explicit admin command.

Conflicts in this category should generally be resolved by latest reported state, unless the conflict relates to device identity or security. Device identity and security conflicts must go to admin review.

## Admin Review Queue

An admin review queue is required.

Most conflicts should be prevented by design through append-only operational records, cloud-authoritative configuration, idempotency keys, and immutable historical snapshots.

However, an admin review queue is still good practice because some conflicts should not be automatically resolved.

The review queue should be used when:

- The conflict involves financial records and cannot be safely classified as an idempotent retry.
- The conflict involves tax configuration or historical reporting integrity.
- The conflict involves device identity, payment terminal identity, or security-sensitive configuration.
- The system cannot safely determine which side is authoritative.
- Automatic resolution would cause data loss.
- A local change was overridden by cloud configuration and should be visible to the client.
- Manual admin acknowledgement is required for audit reasons.

The review queue should allow authorised admin users to:

- View the conflict.
- Compare local and cloud values.
- See which rule was applied automatically, if any.
- Accept the cloud version.
- Accept the local version where permitted.
- Create a corrected replacement record.
- Mark the conflict as reviewed.
- Add an admin note.

The review action must be audit logged.

## Admin UI Surfacing

Conflicts must be surfaced through the admin UI.

Because Daxa POS is multi-location by default, conflict visibility must support both:

- **Client-level conflict dashboard** — shows conflicts across all locations for a client.
- **Location-level conflict dashboard** — shows conflicts for one specific location/server.

The admin UI should include a sync/conflict dashboard with:

- Open conflict count.
- Conflicts by location.
- Conflicts by entity type.
- Conflicts by severity.
- Conflicts by sync direction.
- Conflicts by age.
- Last successful sync time per location.
- Current sync status per location.
- Retry/failure status.
- Review status.

Conflict severity should be classified as:

- **Info** — conflict was automatically resolved and only requires visibility.
- **Warning** — conflict was resolved by rule but should be reviewed.
- **Action Required** — admin review is required before the conflict is considered resolved.
- **Critical** — financial, tax, security, or device identity conflict requiring prompt admin action.

The dashboard should allow filtering by:

- Client.
- Location.
- Local server.
- Entity type.
- Severity.
- Status.
- Date/time range.

## SyncConflict Data Requirements

The sync conflict record should capture enough information to support audit, review, debugging, and support.

A `SyncConflict` record should include:

- Conflict ID.
- Client ID.
- Location ID.
- Local server ID.
- Entity type.
- Entity ID.
- Sync direction.
- Conflict type.
- Severity.
- Local value snapshot.
- Cloud value snapshot.
- Local timestamp.
- Cloud timestamp.
- Local user ID, where known.
- Cloud user ID, where known.
- Device ID, where known.
- Resolution status.
- Resolution rule applied.
- Resolved by user ID, where manually resolved.
- Resolved timestamp.
- Admin notes.
- Audit log reference.

## Outcome

The hybrid sync conflict rules are resolved as follows:

- Operational data is append-only and local-origin authoritative.
- Configuration data is cloud-authoritative.
- Reference/master data is cloud-authoritative.
- Device/runtime data is local-origin authoritative except for security-sensitive records.
- Idempotency keys prevent duplicate records during retry.
- Unsafe conflicts are not silently overwritten.
- An admin review queue is required.
- Conflicts are surfaced through client-level and location-level admin dashboards.
- All conflict detection, automatic resolution, manual review, and final resolution are audit logged.

## Status Update

This open issue is resolved by adopting category-based sync conflict rules with an explicit admin review queue and multi-location-aware conflict dashboard.

Status: **Closed**
