# Documentation Changelog — Daxa POS

Changes are listed in reverse chronological order.

---

## 2026-07-05 — PLAN-0004 Milestone H (consolidation, RBAC sweep, ADR-0016 acceptance, and PLAN-0004 closeout)

### Summary

Test-and-documentation-only closeout of PLAN-0004: no new entities, migrations, or endpoint groups. `RbacTests.cs`'s permission-gated endpoint inventory extended from PLAN-0003-only (29 routes) to include all 73 PLAN-0004 `rejectStaffPin: true` routes across Milestones C–G, driving the full 401/403/device-token/staff-PIN matrix automatically; the resolved-menu read added to the 401-only sweep. The sold-out toggle and resolved-menu read were deliberately excluded from the rejection inventory (they're the plan's two staff-accessible exceptions, each already proven staff-**succeeds** by its own dedicated test) rather than added incorrectly. Permission categories, role grants, endpoint registration (`Program.cs`), and the `IgnoreQueryFilters()` allowlist were all swept and found already correct — zero fixes needed for any of them. ADR-0016 (Multi-Language and Localisation Strategy) was accepted and moved from `proposed/` to `accepted/`, after confirming it is internally consistent, nothing in Milestones A–G contradicts it, and acceptance requires no schema/code work. `OI-0017` (product archive-and-replace concurrency race) was filed; the plan's other two candidate issues were evaluated and found already resolved (`VenueTaxConfiguration`-absence, decided at planning approval) or not yet warranted (menu merge-precedence revisit, no real usage exists to have surfaced a problem). PLAN-0004 is now marked complete, in place under `docs/plans/active/` (not relocated — `OI-0016`'s archival-convention question remains open, now spanning three finished plans).

### Key areas changed

- `tests/DaxaPos.Api.Tests/RbacTests.cs` (modified — 73 new endpoint rows, 1 resolved-menu 401-only row, a corrected stale doc comment).
- `docs/adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md` → `docs/adr/accepted/ADR-0016-multi-language-and-localisation-strategy.md` (moved; Status field updated; internal sibling-ADR links fixed).
- `docs/adr/index.md` (ADR-0016 moved to the Accepted table).
- `docs/issues/open/OI-0017-product-archive-and-replace-concurrency.md` (new); `docs/issues/index.md` (new area section); `docs/issues/open/OI-0016-define-completed-plan-archival-convention.md` (updated — PLAN-0004 now a third finished-but-not-relocated plan).
- `docs/README.md`, `docs/03-phase-roadmap.md`, `docs/architecture/overview.md`, `docs/architecture/tax-engine.md`, `docs/modules/tax.md`, `docs/modules/catalog.md`, `docs/modules/receipts.md`, `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`, `docs/plans/active/PLAN-localisation-multi-language.md` (ADR-0016 path/status references updated — historical point-in-time records in `PLAN-0003-worker-notes.md`/`PLAN-0003-identity-tenancy-locations-devices.md` deliberately left unchanged).
- `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md` (Milestone H sections, plan marked complete).

### Open issues resolved

None closed. `OI-0017` opened (see above).

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 944/944 passed (104 unit tests + 840 API tests, up from 577 at Milestone G close — 367 new test executions from the RBAC theory expansion, zero regressions), against real Postgres. `IgnoreQueryFiltersUsageTests` re-verified passing with the unchanged 5-file allowlist. All 12 migrations (unchanged count — no migration added this milestone) re-verified to apply cleanly in sequence from an empty database.

### ADR-0016

**Accepted 2026-07-05.** Moved from `docs/adr/proposed/` to `docs/adr/accepted/` after confirming internal consistency, no contradiction with anything Milestones A–G implemented, and that acceptance requires no further schema/code work — see the Milestone H Report in `PLAN-0004-worker-notes.md` for the full four-point confirmation.

### Next

**PLAN-0004 is complete.** PLAN-0005 (Payments, Receipts, Printing) is next per `docs/README.md`'s Active Plans ordering — its Order module is the first consumer of this plan's `TaxCalculationEngine`, `PriceResolver`, and resolved-menu output, and is where ADR-0006's per-order 20-tax-component limit (deferred throughout PLAN-0004) finally gets enforced.

---

## 2026-07-05 — PLAN-0004 Milestone G (menu construction and resolved-menu read endpoint)

### Summary

Implemented PLAN-0004 Milestone G only: `Menu`, `MenuSection`, `MenuSectionItem`, `MenuAvailabilityRule` with CRUD/assign-unassign endpoints, and the resolved-menu read endpoint (`GET /api/v1/menus/resolved?locationId={id}`) — the plan's other deliberately staff-accessible endpoint, gated `.RequireAuthorization()` only with no permission code at all (approved Human Decision #1), unlike the sold-out toggle's `Operational`-category permission. Configuration endpoints are `menus.manage` + `rejectStaffPin: true`. The resolved-menu endpoint merges organisation-wide and location-specific menus with location-specific winning on product conflict (approved Human Decision #7), applies `MenuAvailabilityRule` day/time filtering against the location's own local time (`Location.TimeZoneId`, a new column added this milestone), excludes sold-out/unavailable/inactive/archived products, and resolves prices via Milestone F's `PriceResolver` (no variant/modifier — those are order-time selections). Fails closed (404) when `VenueTaxConfiguration` is missing. This session was a recovery from an interrupted prior attempt: the partial work (8 files + 3 tracked edits) was inspected byte-for-byte, confirmed uncorrupted and fully buildable despite the crash-tail's claim otherwise, and preserved rather than rewritten. One migration (`AddMenus`). No order integration, payments, receipts, UI, sync, inventory, or KDS.

### Key areas changed

- `src/DaxaPos.Domain/Entities/Menu.cs`, `MenuSection.cs`, `MenuSectionItem.cs`, `MenuAvailabilityRule.cs` (new); `src/DaxaPos.Domain/Enums/DaysOfWeekMask.cs` (new); `src/DaxaPos.Domain/Events/MenuLifecycleDomainEvent.cs`, `MenuSectionLifecycleDomainEvent.cs`, `MenuSectionItemChangedDomainEvent.cs`, `MenuAvailabilityRuleChangedDomainEvent.cs` (new).
- `src/DaxaPos.Domain/Entities/Location.cs` (modified — new `TimeZoneId` column, default `"UTC"`); `src/DaxaPos.Persistence/Configurations/LocationConfiguration.cs` (modified — mapping).
- `src/DaxaPos.Api/Endpoints/Menus/MenuEndpoints.cs`, `MenuSectionEndpoints.cs`, `MenuSectionItemEndpoints.cs`, `MenuAvailabilityRuleEndpoints.cs`, `ResolvedMenuEndpoints.cs` (new).
- `src/DaxaPos.Persistence/Configurations/MenuConfiguration.cs`, `MenuSectionConfiguration.cs`, `MenuSectionItemConfiguration.cs`, `MenuAvailabilityRuleConfiguration.cs` (new); `DaxaDbContext.cs` (modified — 4 new `DbSet`s, 4 new fail-closed query filters).
- `src/DaxaPos.Persistence/Migrations/20260705102237_AddMenus.cs` (new).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` (modified — 4 new handlers); `Program.cs` (modified — DI registrations + 5 new endpoint mappings, none of which existed before this session).
- `tests/DaxaPos.Api.Tests/MenuEndpointsTests.cs`, `MenuSectionEndpointsTests.cs`, `MenuSectionItemEndpointsTests.cs`, `MenuAvailabilityRuleEndpointsTests.cs`, `ResolvedMenuEndpointsTests.cs` (new, 44 tests); `StaffPinLoginTests.cs` (modified — extended the shared staff-PIN-rejection inventory with the 4 `menus.manage` endpoints only, never the resolved-menu endpoint, proven staff-**succeeds** separately).
- `docs/modules/menus.md`, `docs/modules/catalog.md`, `docs/modules/pricing.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

None.

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 577/577 passed (104 unit tests + 473 API tests, up from 533 at Milestone F close — 44 new tests, zero regressions), against real Postgres. All 12 migrations verified to apply cleanly in sequence from an empty database (disposable throwaway database, then dropped).

### ADR-0016

Re-checked per this session's explicit instruction: still `docs/adr/proposed/`, not moved. `Menu.Name`/`MenuSection.Name` are the newest translatable-in-future columns, mapped per the plan's pre-recorded constraint; nothing here depended on its acceptance status.

### Next

PLAN-0004 Milestone H (consolidation, RBAC sweep, and documentation closeout — test-and-documentation-only) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

---

## 2026-07-05 — PLAN-0004 Milestone F (location overrides and pricing resolver)

### Summary

Implemented PLAN-0004 Milestone F only: `ProductLocationOverride` and `VenueTaxConfiguration` entities with CRUD endpoints, the pure `PriceResolver`, and the plan's first genuinely staff-accessible catalogue write — the sold-out toggle. Confirmed against the plan's exact permission table before implementation: `ProductLocationOverride`/`VenueTaxConfiguration` are gated `pricing.manage` + `rejectStaffPin: true` (not `catalog.manage`); the sold-out toggle is gated `catalog.sold-out-toggle` + `rejectStaffPin: false`, deliberately the opposite. The sold-out toggle may only ever touch `IsSoldOut`, upserts the override row, and additionally checks that a location-bound staff-PIN session's own location matches the target — a new check beyond organisation matching. `PriceResolver` (TDD'd first, the second genuinely financial logic in the codebase) resolves base price → variant delta → modifier deltas → location override (replaces the total outright) → tax-inclusive/exclusive mode from `VenueTaxConfiguration`, failing closed when that configuration is missing rather than silently defaulting. One migration (`AddLocationOverridesAndVenueTaxConfig`). No menus, order-price snapshotting, receipts, or UI.

### Key areas changed

- `src/DaxaPos.Domain/Entities/ProductLocationOverride.cs`, `VenueTaxConfiguration.cs` (new); `src/DaxaPos.Domain/Events/ProductLocationOverrideChangedDomainEvent.cs`, `VenueTaxConfigurationLifecycleDomainEvent.cs` (new).
- `src/DaxaPos.Application/Pricing/PriceResolutionModels.cs`, `PriceResolutionResult.cs`, `PriceResolver.cs` (new) — the pure resolver.
- `src/DaxaPos.Api/Endpoints/Catalog/ProductLocationOverrideEndpoints.cs`, `ProductSoldOutEndpoints.cs` (new); `src/DaxaPos.Api/Endpoints/Tax/VenueTaxConfigurationEndpoints.cs` (new, matching the plan's own file-location intent).
- `src/DaxaPos.Persistence/Configurations/ProductLocationOverrideConfiguration.cs`, `VenueTaxConfigurationConfiguration.cs` (new); `DaxaDbContext.cs` (modified — 2 new `DbSet`s, 2 new fail-closed query filters).
- `src/DaxaPos.Persistence/Migrations/20260705051120_AddLocationOverridesAndVenueTaxConfig.cs` (new).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` (modified — 2 new handlers); `Program.cs` (modified — DI registrations + endpoint mapping).
- `tests/DaxaPos.UnitTests/Pricing/PriceResolverTests.cs` (new, 12 tests, TDD'd first); `tests/DaxaPos.Api.Tests/ProductLocationOverrideEndpointsTests.cs`, `VenueTaxConfigurationEndpointsTests.cs`, `ProductSoldOutEndpointsTests.cs` (new, 32 tests); `StaffPinLoginTests.cs` (modified — extended the shared staff-PIN-rejection inventory with the `pricing.manage` endpoints only, never the sold-out toggle).
- `docs/modules/catalog.md`, `docs/modules/pricing.md`, `docs/modules/tax.md`, `docs/architecture/tax-engine.md`, `docs/architecture/multi-location.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

None.

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 533/533 passed (104 unit tests + 429 API tests, up from 489 at Milestone E close — 44 new tests, zero regressions), against real Postgres. All 11 migrations verified to apply cleanly in sequence from an empty database (disposable throwaway database, then dropped).

### ADR-0016

Re-checked per this session's explicit instruction: still `docs/adr/proposed/`, not moved. No new translatable-in-future text columns were added this milestone (`ProductLocationOverride`/`VenueTaxConfiguration` carry no `Name`-like fields), so nothing here depended on its acceptance status.

### Next

PLAN-0004 Milestone G (menu construction and the resolved-menu read endpoint — the plan's second and last deliberately staff-accessible endpoint, this time with no permission code at all) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

---

## 2026-07-05 — PLAN-0004 Milestone E (product variants and modifiers)

### Summary

Implemented PLAN-0004 Milestone E only: `ProductVariant`, `ModifierGroup`, `Modifier`, and `ProductModifierGroup` (attach/detach only) with CRUD endpoints (or, for the join, assign/unassign endpoints). `ProductVariant`/`Modifier` carry no `OrganisationId` column of their own — scoped through `ProductId`/`ModifierGroupId` respectively, matching the `Terminal`-through-`Location` precedent. `PriceDelta` on both entities may be positive, zero, or negative (a delta on the resolved base price, not an absolute amount) — deliberately not validated with `Product.BasePrice`'s `>= 0` rule. `ProductModifierGroup` has only assign/unassign, no list/read/update/archive lifecycle. One migration (`AddVariantsAndModifiers`). No pricing resolver, menus, order integration, or UI.

### Key areas changed

- `src/DaxaPos.Domain/Entities/ProductVariant.cs`, `ModifierGroup.cs`, `Modifier.cs`, `ProductModifierGroup.cs` (new); `src/DaxaPos.Domain/Events/ProductVariantLifecycleDomainEvent.cs`, `ModifierGroupLifecycleDomainEvent.cs`, `ModifierLifecycleDomainEvent.cs`, `ProductModifierGroupChangedDomainEvent.cs` (new).
- `src/DaxaPos.Api/Endpoints/Catalog/ProductVariantEndpoints.cs`, `ModifierGroupEndpoints.cs`, `ModifierEndpoints.cs`, `ProductModifierGroupEndpoints.cs` (new).
- `src/DaxaPos.Persistence/Configurations/ProductVariantConfiguration.cs`, `ModifierGroupConfiguration.cs`, `ModifierConfiguration.cs`, `ProductModifierGroupConfiguration.cs` (new); `DaxaDbContext.cs` (modified — 4 new `DbSet`s, 4 new fail-closed query filters).
- `src/DaxaPos.Persistence/Migrations/20260705041146_AddVariantsAndModifiers.cs` (new).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` (modified — 4 new handlers); `Program.cs` (modified — DI registrations + endpoint mapping).
- `tests/DaxaPos.Api.Tests/ProductVariantEndpointsTests.cs`, `ModifierGroupEndpointsTests.cs`, `ModifierEndpointsTests.cs`, `ProductModifierGroupEndpointsTests.cs` (new, 44 tests); `StaffPinLoginTests.cs` (modified — extended the shared staff-PIN-rejection endpoint inventory with the 5 new catalogue endpoints).
- `docs/modules/catalog.md`, `docs/modules/pricing.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

None.

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 489/489 passed (92 unit tests + 397 API tests, up from 445 at Milestone D close — 44 new tests, zero regressions), against real Postgres. All 10 migrations verified to apply cleanly in sequence from an empty database (disposable throwaway database, then dropped).

### ADR-0016

Re-checked per this session's explicit instruction: still `docs/adr/proposed/`, not moved. `ProductVariant.Name`, `ModifierGroup.Name`, and `Modifier.Name` are new translatable-in-future columns this milestone adds; mapped as plain invariant/fallback `varchar` columns per the plan's pre-recorded ADR-0016 constraint, so nothing here depended on its acceptance status.

### Next

PLAN-0004 Milestone F (location-level catalog overrides and the pricing resolver: `ProductLocationOverride`, `VenueTaxConfiguration`, `PriceResolver`, plus the sold-out toggle — the plan's first genuinely staff-accessible write endpoint) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

---

## 2026-07-05 — PLAN-0004 Milestone D (product catalogue foundation)

### Summary

Implemented PLAN-0004 Milestone D only: `ProductCategory` and `Product` entities with CRUD endpoints, including OI-0007's archive-and-replace behaviour for `TaxCategoryId`-changing `Product` updates. `ProductCategoryEndpoints` follows the standard six-endpoint CRUD-sextet shape. `ProductEndpoints` branches its `PATCH` handler: an unchanged `TaxCategoryId` updates in place (200 OK); a changed one archives the current row and creates a replacement (201 Created), linked via `SupersededByProductId`. Archived rows are permanent — no further writes are accepted against them. The documented two-simultaneous-edits concurrency race is accepted as an MVP risk (matching OI-0013), no row-locking added. One migration (`AddProductCatalogueFoundation`). No variants, modifiers, pricing resolver, menus, order integration, or UI.

### Key areas changed

- `src/DaxaPos.Domain/Entities/ProductCategory.cs`, `Product.cs` (new); `src/DaxaPos.Domain/Events/ProductCategoryLifecycleDomainEvent.cs`, `ProductLifecycleDomainEvent.cs` (new).
- `src/DaxaPos.Api/Endpoints/Catalog/ProductCategoryEndpoints.cs`, `ProductEndpoints.cs` (new).
- `src/DaxaPos.Persistence/Configurations/ProductCategoryConfiguration.cs`, `ProductConfiguration.cs` (new); `DaxaDbContext.cs` (modified — 2 new `DbSet`s, 2 new fail-closed query filters).
- `src/DaxaPos.Persistence/Migrations/20260705034151_AddProductCatalogueFoundation.cs` (new).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` (modified — 2 new handlers); `Program.cs` (modified — DI registrations + endpoint mapping).
- `tests/DaxaPos.Api.Tests/ProductCategoryEndpointsTests.cs`, `ProductEndpointsTests.cs` (new, 27 tests); `StaffPinLoginTests.cs` (modified — extended the shared staff-PIN-rejection endpoint inventory with the 4 new catalogue endpoints).
- `docs/modules/catalog.md`, `docs/modules/tax.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

None. The archive-and-replace concurrency race is flagged for a new open issue at Milestone H (per the plan's Open Issues Required section), not opened this milestone.

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 445/445 passed (92 unit tests + 353 API tests, up from 418 at Milestone C close — 27 new catalogue tests, zero regressions), against real Postgres. All 9 migrations verified to apply cleanly in sequence from an empty database (disposable throwaway database, then dropped).

### ADR-0016

Re-checked per this session's explicit instruction: still `docs/adr/proposed/`, not moved. `Product.Name`/`Description` and `ProductCategory.Name` are new translatable-in-future columns this milestone adds; mapped as plain invariant/fallback `varchar` columns per the plan's pre-recorded ADR-0016 constraint (no `{Entity}Translation` tables, no culture-resolution logic), so nothing here depended on its acceptance status.

### Next

PLAN-0004 Milestone E (product variants and modifiers: `ProductVariant`, `ModifierGroup`, `Modifier`, `ProductModifierGroup`) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

---

## 2026-07-05 — PLAN-0004 Milestone C (tax configuration endpoints)

### Summary

Implemented PLAN-0004 Milestone C only: tax configuration API endpoints on top of Milestone B's entities. `TaxDefinitionTemplateEndpoints` (read-only template listing), `TaxDefinitionEndpoints` (create from-scratch or from-template, list, get, update, deactivate, reactivate), `TaxCategoryEndpoints` (same six-endpoint shape), `TaxCategoryDefinitionEndpoints` (create, list, hard delete — a pure mapping row, not a financial record). No schema changes, no product/menu/pricing entities, no order integration, no UI. Every endpoint is gated `catalog.manage` + `rejectStaffPin: true` (OI-0007); every write raises a lifecycle domain event audited with a before/after JSON snapshot. The 10-component-per-line design limit (ADR-0006) is enforced at `TaxCategoryDefinition` creation time.

### Key areas changed

- `src/DaxaPos.Domain/Events/TaxDefinitionLifecycleDomainEvent.cs`, `TaxCategoryLifecycleDomainEvent.cs`, `TaxCategoryDefinitionChangedDomainEvent.cs` (new).
- `src/DaxaPos.Api/Endpoints/Tax/TaxDefinitionTemplateEndpoints.cs`, `TaxDefinitionEndpoints.cs`, `TaxCategoryEndpoints.cs`, `TaxCategoryDefinitionEndpoints.cs` (new).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` (modified — 3 new handlers); `Program.cs` (modified — DI registrations + endpoint mapping).
- `tests/DaxaPos.Api.Tests/TaxDefinitionEndpointsTests.cs`, `TaxCategoryEndpointsTests.cs`, `TaxCategoryDefinitionEndpointsTests.cs` (new, 35 tests); `StaffPinLoginTests.cs` (modified — extended the shared staff-PIN-rejection endpoint inventory with the 7 new tax endpoints).
- `docs/modules/tax.md`, `docs/architecture/tax-engine.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/plans/active/PLAN-0004-worker-notes.md`.

### Open issues resolved

None. (OI-0007 was already closed; this milestone directly implements its Decision, per the plan's Open Issues Required section — not reopened or re-litigated.)

### Tests / verification outcome

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors. `dotnet test DaxaPos.sln` — 418/418 passed (92 unit tests + 326 API tests, up from 383 at Milestone B close — 35 new tax-configuration tests, zero regressions), against real Postgres. No migration added (Milestone B's schema already covered this milestone's needs); the existing 8 migrations were not touched.

### ADR-0016

Re-checked per this session's explicit instruction: still `docs/adr/proposed/`, not moved. No new translatable-in-future columns were added this milestone (no schema changes at all), so nothing here depended on its acceptance status.

### Next

PLAN-0004 Milestone D (product catalogue foundation: `ProductCategory`, `Product`, archive-and-replace on tax-category-changing updates per OI-0007) — see `docs/plans/active/PLAN-0004-worker-notes.md` for the recommended next-session prompt.

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
