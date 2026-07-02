# Open Issue Index — Daxa POS

All Open Issues that require human decision before closing are listed here.
Issues that have been decided are moved to `issues/closed/` and listed under Closed Issues below.

---

## Open Issues

Grouped by area. All five were opened by PLAN-0003 Milestone G (2026-07-03), converting deferred risks recorded in the Milestone C–F worker notes into tracked issues.

### Identity / Security

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0011](open/OI-0011-user-management-endpoints.md) | User Management Endpoints | PLAN-0003 (permission seeded, endpoints never scoped into a milestone) |
| [OI-0012](open/OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md) | Inactive Parent Lifecycle vs Device/Staff Authentication | PLAN-0003 Milestones D/E |
| [OI-0015](open/OI-0015-permission-metadata-for-staff-pin-eligibility.md) | Permission Metadata for Staff-PIN Eligibility | PLAN-0003 Milestone F (Decision 8) |

### Devices

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0013](open/OI-0013-device-registration-pin-maxuses-concurrency.md) | DeviceRegistrationPin MaxUses Concurrency Race | PLAN-0003 Milestone E |

### Audit

| Issue | Title | Deferred from |
|-------|-------|---------------|
| [OI-0014](open/OI-0014-tenantless-security-event-auditing.md) | Tenant-less Unauthenticated Security-Event Auditing | PLAN-0003 Milestones C/E |

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
