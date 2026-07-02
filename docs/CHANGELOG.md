# Documentation Changelog — Daxa POS

Changes are listed in reverse chronological order.

---

## 2026-07-03 — PLAN-0003 complete (Milestones A–H)

### Summary

PLAN-0003 (Identity, Tenancy, Locations, and Devices) is complete, covering the mixed-authentication identity/tenancy/device foundation for Daxa POS: fail-closed multi-tenant isolation, RBAC, local username/password login, `Organisation`/`Location`/`Terminal` management, device registration and credentials, `StaffMember` and staff PIN login, and consolidated offline/RBAC verification. This is a summary entry only — full milestone-by-milestone detail (files changed, migrations, deviations, test counts) lives in `docs/plans/active/PLAN-0003-worker-notes.md`, not repeated here. PLAN-0002 (Platform Skeleton) predates this changelog's coverage and is not backfilled.

### Key areas changed

- `src/DaxaPos.Domain`, `Application`, `Infrastructure`, `Persistence`, `Api` — identity/tenancy/device/staff entities, EF Core configurations and migrations, authentication handlers (`Session`, `DeviceToken`), `RequirePermissionFilter`, audit domain-event handlers, and the identity/tenancy/device endpoint groups (`organisations`, `locations`, `terminals`, `devices`, `device-registration(-pins)`, `staff-members`, `auth`).
- `tests/DaxaPos.UnitTests`, `tests/DaxaPos.Api.Tests` — 371 tests at Milestone G completion, including consolidated RBAC (`RbacTests.cs`), offline/hybrid verification (`HybridOfflineLoginTests.cs`), and a source-scan guard (`IgnoreQueryFiltersUsageTests.cs`).
- `docs/architecture/security.md`, `tenancy.md`, `multi-location.md`, `docs/modules/devices.md`, `audit.md` — updated incrementally through Milestones B–G, closed out with final rollup notes at Milestone H.
- `docs/testing/security-tests.md`, `testing-strategy.md`, `local-smoke-test.md` — implementation-status mapping and the adopted manual smoke-test walkthrough.

### ADRs applied

- [ADR-0013](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) — Cloud Identity and Local POS Authentication Strategy (the mixed-auth model this plan implements).
- [ADR-0015](../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md) — Tenant Isolation Mechanism and POS Session Token Format (proposed and accepted during this plan, 2026-07-01).
- [ADR-0016](../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) — Multi-Language and Localisation Strategy: proposed as a planning-only follow-up during this plan's Milestone D/E session, **not** part of PLAN-0003's implementation scope and not accepted or advanced by Milestone H.

### Open issues created

Six, all still open at PLAN-0003 completion — see `docs/issues/index.md`:

- OI-0011 — User Management Endpoints
- OI-0012 — Inactive Parent Lifecycle vs Device/Staff Authentication
- OI-0013 — DeviceRegistrationPin MaxUses Concurrency Race
- OI-0014 — Tenant-less Unauthenticated Security-Event Auditing
- OI-0015 — Permission Metadata for Staff-PIN Eligibility
- OI-0016 — Define Completed-Plan Archival Convention

### Tests / verification outcome at closeout

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 371/371 passed (82 unit tests + 289 API tests), against a real Postgres container with Keycloak stopped throughout, including a fresh-database migration re-verification (all six migrations apply cleanly in sequence). Milestone H itself added no code, migrations, or tests — documentation only.

### Follow-Up Items

- The six open issues above remain unresolved by design; none are closed by this plan.
- PLAN-0003 was **not** moved to `docs/plans/completed/` — it stays under `docs/plans/active/` pending a decision on the archival convention itself (OI-0016).
- PLAN-0004 (Catalog, Menu, Tax, Pricing) is the next active plan and can build directly on this foundation — see the "PLAN-0003 → PLAN-0004 Handoff" section in `docs/plans/active/PLAN-0003-worker-notes.md`.

