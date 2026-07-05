# Documentation Changelog — Daxa POS

Changes are listed in reverse chronological order.

---

## 2026-07-04 — PLAN-0004 Milestone B (tax foundation entities and pure calculation engine)

### Summary

Implemented PLAN-0004 Milestone B only: the tax data model and a pure, DB-independent tax calculation engine. Added `TaxDefinitionTemplate` (global, unfiltered, 5 AU/NZ rows seeded), `TaxDefinition` (tenant-owned, clonable from a template), `TaxCategory` (tenant-owned semantic label), and `TaxCategoryDefinition` (tenant-owned join, optionally location-scoped) — no tax configuration endpoints yet (Milestone C). `TaxCalculationEngine.CalculateLine` supports tax-inclusive/exclusive calculation, mixed baskets, configurable rounding, and fails closed on missing tax configuration rather than silently returning zero tax. TDD throughout; reproduces the CLAUDE.md/ADR-0006 AU mixed-basket worked example byte-for-byte. No product/catalog/menu/pricing entities, order-line tax snapshots, or endpoints were added.

### Key areas changed

- `src/DaxaPos.Domain/Enums/TaxJurisdictionType.cs`, `TaxRoundingMode.cs`, `TaxCalculationScope.cs`, `TaxTreatment.cs` (new); `Entities/TaxDefinitionTemplate.cs`, `TaxDefinition.cs`, `TaxCategory.cs`, `TaxCategoryDefinition.cs` (new).
- `src/DaxaPos.Application/Tax/TaxCalculationModels.cs`, `TaxLineCalculationResult.cs`, `TaxCalculationEngine.cs` (new) — the pure engine.
- `src/DaxaPos.Persistence/Seed/TaxSeedIds.cs`, `Configurations/TaxDefinitionTemplateConfiguration.cs`, `TaxDefinitionConfiguration.cs`, `TaxCategoryConfiguration.cs`, `TaxCategoryDefinitionConfiguration.cs` (new); `DaxaDbContext.cs` (modified — 4 new `DbSet`s, 3 new fail-closed query filters).
- `src/DaxaPos.Persistence/Migrations/20260704120431_AddTaxFoundation.cs` (new).
- `tests/DaxaPos.UnitTests/Tax/TaxCalculationEngineTests.cs` (new, 10 tests).
- `docs/modules/tax.md`, `docs/architecture/tax-engine.md`, `docs/testing/tax-tests.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

None. (OI-0015 was closed by Milestone A, not this milestone.)

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 383/383 passed (92 unit tests + 291 API tests, up from 373 at Milestone A close), against real Postgres. All 8 migrations verified to apply cleanly in sequence from an empty database (disposable throwaway database, not the shared dev database).

### ADR-0016

Re-checked per this session's explicit instruction: still `docs/adr/proposed/`, not moved. Milestone B's new `Name`/`ReceiptMarkerLabel` columns are already mapped as plain invariant/fallback text per the plan's pre-recorded ADR-0016 constraint, so nothing here depended on its acceptance status.

### Next

PLAN-0004 Milestone C (tax configuration endpoints: `TaxDefinitionTemplateEndpoints`, `TaxDefinitionEndpoints`, `TaxCategoryEndpoints`, `TaxCategoryDefinitionEndpoints`, all `catalog.manage` + `rejectStaffPin: true`) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

---

## 2026-07-03 — PLAN-0004 Milestone A (permission metadata, closes OI-0015)

### Summary

Implemented PLAN-0004 Milestone A only: permission metadata for staff-PIN eligibility. Added a required `Permission.Category` (`Operational`/`AdminSensitive`) column, refactored the staff-PIN login guard in `AuthEndpoints.StaffPinLoginAsync` to read it instead of the hard-coded `Permissions.AdminSensitive` list (deleted), and added 4 new permission codes: `catalog.manage`, `pricing.manage`, `menus.manage` (all `AdminSensitive`), and `catalog.sold-out-toggle` (`Operational` — the first permission code ever granted to the `Staff` role). Closes OI-0015. No catalog/menu/tax/pricing domain entities were added — those are Milestones B onward.

### Key areas changed

- `src/DaxaPos.Domain/Enums/PermissionCategory.cs` (new), `Entities/Permission.cs` (new `Category` property).
- `src/DaxaPos.Persistence/Configurations/PermissionConfiguration.cs`, `RolePermissionConfiguration.cs`, `Seed/RbacSeedIds.cs` — seed data for the 4 new permissions and their role grants (`SystemAdmin`/`OrganisationOwner`/`VenueManager` get all 4; `Staff` gets only `catalog.sold-out-toggle`).
- `src/DaxaPos.Persistence/Migrations/20260703120121_AddPermissionCategory.cs` — adds the column, backfills the 8 pre-existing permissions to `AdminSensitive`, inserts the 4 new permissions and their role grants.
- `src/DaxaPos.Application/Identity/Permissions.cs` — added 4 new code constants, deleted the `AdminSensitive` hard-coded set.
- `src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs` — staff-PIN login guard now checks `Permission.Category` instead of the deleted list.
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — 2 new tests (`Login_WhenAssignedRoleGrantsOnlyOperationalPermissions_Succeeds`, `PermissionCatalogue_ClassifiesPLAN0004MilestoneAPermissions_ByCategory`); one existing test updated to list explicit permission codes instead of the deleted `Permissions.AdminSensitive`.
- `docs/issues/closed/OI-0015-permission-metadata-for-staff-pin-eligibility.md` (moved from `open/`), `docs/issues/index.md`, `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

- OI-0015 — Permission Metadata for Staff-PIN Eligibility: resolved by `Permission.Category`, per the plan's Option 1 recommendation.

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 373/373 passed (82 unit tests + 291 API tests), against real Postgres. All 7 migrations (6 from PLAN-0003 + this one) verified to apply cleanly in sequence from an empty database (checked against a disposable throwaway database, not the shared dev database).

### Next

PLAN-0004 Milestone B (tax foundation: entities, templates, and the pure tax calculation engine) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

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
