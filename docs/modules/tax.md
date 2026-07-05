# Module: Tax Engine

The tax module calculates tax per order line and manages tax configuration.

See also: `docs/modules/09-tax-engine.md`, `docs/architecture/tax-engine.md`.

---

## Responsibilities

- Tax categories (AU_GST_10, AU_GST_FREE, NZ_GST_15, NZ_ZERO_RATED, etc.).
- Tax rate configuration per country/region.
- Tax calculation per order line (tax-inclusive and tax-exclusive).
- Mixed-basket support (taxable + tax-free in same order).
- Tax snapshot at sale time (immutable after finalisation).
- Tax summary per order.
- Tax marker on receipts (F = GST-free).
- Country/region tax configuration.
- Venue tax configuration (tax-inclusive pricing mode).

## Implementation Status (PLAN-0004 Milestone B, 2026-07-04)

The data model and calculation engine are implemented; configuration endpoints are not (Milestone C).

- Entities: `TaxDefinitionTemplate` (global, unfiltered, 5 AU/NZ rows seeded), `TaxDefinition` (tenant-owned, optionally cloned from a template), `TaxCategory` (tenant-owned semantic label), `TaxCategoryDefinition` (tenant-owned join, optionally location-scoped).
- Engine: `DaxaPos.Application.Tax.TaxCalculationEngine.CalculateLine(TaxableLineRequest)` — pure, no EF/DB/HTTP dependency, no constructor parameters. Supports tax-inclusive extraction, tax-exclusive addition, mixed baskets (via repeated per-line calls), and fails closed (`TaxCalculationErrorCode.MissingTaxConfiguration`) rather than silently returning zero tax when no components are supplied.
- Rounding: `TaxRoundingMode.NearestCent` = round-half-away-from-zero at the component's configured precision — a concrete implementation choice, not specified by ADR-0006 itself, proven by a dedicated midpoint test.
- Not yet built: tax configuration endpoints (`GET/POST /api/v1/tax-definitions` etc.), the DB-touching resolution step that turns a product+location into a list of `TaxComponentSnapshot`s, and `OrderLineTax` (waits on `Order`, PLAN-0005).
- See `docs/plans/active/PLAN-0004-worker-notes.md`'s "Milestone B Report" for full detail and deviations.

## Implementation Status (PLAN-0004 Milestone C, 2026-07-05)

Tax configuration endpoints are implemented; no schema changes were needed (Milestone B's entities already covered this milestone's needs).

- `GET /api/v1/tax-definition-templates` — read-only listing of the global template catalogue.
- `POST /api/v1/tax-definitions` (from-scratch) and `POST /api/v1/tax-definitions/from-template` (clones a `TaxDefinitionTemplate` by `Code`, setting `SourceTemplateCode`), plus `GET` (list/by-id), `PATCH` (rate/name/rounding/marker fields only — `Code`/`CountryCode`/`RegionCode` are the definition's stable identity and are not editable), `POST .../deactivate`, `POST .../reactivate`.
- `POST/GET /api/v1/tax-categories`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` — `Code` likewise immutable after creation.
- `POST/GET /api/v1/tax-category-definitions`, `DELETE /{id}` — the only Milestone C entity with hard delete (a pure mapping row, not itself a financial record, ADR-0010). Creation enforces ADR-0006's per-line design limit (max 10 active mappings per `(TaxCategoryId, LocationId)` pair, `LocationId` null = organisation-wide bucket) and validates that the referenced `TaxCategory`/`TaxDefinition`/`Location` all belong to the caller's organisation.
- Permission: `catalog.manage` on every endpoint, `rejectStaffPin: true` throughout (OI-0007's "manager-level or higher" surface) — proven in `StaffPinLoginTests.AssertAllSensitiveEndpointsForbiddenAsync`, not duplicated per entity (matching the Milestone D convention).
- Every write raises a lifecycle domain event (`TaxDefinitionLifecycleDomainEvent`, `TaxCategoryLifecycleDomainEvent`, `TaxCategoryDefinitionChangedDomainEvent`) with a JSON before/after snapshot, audited per OI-0007's explicit requirement.
- See `docs/plans/active/PLAN-0004-worker-notes.md`'s "Milestone C Report" for full detail and deviations.

## Implementation Status (PLAN-0004 Milestone D, 2026-07-05)

`Product.TaxCategoryId` (see `docs/modules/catalog.md`) is a required, organisation-validated reference to a `TaxCategory` row from Milestone C — a product always has exactly one tax category, unchanged from the original architecture assumption. Changing it is the sole trigger for OI-0007's archive-and-replace on `Product` (see `docs/modules/catalog.md`'s Implementation Status). Resolving a product's `TaxCategoryId` into the actual `TaxComponentSnapshot`s the pure `TaxCalculationEngine` consumes (via `TaxCategoryDefinition`) is still not built — that DB-touching resolution step depends on `Order`/pricing context that doesn't exist until later milestones/PLAN-0005.

## Implementation Status (PLAN-0004 Milestone F, 2026-07-05)

`VenueTaxConfiguration` (`GET/POST/PATCH /api/v1/venue-tax-configurations`, `src/DaxaPos.Api/Endpoints/Tax/VenueTaxConfigurationEndpoints.cs`) is implemented — one row per `Location`, gated `pricing.manage` + `rejectStaffPin: true`, no hard delete/deactivate lifecycle (the entity has no `IsActive` column). `TaxCalculationMode` reuses the existing `TaxCalculationScope` enum (`PerLine`/`PerComponent`) rather than a near-duplicate type with identical values. Absence for a location 404s exactly like any other missing row — nothing in this milestone auto-creates a `VenueTaxConfiguration` on a caller's behalf, matching the plan's approved Human Decision #5 and the same "no silent auto-provisioning" rule already applied to `TaxDefinition` cloning (Milestone C). `PriceResolver` (`docs/modules/pricing.md`) consumes `VenueTaxConfiguration.TaxInclusivePricing` and fails closed when it's missing, rather than defaulting.

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [ADR-0006 — Tax-Line Based Tax Engine](../adr/accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [ADR-0016 — Multi-Language and Localisation Strategy](../adr/accepted/ADR-0016-multi-language-and-localisation-strategy.md) (accepted 2026-07-05) — tax/legal label localisation is planned but deferred, not part of PLAN-0004 or the initial MVP. The `Name`/`ReceiptMarkerLabel` columns added by Milestone B are already mapped as plain invariant/fallback text per this ADR's pre-recorded constraint, so no rework is anticipated when it is eventually implemented.
