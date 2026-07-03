# PLAN-0004 — Catalog, Menu, Tax, and Pricing

## Status

Draft — planning pass complete 2026-07-03, awaiting human approval. No code, no migrations, no `src/` changes. Blocked on committed PLAN-0003 state (commits through `a451482`, PLAN-0003 closed out).

## Goal

Implement the product catalogue, menu management, tax engine, and pricing engine — the core product data layer that will drive the POS sales screen (order entry, built in PLAN-0005+, is not part of this plan).

## Scope

- Tax definitions (jurisdictional rate/rounding/marker configuration) and tax categories (product-facing assignment labels), tenant-editable per OI-0007.
- A pure, DB-independent tax calculation engine (per-line, snapshot-shaped output), unit-tested against AU/NZ mixed baskets.
- Product categories, products, product variants, modifier groups, modifiers.
- Product tax category assignment, with OI-0007's archive-and-replace behaviour for tax-affecting changes.
- Location-level product availability, sold-out toggle, and price override (default-and-override per ADR-0003).
- A pure pricing resolver (base price → variant/modifier deltas → location override).
- Menus, menu sections, menu section items, time/day availability rules, location-specific menu assignment.
- A read-only "resolved menu" projection endpoint — the first endpoint in the system a staff-PIN session must be able to call, since it is how a POS operator sees what to sell.
- Closing OI-0015 (permission metadata for staff-PIN eligibility) as part of this plan's first milestone, per OI-0015's own recommended timing.

## Non-goals

- Orders, order lines, or anything that persists a sale (PLAN-0005).
- Payments, refunds, receipts, printing (PLAN-0005).
- Stripe Terminal or any payment adapter (PLAN-0009).
- Sync/offline/hybrid replication (PLAN-0007).
- Any UI (MAUI, PWA, admin, KDS) — this plan is API + domain + persistence only.
- KDS routing.
- Surcharges (`docs/modules/surcharges.md`) — Phase 2 per the phase roadmap; not touched by this plan. `SurchargeRule`/`OrderSurcharge` are out of scope.
- Full pricing rule engine: time-based/day-based/happy-hour/public-holiday pricing, customer-group pricing, promotions, bundles — Phase 2 (`docs/modules/pricing.md` Responsibilities (Phase 2+)).
- Advanced inventory/BOM, purchase orders, serial tracking.
- Implementing ADR-0016 (multi-language/localisation) — only its schema/naming *constraints* are honoured (invariant-name columns, no hard-coded label strings), no `{Entity}Translation` tables, no culture resolution logic, no second shipped language.
- Any new `DaxaPos.Modules.*` project — this plan follows PLAN-0003's precedent exactly: catalog/tax/pricing/menu code lives directly in `Domain`/`Application`/`Infrastructure`/`Persistence`/`Api`, no new project, no ADR-0014 reference-graph change.

## Context Read

Full list read before writing this plan (see `PLAN-0004-worker-notes.md` for what each contributed):

- `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md` (final, all milestones) and `PLAN-0003-worker-notes.md` (all milestone reports A–H)
- `docs/adr/accepted/ADR-0003-multi-location-by-default.md`
- `docs/adr/accepted/ADR-0006-tax-line-based-tax-engine.md`
- `docs/adr/accepted/ADR-0010-financial-records-ledger-and-audit.md`
- `docs/adr/accepted/ADR-0011-receipt-tax-marker-strategy.md`
- `docs/adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md`
- `docs/adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md`
- `docs/architecture/tax-engine.md`, `docs/architecture/multi-location.md`
- `docs/modules/catalog.md`, `menus.md`, `tax.md`, `pricing.md`, `surcharges.md`
- `docs/testing/tax-tests.md`, `docs/testing/security-tests.md`
- `docs/issues/closed/OI-0007-tax-configuration-editing-permissions.md`
- `docs/issues/open/OI-0015-permission-metadata-for-staff-pin-eligibility.md`
- Current source: `Permissions.cs`, `RequirePermissionFilter.cs`, `DaxaDbContext.cs`, `AuthContext.cs`, `LocationEndpoints.cs`, `AuditEvent.cs`, `RbacTestSeeder.cs`, `Location.cs` (as the entity-shape template)

## Files Likely To Change

No new projects (see Non-goals). All new code lands in the existing five projects, same as PLAN-0003:

```
src/DaxaPos.Domain/Entities/            (new: TaxDefinitionTemplate, TaxDefinition, TaxCategory,
                                          TaxCategoryDefinition, ProductCategory, Product,
                                          ProductVariant, ModifierGroup, Modifier,
                                          ProductModifierGroup, ProductLocationOverride,
                                          VenueTaxConfiguration, Menu, MenuSection,
                                          MenuSectionItem, MenuAvailabilityRule)
src/DaxaPos.Domain/Entities/Permission.cs   (modify: add Category)
src/DaxaPos.Domain/Enums/                   (new: PermissionCategory, TaxTreatment, TaxJurisdictionType,
                                              RoundingMode, DayOfWeekMask or similar)
src/DaxaPos.Domain/Events/              (new lifecycle/config-change domain events, one per entity
                                          group, following the Milestone D "Action" pattern)
src/DaxaPos.Application/Tax/            (new: TaxCalculationEngine + its input/output records)
src/DaxaPos.Application/Pricing/        (new: PriceResolver + its input/output records)
src/DaxaPos.Application/Identity/       (modify: staff-PIN sensitive-permission guard now reads
                                          Permission.Category instead of Permissions.AdminSensitive)
src/DaxaPos.Persistence/Configurations/ (new EF configs for every new entity)
src/DaxaPos.Persistence/Migrations/     (6 new migrations — see Milestones)
src/DaxaPos.Persistence/DaxaDbContext.cs (modify: new DbSets + fail-closed filters)
src/DaxaPos.Api/Endpoints/Catalog/      (new: ProductCategoryEndpoints, ProductEndpoints,
                                          ProductVariantEndpoints, ModifierGroupEndpoints,
                                          ModifierEndpoints, ProductLocationOverrideEndpoints)
src/DaxaPos.Api/Endpoints/Tax/          (new: TaxCategoryEndpoints, TaxDefinitionEndpoints,
                                          TaxDefinitionTemplateEndpoints, TaxCategoryDefinitionEndpoints,
                                          VenueTaxConfigurationEndpoints)
src/DaxaPos.Api/Endpoints/Menus/        (new: MenuEndpoints, MenuSectionEndpoints,
                                          MenuAvailabilityRuleEndpoints, ResolvedMenuEndpoints)
src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs (modify: new handlers)
src/DaxaPos.Api/Program.cs              (modify: DI/audit-handler/endpoint-map registrations)
tests/DaxaPos.UnitTests/Tax/            (new: TaxCalculationEngineTests)
tests/DaxaPos.UnitTests/Pricing/        (new: PriceResolverTests)
tests/DaxaPos.UnitTests/Identity/       (new: PermissionCategoryTests or equivalent)
tests/DaxaPos.Api.Tests/                (new: one *EndpointsTests.cs per entity, following the
                                          Milestone D convention; RbacTests.cs inventory extended)
```

