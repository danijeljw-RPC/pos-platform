# PLAN-0004 — Catalog, Menu, Tax, and Pricing

## Status

Draft

## Goal

Implement the product catalogue, menu management, tax engine, and pricing engine. This is the core product data layer that drives the POS sales screen.

## Scope

- Product categories.
- Products (name, SKU, price, modifiers, tax category, images).
- Product variants.
- Modifier groups and modifiers.
- Tax categories (AU_GST_10, AU_GST_FREE, NZ_GST_15).
- Tax rate configuration per country/region.
- Tax calculation engine (per-line, snapshot at sale time).
- Basic pricing rules (base price, location override, size/modifier pricing).
- Menu construction (sections, items, availability).

## Non-goals

- Full pricing rule engine (time-based, day-based, happy hour) — Phase 2.
- Advanced inventory/BOM.
- Loyalty/customer-specific pricing.

## Context Read

- `docs/adr/proposed/ADR-0006-tax-line-based-tax-engine.md`
- `docs/adr/proposed/ADR-0011-receipt-tax-marker-strategy.md`
- `docs/architecture/tax-engine.md`
- `docs/modules/catalog.md`
- `docs/modules/tax.md`
- `docs/modules/menus.md`
- `docs/regions/01-au-nz-tax.md`
- `docs/architecture/04-tax-pricing-model.md`

## Files Likely To Change

```
src/DaxaPos.Modules.Catalog/
src/DaxaPos.Modules.Tax/
src/DaxaPos.Modules.Pricing/
src/DaxaPos.Domain/   (Product, TaxCategory, TaxRate, PriceRule, Menu)
src/DaxaPos.Persistence/ (catalog migrations)
```

## Architecture Assumptions

- Tax engine takes an order line and returns tax lines.
- Tax snapshot is stored on the order line at sale time.
- Menus are data-driven (not hard-coded by industry type).
- Location-level product overrides are supported.

## Domain Assumptions

- A product always has exactly one TaxCategory.
- Tax rates are configured per country/region.
- Tax-inclusive pricing is the default for AU/NZ.
- GST-free products have TaxCategory = AU_GST_FREE (0%).

## Risks

- Tax rounding must follow ATO guidelines.
- Modifier pricing interactions with tax need careful testing.
- Menus need availability rules (time of day, day of week) from the start.

## Implementation / Documentation Steps

1. Define TaxCategory, TaxRate domain entities.
2. Implement tax engine: calculate tax per order line.
3. Add AU GST 10% and AU GST-free tax categories.
4. Add NZ GST 15% and zero-rated tax categories.
5. Write mixed-basket tax tests.
6. Define Product, ProductCategory, ProductVariant, ModifierGroup, Modifier entities.
7. Implement product catalogue API (CRUD).
8. Implement menu construction (sections, items, availability).
9. Implement basic pricing engine (base price, location override).
10. Write product, menu, and pricing tests.
11. Update docs: catalog, tax, menus, pricing modules.

## Tests To Run Later

- AU mixed-basket GST calculation (taxable + GST-free items).
- NZ GST calculation.
- Tax snapshot persistence.
- Tax report accuracy.
- GST-free marker on receipt rendering.
- Modifier pricing with tax.
- Menu availability filter.

## Documentation To Update

- `docs/modules/catalog.md`
- `docs/modules/tax.md`
- `docs/modules/menus.md`
- `docs/modules/pricing.md`
- `docs/architecture/tax-engine.md`

## ADRs Required

- ADR-0006, ADR-0011 (already proposed).

## Open Issues Required

- OI-0007 (tax configuration editing permissions) — needs decision before implementing tax config UI.

## Commit Sequence

```
feat(tax): add tax category and tax rate entities
feat(tax): implement AU GST and GST-free calculation engine
feat(catalog): add product, category, variant, modifier entities
feat(catalog): add product catalogue API
feat(menus): add menu and availability management
feat(pricing): add base pricing and location override
test(tax): add AU/NZ mixed-basket tax calculation tests
docs: update catalog, tax, menu, and pricing docs
```

## Handoff Notes

Depends on PLAN-0003 (Identity/Tenancy). Tax engine must be correct before orders are implemented. Tax tests are mandatory before any order or payment work begins. Next plan: PLAN-0005 (Payments, Receipts, Printing).
