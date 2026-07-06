# Open Issue Index — Daxa POS

All Open Issues that require human decision before closing are listed here.
Issues that have been decided are moved to `issues/closed/` and listed under Closed Issues below.
Reviewed during PLAN-0011 planning on 2026-07-07 and again after PLAN-0011 implementation later
the same day; the local demo setup helper introduced no new unresolved product or architecture
issue. Implementation surfaced one implementation-surface gap (no way to detect an inactive
location by name via the current `LocationEndpoints` API surface) recorded in
`docs/plans/active/PLAN-0011-worker-notes.md` for a future worker touching that area, not filed
here as it is not a product/architecture decision.

---

## Open Issues

Grouped by area. Five were opened by PLAN-0003 Milestone G (2026-07-03), converting deferred risks recorded in the Milestone C–F worker notes into tracked issues. A sixth, OI-0016, was opened by PLAN-0003 Milestone H (2026-07-03) closeout, recording a process question raised while deciding whether to relocate the plan document itself. One of the original six, OI-0015, was closed by PLAN-0004 Milestone A (2026-07-03) — see Closed Issues below. A seventh, OI-0017, was opened by PLAN-0004 Milestone H (2026-07-05) closeout, converting the archive-and-replace concurrency race (deferred since Milestone D) into a tracked issue — the plan's other two candidate issues (`VenueTaxConfiguration`-absence behaviour, menu merge-precedence revisit) were evaluated and found not to need filing; see the Milestone H report in `PLAN-0004-worker-notes.md`. An eighth, OI-0018, was opened by PLAN-0005 Milestone F (2026-07-06) closeout, converting the location-scoped production printer routing follow-up (deferred since Milestone E) into a tracked issue.

### Identity / Security

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0011](open/OI-0011-user-management-endpoints.md) | User Management Endpoints | PLAN-0003 (permission seeded, endpoints never scoped into a milestone) |
| [OI-0012](open/OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md) | Inactive Parent Lifecycle vs Device/Staff Authentication | PLAN-0003 Milestones D/E |

### Devices

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0013](open/OI-0013-device-registration-pin-maxuses-concurrency.md) | DeviceRegistrationPin MaxUses Concurrency Race | PLAN-0003 Milestone E |

### Audit

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0014](open/OI-0014-tenantless-security-event-auditing.md) | Tenant-less Unauthenticated Security-Event Auditing | PLAN-0003 Milestones C/E |

### Catalog / Data Integrity

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0017](open/OI-0017-product-archive-and-replace-concurrency.md) | Product Archive-and-Replace Concurrency Race | PLAN-0004 Milestone D |

### Printing / Devices

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0018](open/OI-0018-location-scoped-production-printer-routing.md) | Location-Scoped Production Printer Routing | PLAN-0005 Milestone E |

### Documentation / Process

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0016](open/OI-0016-define-completed-plan-archival-convention.md) | Define Completed-Plan Archival Convention | PLAN-0003 Milestone H |

---

## Closed Issues

| Issue | Title | Area | Resolved By |
|-------|-------|------|-------------|
| [OI-0001](closed/OI-0001-first-payment-provider.md) | First Payment Provider | Payments | Stripe Terminal selected |
| [OI-0002](closed/OI-0002-identity-provider-local-cloud-hybrid.md) | Identity Provider for Local, Cloud, Hybrid | Identity / Security | ADR-0013 |
| [OI-0003](closed/OI-0003-local-server-reference-hardware.md) | Local Server Reference Hardware | Deployment / Hardware | Hardware baseline defined |
| [OI-0004](closed/OI-0004-first-receipt-printer-reference-device.md) | First Receipt Printer Reference Device | Devices / Hardware | Epson TM-T88VI selected |
| [OI-0005](closed/OI-0005-first-payment-terminal-reference-device.md) | First Payment Terminal Reference Device | Devices / Hardware / Payments | Stripe BBPOS WisePOS E selected |
| [OI-0006](closed/OI-0006-hybrid-sync-conflict-rules.md) | Hybrid Sync Conflict Rules | Sync | Category-based conflict rules adopted |
| [OI-0007](closed/OI-0007-tax-configuration-editing-permissions.md) | Tax Configuration Editing Permissions | Tax / Identity / Security | Manager-level + catalogue permission |
| [OI-0008](closed/OI-0008-cloud-data-region-strategy.md) | Cloud Data Region Strategy | Architecture / Deployment | Configurable per-tenant region strategy |
| [OI-0009](closed/OI-0009-maui-app-update-delivery.md) | MAUI App Update Delivery | Devices / Deployment | Operator-controlled via Daxa Local server |
| [OI-0010](closed/OI-0010-local-keycloak-vs-cloud-keycloak.md) | Local Keycloak vs Cloud Keycloak | Identity / Deployment | ADR-0013 — no local Keycloak for MVP |
| [OI-0015](closed/OI-0015-permission-metadata-for-staff-pin-eligibility.md) | Permission Metadata for Staff-PIN Eligibility | Identity / Security | `Permission.Category` enum, PLAN-0004 Milestone A |