## Architecture Assumptions

- **No new `Modules.*` project** (see Non-goals). Same reasoning as PLAN-0003: nothing here yet needs a second DB-touching domain-event consumer outside `Api`, so ADR-0015 Decision §4 (audit handlers stay in `Api`) is unchanged.
- **Tenant isolation follows the established Location/Terminal pattern exactly**, not the Organisation pattern: every new entity carries a denormalized `TenantId` (fail-closed EF Core global query filter, per ADR-0015 §1) *and* an `OrganisationId` cross-checked against `AuthContext.OrganisationId` (ADR-0015's Context Provenance rule) — because `catalog.manage`/`pricing.manage`/`menus.manage` will be granted to `OrganisationOwner`/`VenueManager`, not just `SystemAdmin` (see Milestone D's Organisation-vs-Location asymmetry precedent). A mismatch is 404, never 403.
- **No client-supplied `TenantId` is ever accepted** — same 400-on-non-null-body-field pattern as every PLAN-0003 write endpoint.
- **The tax calculation engine is pure and DB-independent.** It takes already-resolved `TaxComponentSnapshot` inputs (rate, name, jurisdiction, rounding rule) and a taxable amount, and returns tax line results. It does not query the database, does not know about `Product` or `Order`, and does not exist as an injectable service with EF dependencies — this is what makes it unit-testable against the AU/NZ mixed-basket examples without any order/product infrastructure existing yet. Resolving *which* `TaxDefinition`s apply to a given product at a given location (the DB-touching part) is a separate, thin resolution step layered on top, callable independently so PLAN-0005's Order module can reuse it without re-deriving the resolution logic.
- **The per-order 20-tax-component design limit (ADR-0006) is not enforced by this plan.** It is an order-level aggregate across multiple lines; `Order` does not exist until PLAN-0005. This plan enforces the per-line 10-component limit (`TaxCategoryDefinition` rows resolved for one category/location pair), and documents the order-level limit as PLAN-0005's responsibility to enforce when it aggregates lines into an order. Flagged explicitly so it is not silently dropped.
- **The pricing resolver is likewise pure** — `Product.BasePrice` → variant `PriceDelta` → modifier `PriceDelta`s → location `PriceOverride` (replaces, not adds to, the resolved base) → tax-inclusive/exclusive interpretation from `VenueTaxConfiguration`. No order/basket-level logic (quantity, combos, promotions) belongs here — Phase 2.
- **`TaxDefinition` is tenant-owned, not global** — see Design Decision 1. `TaxDefinitionTemplate` is the global, unfiltered, system-wide reference catalogue (same status as `Role`/`Permission`) that `TaxDefinition` rows are optionally cloned from.
- **Reuse of `RequirePermissionFilter(rejectStaffPin:)` is universal for every write/config endpoint in this plan** — catalog, tax, pricing, and menu configuration are all financially/operationally sensitive per ADR-0013. The one deliberate exception is the sold-out toggle (Milestone F) and the resolved-menu read endpoint (Milestone G) — both `rejectStaffPin: false` by design, since a POS operator must be able to call them. This asymmetry is the plan's single highest-risk design call; see Human Decisions Needed.
- **`Permission.Category` (OI-0015) is added in Milestone A**, before any new permission code is created, so every PLAN-0004 permission code is classified at creation time instead of extending the hard-coded `Permissions.AdminSensitive` list a third time.

## Domain Assumptions

- A product always has exactly one `TaxCategoryId` (already stated in the original draft; unchanged).
- A `TaxCategory` is a tenant/organisation-owned semantic label (`Taxable`, `GSTFree`, `ZeroRated`, `Exempt`, or a tenant-defined custom code) — it does not itself carry a rate. Rates live on `TaxDefinition`, connected via `TaxCategoryDefinition`.
- A `TaxCategoryDefinition` mapping can be organisation-wide (`LocationId == null`) or location-specific (`LocationId` set) — this is what lets a multi-location tenant spanning AU and NZ locations share one `TaxCategory` ("Taxable") while resolving to a different `TaxDefinition` per location's jurisdiction, without needing per-country product duplication.
- Tax-inclusive pricing is the default for AU/NZ (unchanged from the original draft), expressed per-location via `VenueTaxConfiguration.TaxInclusivePricing`, not a global constant.
- A tax-affecting `Product` update (its `TaxCategoryId` changes) triggers OI-0007's archive-and-replace: the existing row is archived (`IsArchived = true`, `ArchivedAtUtc` set), a new `Product` row is created with a new `Id` and the updated `TaxCategoryId`, and the archived row's `SupersededByProductId` points at the new row. A non-tax-affecting update (`Name`, `Description`, `BasePrice`, `DisplayOrder`, category, image) is an ordinary in-place `PATCH`, exactly like Milestone D's `Location`/`Terminal` rename. This distinction is enforced in the endpoint handler, not left to caller discretion.
- `ProductVariant`/`Modifier` price fields are deltas (`+`/`-` on the resolved base price), not absolute prices — consistent with `docs/modules/pricing.md`'s "Modifier pricing (additional charge per modifier selection)."
- `ProductLocationOverride` absence for a given `(ProductId, LocationId)` pair means "use the organisation-wide `Product` defaults" — the default-and-override model ADR-0003 requires, not a special-cased single-location code path.
- A `Menu` with `LocationId == null` is organisation-wide; a `Menu` with `LocationId` set applies to that location only. The "resolved menu" endpoint always requires a `locationId` query parameter and merges organisation-wide + location-specific menus deterministically (location-specific menu items win on conflict) — exact merge precedence is a Milestone G Design Decision, flagged for approval, not assumed silently.