---

## 2026-07-01

### Summary

- Applied all accepted ADR and resolved Open Issue decisions consistently across the documentation set.
- Fixed critical index errors: ADR index and Issues index were both completely out of date.
- Replaced all stale `adr/proposed/` links with `adr/accepted/` or `adr/superseded/` as appropriate.
- Replaced all stale `issues/open/` links with `issues/closed/`.
- Updated architecture, deployment, and module docs to reflect ADR-0013 authentication model.
- Expanded MANIFEST.md to cover all files in the docs directory.
- Created PLAN-docs-consolidation.md to track this work.

### Files Changed

- `docs/adr/index.md` — corrected Proposed/Accepted/Superseded sections, added ADR-0013
- `docs/issues/index.md` — all 10 OIs moved to Closed, status updated
- `docs/README.md` — removed stale "no accepted ADRs" claim, updated issues section, added PLAN-docs-consolidation
- `docs/architecture/security.md` — full rewrite to reflect ADR-0013 mixed auth model
- `docs/architecture/sync.md` — fixed `adr/accepted/` link
- `docs/architecture/tax-engine.md` — fixed `adr/accepted/` link
- `docs/modules/tax.md` — fixed `adr/accepted/` links
- `docs/deployment/local.md` — removed Keycloak from Docker Compose stack, updated hardware spec, updated OI-0003 link
- `docs/adr/superseded/ADR-0009-keycloak-or-identity-provider-strategy.md` — fixed typo "Superseeded" → "Superseded"
- `docs/issues/closed/OI-0008-cloud-data-region-strategy.md` — fixed Status field from "Open" to "Closed"
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md` — fixed OI links to `issues/closed/`
- `docs/issues/closed/OI-0001-first-payment-provider.md` — fixed ADR link
- `docs/issues/closed/OI-0002-identity-provider-local-cloud-hybrid.md` — fixed ADR link
- `docs/issues/closed/OI-0003-local-server-reference-hardware.md` — fixed ADR links
- `docs/issues/closed/OI-0005-first-payment-terminal-reference-device.md` — fixed ADR link
- `docs/issues/closed/OI-0006-hybrid-sync-conflict-rules.md` — fixed ADR links
- `docs/issues/closed/OI-0007-tax-configuration-editing-permissions.md` — fixed ADR links
- `docs/issues/closed/OI-0008-cloud-data-region-strategy.md` — fixed ADR links
- `docs/issues/closed/OI-0009-maui-app-update-delivery.md` — fixed ADR link
- `docs/issues/closed/OI-0010-local-keycloak-vs-cloud-keycloak.md` — fixed ADR links
- All remaining docs with `adr/proposed/` or `issues/open/` references — bulk replaced via sed
- `docs/MANIFEST.md` — expanded to cover all ~120 files
- `docs/CHANGELOG.md` — created (this file)
- `docs/plans/active/PLAN-docs-consolidation.md` — created

### Decisions Applied

- ADR-0013 — Cloud Identity and Local POS Authentication Strategy (supersedes ADR-0009)
- OI-0001 — First Payment Provider: Stripe Terminal selected
- OI-0002 — Identity Provider: resolved by ADR-0013
- OI-0003 — Local Server Reference Hardware: baseline defined
- OI-0004 — First Receipt Printer: Epson TM-T88VI selected
- OI-0005 — First Payment Terminal: Stripe BBPOS WisePOS E selected
- OI-0006 — Hybrid Sync Conflict Rules: category-based model adopted
- OI-0007 — Tax Configuration Permissions: manager-level + catalogue permission
- OI-0008 — Cloud Data Region: configurable per-tenant region strategy
- OI-0009 — MAUI App Updates: operator-controlled via Daxa Local server
- OI-0010 — Local Keycloak: resolved by ADR-0013, no local Keycloak for MVP

### Follow-Up Items

- None. No new ADRs or OIs were created during this pass.