## Risks

- **Tax rounding correctness.** The engine must reproduce the AU worked example exactly ($5.50/11 = $0.50 per line, summed not derived from the order total) — this is a unit-test-first requirement (Milestone B), not something to verify after the fact.
- **Archive-and-replace correctness under concurrent product edits.** Two simultaneous tax-category-changing `PATCH` requests against the same product could both read the same "current" row before either archives it, producing two superseding rows. Same class of race as PLAN-0003's already-accepted, already-documented `DeviceRegistrationPin.MaxUses` race (OI-0013) — MVP-acceptable, to be flagged as a new open issue rather than solved with row-locking in this plan, unless the human wants otherwise (see Human Decisions Needed).
- **Modifier/variant pricing interaction with tax** needs deliberate test coverage (a modifier surcharge on a `GSTFree` product must not silently apply GST) — explicitly listed in `docs/testing/tax-tests.md`'s "Modifier Tax" section.
- **Menu availability rules need a concrete time/day model** (day-of-week + local start/end time) decided now, since "TBD later" would leave `MenuAvailabilityRule`'s schema without a real design — this plan fixes a specific shape (Milestone G) rather than deferring it, since deferring a menu's core scheduling model would gut the module's stated purpose.
- **Getting the staff-PIN-accessible/rejected split wrong for even one endpoint is a real operational or security failure**, not a cosmetic bug: rejecting staff PIN on the resolved-menu read blocks the POS from working at all; failing to reject staff PIN on a price/tax-category write endpoint lets a compromised staff PIN alter financial configuration. Both are called out individually per-endpoint in the milestones below, not left to a blanket rule.

## Global Tax Design Constraints (Country-Agnostic)

Restating and applying ADR-0006's accepted direction concretely for this plan, since it is a required deliverable of this planning pass:

- The engine never branches on country code, tax name, or jurisdiction string. All AU/NZ-specific behaviour (10%/15% rates, GST-free/zero-rated/exempt treatment, `F` marker) is *data* — rows in `TaxDefinitionTemplate`/`TaxDefinition`, not `if` statements.
- `TaxDefinition` fields mirror ADR-0006's Acceptance Addendum exactly: `TaxName`, `RatePercent`, `JurisdictionName`, `JurisdictionType`, `IncludedInPrice` (tax-inclusive vs exclusive), `RoundingMode`, `RoundingPrecision`, `CalculationScope` (per-line for MVP; per-component is representable but not exercised until a future stacked-tax jurisdiction is configured), `ReceiptMarkerCode`/`ReceiptMarkerLabel` (per ADR-0011, configurable — not hard-coded `F`), `ReportingCategory`, `IsActive`.
- Design limits enforced: max 10 `TaxCategoryDefinition` rows resolved per `(TaxCategory, Location)` pair (per-line). The 20-per-order limit is explicitly PLAN-0005's responsibility (see Architecture Assumptions).
- `TaxTreatment` (`Taxable`/`GSTFree`/`ZeroRated`/`Exempt`) is metadata on `TaxCategory`, used for reporting/receipt-marker defaulting — it never drives calculation branching directly; calculation only ever reads the resolved `TaxDefinition.RatePercent`/`RoundingMode`/etc. A 0% `TaxDefinition` (e.g. `AU_GST_FREE`) produces a correctly-shaped `TaxLineResult` with `TaxAmount = 0`, not a skipped/absent tax line — this is what lets receipts show the line with its marker rather than silently omitting tax data.

## AU/NZ MVP Tax Assumptions (Concrete, Non-Binding on the Engine)

These are the *seed data* this plan ships, not engine-level rules:

| `TaxDefinitionTemplate.Code` | RatePercent | JurisdictionName | IncludedInPrice | ReceiptMarkerCode |
|---|---|---|---|---|
| `AU_GST_10` | 10 | Australia | true | (none) |
| `AU_GST_FREE` | 0 | Australia | true | `F` |
| `NZ_GST_15` | 15 | New Zealand | true | (none) |
| `NZ_ZERO_RATED` | 0 | New Zealand | true | `Z` |
| `NZ_EXEMPT` | 0 | New Zealand | true | `E` |

`TaxCategory` seed suggestion (tenant-created via the `from-template` endpoint, not globally seeded — see Design Decision 1): `Taxable` → `AU_GST_10`/`NZ_GST_15` depending on location; `GSTFree`/`ZeroRated`/`Exempt` → the corresponding 0%-rate definitions. Rounding: `NearestCent`, precision 2, matching the worked example ($5.50/11 = $0.50).

## ADR-0016 Impact On This Plan (No Localisation Implementation)

Per CLAUDE.md's explicit instruction, ADR-0016 is not implemented. Its constraints on schema/naming decided in this plan:

- Every translatable-in-future column (`Product.Name`, `ProductCategory.Name`, `ModifierGroup.Name`, `Modifier.Name`, `Menu.Name`, `MenuSection.Name`, `TaxDefinition.TaxName`, `TaxDefinition.ReceiptMarkerLabel`) is the **invariant/fallback value** per ADR-0016 §3/§4 — plain `text`/unbounded `varchar`, no length assumptions that presume Latin-script English, no business logic anywhere that string-matches on these values (matching happens on `Id`/`Code`, e.g. `TaxDefinitionTemplate.Code`, never on `TaxName`).
- No `{Entity}Translation` tables are created. No culture-resolution logic exists. A single implicit culture (`en-AU`) is assumed everywhere, matching ADR-0016 §7's MVP scope exactly.
- Receipt/tax label fields (`ReceiptMarkerCode`, `ReceiptMarkerLabel`) are already configurable per ADR-0011 and per-`TaxDefinition` in this plan's schema — this is deliberately the same mechanism ADR-0016 §5 says receipt/tax localisation will extend later, so no rework is anticipated when translation is eventually added.
- **Recommendation, not part of this plan's action items:** ADR-0016 has been sitting as Proposed since 2026-07-02 and this plan is the first to build the entities it constrains. Recommend accepting it now (formalizing the constraint, not building anything) — see Human Decisions Needed.

## Milestones

### Milestone A — Permission metadata (closes OI-0015) and new permission codes

No product/tax/menu entities. Pure identity-layer follow-up, deliberately sequenced first so every permission code this plan adds is classified at birth.

- Add `PermissionCategory` enum (`Operational`, `AdminSensitive`) to `DaxaPos.Domain.Enums`.
- Add `Permission.Category` column (`PermissionCategory`, required). Migration `AddPermissionCategory`: backfills all 8 existing seeded permissions to `AdminSensitive` (matches the current hard-coded `Permissions.AdminSensitive` set exactly — no behaviour change for existing codes).
- Add 3 new permission codes to `Permissions.cs`: `CatalogManage = "catalog.manage"`, `PricingManage = "pricing.manage"`, `MenusManage = "menus.manage"`. Seed `RolePermission` rows: all three → `SystemAdmin`, `OrganisationOwner`, `VenueManager` (matching OI-0007's "manager-level permission or higher"). All three `Category = AdminSensitive`.
- Add a 4th new permission code: `CatalogSoldOutToggle = "catalog.sold-out-toggle"`, `Category = Operational`, seeded to `SystemAdmin`, `OrganisationOwner`, `VenueManager`, **and `Staff`** — the first `Operational`-category, staff-PIN-eligible permission in the codebase (see Milestone F). Recorded explicitly since it is a new kind of grant (`Staff` role has received zero permissions until now).
- Refactor the staff-PIN login sensitive-permission guard (`AuthEndpoints.cs`'s staff-pin login handler) to query `Permission.Category` for the resolved role snapshot instead of intersecting against the hard-coded `Permissions.AdminSensitive` set. Delete `Permissions.AdminSensitive` once the guard no longer reads it (confirm no other caller depends on it first).
- Close OI-0015: move `docs/issues/open/OI-0015-permission-metadata-for-staff-pin-eligibility.md` to `docs/issues/closed/`, record the `Permission.Category` decision as the Outcome, update `docs/issues/index.md`.

**Entities/tables:** `Permission` (modify — add `Category`). No new tables.
**Migration:** `AddPermissionCategory` — one column, `HasData` update for existing 8 rows' `Category`, `HasData` insert for the 4 new `Permission` rows and their `RolePermission` seed rows.
**Endpoints:** None.
**Permission codes introduced:** `catalog.manage`, `pricing.manage`, `menus.manage` (all `AdminSensitive`); `catalog.sold-out-toggle` (`Operational`).
**`rejectStaffPin`:** N/A this milestone (no endpoints).
**Tests:** `tests/DaxaPos.UnitTests/Identity/StaffPinSensitivePermissionGuardTests.cs` (or extend the existing staff-pin-login test file) — a staff session whose role snapshot includes an `AdminSensitive` permission is still rejected at login (regression-proves the refactor didn't weaken the existing guard); a staff session with only `Operational` permissions (once `catalog.sold-out-toggle` exists) logs in successfully. `RbacTests.cs` unaffected this milestone (no new endpoints).
**Docs:** `docs/issues/open/OI-0015...md` → `docs/issues/closed/`, `docs/issues/index.md`, `src/DaxaPos.Application/Identity/Permissions.cs`'s doc comment (remove the "TEMPORARY MECHANISM" note).

---

### Milestone B — Tax foundation: entities, templates, and the pure calculation engine

- `TaxDefinitionTemplate` (system-wide, unfiltered — same status as `Role`/`Permission`): `Id`, `Code`, `Name`, `CountryCode`, `RegionCode?`, `RatePercent`, `JurisdictionName`, `JurisdictionType`, `IncludedInPrice`, `RoundingMode`, `RoundingPrecision`, `CalculationScope`, `ReceiptMarkerCode?`, `ReceiptMarkerLabel?`, `ReportingCategory?`, `IsActive`. Seeded via `HasData` with the 5 AU/NZ rows from the table above.
- `TaxDefinition` (tenant-owned: `TenantId`, `OrganisationId`, plus the same fields as the template, plus `SourceTemplateCode?` for traceability, `IsActive`, `CreatedAtUtc`).
- `TaxCategory` (tenant-owned: `TenantId`, `OrganisationId`, `Code`, `Name`, `TaxTreatment` enum, `IsActive`, `CreatedAtUtc`).
- `TaxCategoryDefinition` (tenant-owned join: `TenantId`, `TaxCategoryId`, `TaxDefinitionId`, `LocationId?` [null = organisation-wide], `Priority` int, `IsActive`, `CreatedAtUtc`).
- `TaxCalculationEngine` (`DaxaPos.Application.Tax`, pure, no DB dependency): input `TaxableLineRequest(decimal TaxableAmount, IReadOnlyList<TaxComponentSnapshot> Components)` where `TaxComponentSnapshot` carries the resolved `TaxDefinition` fields by value (not an EF entity); output `IReadOnlyList<TaxLineResult>` matching `OrderLineTax`'s documented shape (`TaxRateId`→`TaxDefinitionId`, `TaxName`, `RatePercent`, `TaxableAmount`, `TaxAmount`, `JurisdictionName`, `JurisdictionType`). Throws/rejects (a validation result, not an exception, per the codebase's `Results.BadRequest` convention at the endpoint layer — the engine itself returns a typed failure) if `Components.Count > 10`.

**Entities/tables:** `TaxDefinitionTemplate`, `TaxDefinition`, `TaxCategory`, `TaxCategoryDefinition` (all new).
**Migration:** `AddTaxFoundation` — creates all 4 tables, `HasData` seed for `TaxDefinitionTemplate`'s 5 rows.
**Endpoints:** None yet (Milestone C).
**Permission codes:** None new.
**`rejectStaffPin`:** N/A (no endpoints).
**Tests:** `tests/DaxaPos.UnitTests/Tax/TaxCalculationEngineTests.cs` — AU mixed basket ($5.50 → $0.50 GST, $8.80 → $0.80 GST, $6.00 GST-free → $0.00, sum $1.30 on $20.30), NZ 15% basket, zero-rated/exempt → $0.00 with a populated (not omitted) `TaxLineResult`, tax-inclusive vs exclusive math, rounding mode application, >10-components rejection, immutability (the engine takes value inputs, cannot mutate anything). This is the first genuinely financial logic in the codebase — TDD (test-first) is mandatory per CLAUDE.md's Testing Rules, matching how PLAN-0003 TDD'd `Pbkdf2PinHasher`/`LoginLockoutPolicy`.
**Docs:** `docs/architecture/tax-engine.md`, `docs/modules/tax.md` — implementation-status sections once Milestone B lands (not yet, this is the plan).

---

### Milestone C — Tax configuration endpoints

CRUD-lifecycle pattern identical to PLAN-0003 Milestone D (create/read/list/update-limited-fields/deactivate/reactivate, no hard delete for `TaxCategory`/`TaxDefinition` — both are financially meaningful per ADR-0010). `TaxCategoryDefinition` (a pure mapping row, not itself a financial record) supports hard delete/replace.

- `GET /api/v1/tax-definition-templates` — read-only, lists the 5 (or more, future) system templates. `catalog.manage`, `rejectStaffPin: true` (config surface, not sales-floor).
- `POST /api/v1/tax-definitions` (from-scratch) and `POST /api/v1/tax-definitions/from-template` (body: `TemplateCode`) — both `catalog.manage`, `rejectStaffPin: true`. `GET`/list/`PATCH`/deactivate/reactivate follow the standard six-endpoint shape.
- `POST/GET /api/v1/tax-categories`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` — `catalog.manage`, `rejectStaffPin: true`.
- `POST/GET /api/v1/tax-category-definitions` (body: `TaxCategoryId`, `TaxDefinitionId`, `LocationId?`, `Priority`), `DELETE /{id}` (hard delete — a mapping row, not a financial record itself; removing it does not retroactively affect any already-calculated tax snapshot, which lives independently once PLAN-0005 exists) — `catalog.manage`, `rejectStaffPin: true`.
- Every write raises a lifecycle domain event (`TaxDefinitionLifecycleDomainEvent`, `TaxCategoryLifecycleDomainEvent`, `TaxCategoryDefinitionChangedDomainEvent`) with a JSON before/after snapshot, per the Milestone D `jsonb` pattern (and its known pitfall — bare strings/bools must be `JsonSerializer.Serialize`d, not passed raw, per the Milestone D bug note in `PLAN-0003-worker-notes.md`).

**Entities/tables:** None new (Milestone B's tables).
**Migration:** None.
**Endpoints:** 17 total (5+6 tax-definition-ish + 6 tax-category + 3 tax-category-definition, minus overlap — exact count finalized at implementation; see table above for the shape).
**Permission codes:** `catalog.manage` (all endpoints).
**`rejectStaffPin`:** `true` on every endpoint in this milestone, no exceptions — tax configuration is squarely OI-0007's "manager-level or higher" surface.
**Tests:** `TaxDefinitionEndpointsTests.cs`, `TaxCategoryEndpointsTests.cs`, `TaxCategoryDefinitionEndpointsTests.cs` — happy-path CRUD, 400 on client-supplied `TenantId`, 403 without `catalog.manage`, 403 for a staff-PIN session even with the permission (proves `rejectStaffPin` independently, matching the Milestone D `RequirePermissionFilterTests` pattern), 404 cross-tenant and cross-organisation, audit-row assertions per OI-0007's explicit audit requirement (who, when, old config, new config, reason if supplied).
**Docs:** `docs/modules/tax.md`, `docs/architecture/tax-engine.md` implementation-status sections.

---

### Milestone D — Product catalogue foundation

- `ProductCategory` (`TenantId`, `OrganisationId`, `Name`, `DisplayOrder`, `IsActive`, `CreatedAtUtc`).
- `Product` (`TenantId`, `OrganisationId`, `ProductCategoryId`, `Name`, `Description?`, `Sku?`, `Barcode?`, `TaxCategoryId`, `BasePrice` decimal, `IsActive`, `IsArchived` bool default false, `ArchivedAtUtc?`, `SupersededByProductId?` self-FK, `CreatedAtUtc`).
- `Product` write endpoint enforces the archive-and-replace rule from Domain Assumptions: `PATCH` with an unchanged `TaxCategoryId` updates in place; `PATCH` with a different `TaxCategoryId` archives the current row and creates a new one, returning the new row's `ProductResponse` (with a `PreviousProductId` field pointing back for the caller's convenience).
- List/read endpoints exclude archived products by default (same "list hides, single `GET` doesn't" convention as Milestone D `IsActive`) — `IsArchived` and `IsActive` are independent flags checked separately (an active-but-later-archived product and an explicitly-deactivated-but-not-archived product are different states, both need to be representable, per Domain Assumptions).

**Entities/tables:** `ProductCategory`, `Product` (new).
**Migration:** `AddProductCatalogueFoundation`.
**Endpoints:** 6 (category) + 7 (product — the extra one is the archive-and-replace variant of update, though it's the same route/verb, just different response shape/status) = 13.
**Permission codes:** `catalog.manage`.
**`rejectStaffPin`:** `true` on every endpoint.
**Tests:** `ProductCategoryEndpointsTests.cs`, `ProductEndpointsTests.cs` — standard CRUD matrix (as Milestone C) plus two archive-and-replace-specific tests: a `TaxCategoryId`-changing `PATCH` archives the old row and creates a new one with `SupersededByProductId` correctly linked; a non-tax-affecting `PATCH` (`Name` only) does not archive.
**Docs:** `docs/modules/catalog.md` implementation-status section, cross-referencing OI-0007 and ADR-0016's invariant-name-column note.

---

### Milestone E — Product variants and modifiers

- `ProductVariant` (`ProductId`, `Name`, `PriceDelta` decimal, `Sku?`, `IsActive`, `CreatedAtUtc`).
- `ModifierGroup` (`TenantId`, `OrganisationId`, `Name`, `SelectionMin`, `SelectionMax`, `IsRequired`, `IsActive`, `CreatedAtUtc`).
- `Modifier` (`ModifierGroupId`, `Name`, `PriceDelta` decimal, `IsActive`, `CreatedAtUtc`).
- `ProductModifierGroup` (join: `ProductId`, `ModifierGroupId`, `DisplayOrder`).

**Entities/tables:** `ProductVariant`, `ModifierGroup`, `Modifier`, `ProductModifierGroup` (new).
**Migration:** `AddVariantsAndModifiers`.
**Endpoints:** 6 (variant, body carries `ProductId`) + 6 (modifier group) + 6 (modifier, body carries `ModifierGroupId`) + 2 (assign/unassign modifier group to product) = 20.
**Permission codes:** `catalog.manage`.
**`rejectStaffPin`:** `true` on every endpoint.
**Tests:** `ProductVariantEndpointsTests.cs`, `ModifierGroupEndpointsTests.cs`, `ModifierEndpointsTests.cs` — standard CRUD matrix; a `ProductModifierGroup` assignment test confirming a modifier group can be attached/detached and `DisplayOrder` respected.
**Docs:** `docs/modules/catalog.md` (variants/modifiers section).

---

### Milestone F — Location-level catalog overrides and the pricing resolver

- `ProductLocationOverride` (`TenantId`, `LocationId`, `ProductId`, `IsAvailable` bool default true, `IsSoldOut` bool default false, `PriceOverride` decimal?, `CreatedAtUtc`). No `OrganisationId` column needed — `LocationId` alone is enough to derive/cross-check organisation via the existing `Location` lookup, matching the `Terminal`-walks-through-`Location` precedent from Milestone D.
- `VenueTaxConfiguration` (`TenantId`, `LocationId`, `TaxInclusivePricing` bool, `TaxCalculationMode` enum, `CreatedAtUtc`) — one row per location; absence means "not yet configured," and the resolver/endpoint layer must decide whether that is a 404 or a sensible AU/NZ-default fallback (flagged in Human Decisions Needed).
- `PriceResolver` (`DaxaPos.Application.Pricing`, pure): `Resolve(Product, ProductVariant?, IReadOnlyList<Modifier>, ProductLocationOverride?)` → `ResolvedPrice(decimal Amount, bool IsTaxInclusive)`. `PriceOverride` replaces the resolved base+variant+modifier total outright (an explicit venue-set price), it does not add to it — flagged as a Design Decision since "override" is ambiguous between replace and adjust; replace matches `docs/modules/pricing.md`'s "location-specific price overrides" wording most directly.
- **Sold-out toggle** is a separate, narrow endpoint from the `ProductLocationOverride` CRUD used for price: `POST /api/v1/products/{productId}/locations/{locationId}/sold-out` (body: `{ "IsSoldOut": true }`) — gated by the new `catalog.sold-out-toggle` permission (Milestone A), **`rejectStaffPin: false`**, granted to `Staff` too. This is the plan's first genuinely staff-accessible write endpoint. The full `ProductLocationOverride` record (including `PriceOverride`) is still only editable via `pricing.manage`, `rejectStaffPin: true`.

**Entities/tables:** `ProductLocationOverride`, `VenueTaxConfiguration` (new).
**Migration:** `AddLocationOverridesAndVenueTaxConfig`.
**Endpoints:** `POST/GET /api/v1/product-location-overrides`, `GET/PATCH/{id}` (5, `pricing.manage`) + `POST .../sold-out` (1, `catalog.sold-out-toggle`, staff-accessible) + `POST/GET /api/v1/venue-tax-configurations`, `GET/PATCH/{id}` (4, `pricing.manage`) = 10.
**Permission codes:** `pricing.manage` (price/config), `catalog.sold-out-toggle` (sold-out only).
**`rejectStaffPin`:** `true` everywhere except the sold-out toggle (`false`, deliberately).
**Tests:** `ProductLocationOverrideEndpointsTests.cs` — the 400/403/404 matrix for `pricing.manage` endpoints, *plus* a dedicated test proving a `Staff`-role PIN session **succeeds** on the sold-out toggle and a separate test proving the same session gets 403 on the price-override `PATCH` — this pair is the concrete proof the asymmetry works as designed, not just documented. `PriceResolverTests.cs` (unit) — base price only, variant delta, modifier deltas, location override replaces rather than adds, no-override falls back to base.
**Docs:** `docs/modules/pricing.md`, `docs/architecture/multi-location.md` (location-level catalog/price override implementation status).

---

### Milestone G — Menu construction and the resolved-menu read endpoint

- `Menu` (`TenantId`, `OrganisationId`, `LocationId?` [null = org-wide], `Name`, `IsActive`, `CreatedAtUtc`).
- `MenuSection` (`MenuId`, `Name`, `DisplayOrder`, `IsActive`).
- `MenuSectionItem` (`MenuSectionId`, `ProductId`, `DisplayOrder`).
- `MenuAvailabilityRule` (`MenuId`, `DaysOfWeekMask` [flags enum, Mon–Sun], `StartTimeLocal`, `EndTimeLocal`, `IsActive`) — a menu with zero rules is always available; one or more rules means "available only during at least one matching window," evaluated in the location's own local time (not UTC-naively), per ADR-0003's location-context requirements.
- **`GET /api/v1/menus/resolved?locationId={id}`** — the sales-screen-ready projection: merges the location's org-wide + location-specific `Menu`s, applies `MenuAvailabilityRule`s against current local time, excludes products with `ProductLocationOverride.IsAvailable == false` or `IsSoldOut == true` (or `Product.IsActive == false`/archived), resolves price via `PriceResolver`, and includes the product's tax category marker info (not a calculated tax amount — no order exists to calculate against yet). Merge precedence when both an org-wide and location-specific `Menu` exist: location-specific wins for any product appearing in both (Design Decision, flagged for approval). **Gated by `.RequireAuthorization()` only — no `RequirePermission`, no `rejectStaffPin`** — any authenticated session (including a staff-PIN session) in the resolved location can call it, matching `/auth/me`'s existing no-permission-code precedent. This is the plan's most consequential single design decision; see Human Decisions Needed.

**Entities/tables:** `Menu`, `MenuSection`, `MenuSectionItem`, `MenuAvailabilityRule` (new).
**Migration:** `AddMenus`.
**Endpoints:** 6 (menu) + 4 (section, nested under menu) + 2 (section item add/remove) + 3 (availability rule create/list/delete) + 1 (resolved) = 16.
**Permission codes:** `menus.manage` (all except resolved).
**`rejectStaffPin`:** `true` on all configuration endpoints; the resolved-menu endpoint has no permission check at all (see above).
**Tests:** `MenuEndpointsTests.cs`, `MenuSectionEndpointsTests.cs`, `MenuAvailabilityRuleEndpointsTests.cs` — standard CRUD matrix. `ResolvedMenuEndpointsTests.cs` — a staff-PIN session **succeeds** (the critical assertion), sold-out/unavailable products are excluded, an org-wide menu and a location-specific menu merge with location precedence, an availability rule outside its window hides the whole menu, prices returned match `PriceResolverTests`' expectations for the same inputs.
**Docs:** `docs/modules/menus.md`, `docs/modules/catalog.md` (sold-out visibility cross-reference).

---

### Milestone H — Consolidation, RBAC sweep, and documentation closeout

Test-and-documentation-only, mirroring PLAN-0003 Milestone G's shape.

- Extend `tests/DaxaPos.Api.Tests/RbacTests.cs`'s endpoint inventory with every Milestone A–G permission-gated endpoint (unauthenticated → 401; wrong-tenant/org → 404; missing permission → 403; staff-PIN → 403 except the two deliberate exceptions, which get their own explicit staff-PIN-**succeeds** row in the same inventory so the exception is asserted, not just absent).
- Confirm (and assert via `IgnoreQueryFiltersUsageTests.cs`, extended if needed) that this plan introduces **zero** new `IgnoreQueryFilters()` call sites — every PLAN-0004 endpoint runs under an already-authenticated tenant/org context (unlike PLAN-0003's pre-auth device/PIN bootstrap flows), so the documented bootstrap set should stay at exactly the same files it is today.
- Verify migrations apply cleanly in sequence from a completely empty database (all 6 new migrations + PLAN-0003's existing 6 = 12 total).
- File any new open issues surfaced during implementation (expected candidates, not pre-decided): archive-and-replace concurrency race (parallel to OI-0013); the org-wide/location-specific menu merge precedence, if the human wants a different rule than "location wins" once seen in practice; whether `VenueTaxConfiguration` absence should 404 or default.
- Docs: `docs/modules/catalog.md`, `tax.md`, `menus.md`, `pricing.md` (final implementation-status passes), `docs/architecture/tax-engine.md`, `docs/architecture/multi-location.md`, `docs/modules/audit.md` (new event types), `docs/testing/tax-tests.md`, `docs/testing/security-tests.md` (implementation-status sections, matching PLAN-0003's pattern exactly), `docs/README.md` (Active Plans line, once complete), `docs/issues/index.md`.
- Update this plan's Status section milestone-by-milestone as work proceeds — no more than 3 commits without a refresh, per CLAUDE.md.

**Entities/tables:** None. **Migration:** None. **Endpoints:** None (test/doc-only, same as PLAN-0003 Milestone G).

---

## Permission Catalogue Additions (Summary)

| Permission code | Purpose | Category | Seeded to roles |
|---|---|---|---|
| `catalog.manage` | Product/category/variant/modifier CRUD; tax category/definition CRUD and assignment (per OI-0007) | AdminSensitive | `SystemAdmin`, `OrganisationOwner`, `VenueManager` |
| `pricing.manage` | Location price overrides, venue tax configuration | AdminSensitive | `SystemAdmin`, `OrganisationOwner`, `VenueManager` |
| `menus.manage` | Menu/section/item/availability-rule CRUD | AdminSensitive | `SystemAdmin`, `OrganisationOwner`, `VenueManager` |
| `catalog.sold-out-toggle` | Toggle a product's sold-out state at a location | **Operational** | `SystemAdmin`, `OrganisationOwner`, `VenueManager`, **`Staff`** |

No permission code is required to call the resolved-menu read endpoint — authentication alone is sufficient (see Milestone G).

## Tests To Run Later

- `dotnet build DaxaPos.sln` (0 warnings, 0 errors) after each milestone.
- `dotnet test DaxaPos.sln` against real Postgres (no mocks, matching the established pattern) after each milestone.
- `TaxCalculationEngineTests`/`PriceResolverTests` must exist and pass **before** any endpoint in Milestones C/F/G is implemented — these are the financial-logic units CLAUDE.md's Testing Rules single out, and TDD is mandatory for them specifically (not the acceptance-test convention used for CRUD endpoint files).
- Full AU mixed-basket and NZ basket scenarios from `docs/testing/tax-tests.md` and the CLAUDE.md worked example, byte-for-byte (`$5.50`/`$8.80`/`$6.00` → `$20.30` total, `$1.30` GST).
- `RbacTests.cs` extended inventory, including the two deliberate staff-PIN-succeeds assertions.
- EF Core migrations apply cleanly from an empty database (12 migrations total after this plan).
- `IgnoreQueryFiltersUsageTests.cs` continues to pass with the same documented call-site set (no growth expected).

## Documentation To Update

See each milestone's "Docs" line; consolidated list: `docs/modules/catalog.md`, `tax.md`, `menus.md`, `pricing.md`, `docs/architecture/tax-engine.md`, `docs/architecture/multi-location.md`, `docs/modules/audit.md`, `docs/testing/tax-tests.md`, `docs/testing/security-tests.md`, `docs/issues/index.md`, `docs/issues/open/OI-0015...md` (→ closed), `docs/README.md`.

## ADRs Required / ADR Gaps

- **ADR-0006, ADR-0010, ADR-0011, ADR-0015** — already accepted, unchanged, no amendment needed. This plan is designed to fit inside them, not extend them.
- **ADR-0016 — currently Proposed, not Accepted.** Recommend accepting now (see Human Decisions Needed) — no implementation required by acceptance, only formalizes the constraints this plan already honours.
- **Candidate new ADR: catalogue-entity archive-and-replace as a general pattern.** OI-0007 (closed) authorizes this behaviour for `Product` specifically. This plan treats it as OI-0007's direct implementation, not a new architecture decision, but flags that if a *third* entity beyond `Product`/(any future `Modifier`/`TaxCategory` tax-affecting change) needs the same treatment, it may be worth promoting to a standalone ADR rather than re-deriving the pattern per-entity. Not drafted in this plan — recommend deciding after Milestone D ships and the pattern has been exercised once for real.
- **No new ADR required for the `TaxDefinition`-tenant-owned / `TaxDefinitionTemplate`-global split**, the `catalog.manage`/`pricing.manage`/`menus.manage` permission split, or the `catalog.sold-out-toggle` Operational-category precedent — these are this plan's own implementation-level interpretations of already-accepted ADRs (ADR-0006, OI-0007, ADR-0013's operational/sensitive split), flagged as Design Decisions for approval, same status as PLAN-0003 Milestone D's Organisation-vs-Location scoping call.

## Open Issues Required

- **OI-0007** — already closed, directly implemented by Milestones C/D. Not reopened.
- **OI-0015** — closed by Milestone A of this plan.
- **New issues expected at Milestone H** (not opened yet — this is a planning pass, not an implementation milestone): archive-and-replace concurrency race; `VenueTaxConfiguration`-absence behaviour if the human doesn't resolve it definitively now; menu merge-precedence, if it needs revisiting after real use.

## Commit Sequence

```
docs: close OI-0015 and add permission category metadata
feat(identity): add permission category and catalog/pricing/menus permission codes
feat(tax): add tax definition template, tax definition, and tax category entities
feat(tax): implement pure tax calculation engine with AU/NZ unit tests
feat(tax): add tax configuration endpoints
feat(catalog): add product category and product entities with archive-and-replace
feat(catalog): add product catalogue endpoints
feat(catalog): add product variants and modifier groups
feat(catalog): add variant and modifier endpoints
feat(pricing): add location overrides, venue tax configuration, and pricing resolver
feat(pricing): add location override and sold-out toggle endpoints
feat(menus): add menu, section, and availability rule entities
feat(menus): add menu configuration and resolved-menu read endpoints
test(security): extend RBAC matrix and staff-PIN assertions for PLAN-0004 endpoints
docs: update catalog, tax, menus, pricing, and testing docs for PLAN-0004 closeout
```

## Human Decisions Needed

Recorded here so implementation can start immediately on approval without re-litigating during a milestone. Presented as the specific, consequential calls this planning pass made that a different reasonable person could make differently:

1. **The resolved-menu read endpoint (Milestone G) requires no permission code, only authentication — should any authenticated session in the resolved location see the full resolved menu, or should this still require some minimal permission (e.g. a new `pos.sell` permission granted to `Staff` by default)?** Recommendation: no permission code — matching `/auth/me`'s precedent and the practical reality that a POS operator with zero permissions literally cannot use the product. Flagged as the single highest-consequence design call in this plan.
2. **Should `catalog.manage` and `pricing.manage` really be two separate permission codes, or should MVP fold price-override management into `catalog.manage` (single code) and split it later once Phase 2 promotional pricing exists?** Recommendation: keep them separate now — cheaper to never split a combined code later than to split one after roles have been assigned in production.
3. **`TaxDefinition` is tenant-owned and independently editable per OI-0007's Decision, cloned from a global `TaxDefinitionTemplate` catalogue via an explicit endpoint call — should new-organisation creation (PLAN-0003's existing `OrganisationEndpoints.CreateAsync`) instead auto-clone `AU_GST_10`/`AU_GST_FREE` at creation time, so a new AU tenant has working tax defaults without a manual follow-up call?** Recommendation: manual endpoint call for MVP — avoids modifying already-closed-out PLAN-0003 code from this plan, and keeps the auto-provisioning question (which jurisdiction to default to, whether it's even correct for every industry template) for a later onboarding-flow plan.
4. **Archive-and-replace concurrency (two simultaneous tax-category-changing edits to the same product) is left as an accepted, documented race, like OI-0013 — confirm this is acceptable for MVP rather than adding row-locking/optimistic concurrency now.** Recommendation: accept, matching the OI-0013 precedent exactly.
5. **`VenueTaxConfiguration` absence for a location — should the resolved-menu/pricing/tax-resolution code path 404 (config genuinely missing, block sales until configured) or silently default to AU-tax-inclusive?** Recommendation: 404/explicit-error — silent AU-flavoured defaulting for a non-AU/NZ future tenant would violate this plan's own country-agnostic constraint.
6. **Accept ADR-0016 now** (as Accepted, not just Proposed) — no implementation follows from acceptance, only formalizes constraints this plan already honours. Recommendation: yes, accept now, since PLAN-0004 is exactly the "first implementing plan" ADR-0016's own Follow-Up Work section anticipated.
7. **Menu org-wide vs. location-specific merge precedence (location wins on conflict) and the day/time `MenuAvailabilityRule` shape (day-of-week flags + local start/end time) are both fixed by this plan rather than left open — confirm no different precedence/shape is wanted before Milestone G is implemented.**

## Handoff Notes

Depends on PLAN-0003 (closed out, commit `a451482`). This plan's Milestone B (pure tax engine) is the first genuinely financial logic in the codebase and must be correct and fully tested before PLAN-0005 (Payments, Receipts, Printing) starts — PLAN-0005's Order module will call this plan's tax-resolution and pricing-resolution logic directly rather than re-deriving it, and is also where the ADR-0006 20-components-per-order limit finally gets enforced (deferred here, see Architecture Assumptions). Next plan after this one: PLAN-0005.
