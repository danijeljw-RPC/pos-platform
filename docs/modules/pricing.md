# Module: Pricing Engine

The pricing engine resolves the final price for a product given active pricing rules.

See also: `docs/modules/10-pricing-surcharges-discounts.md`.

---

## Responsibilities (MVP)

- Base price per product.
- Modifier pricing (additional charge per modifier selection).
- Location-specific price overrides.
- Tax-inclusive pricing mode.

## Responsibilities (Phase 2+)

- Time-based pricing (happy hour, off-peak).
- Day-based pricing (Sunday rate, public holiday).
- Customer group pricing.
- Promotion pricing.
- Bundle/combo pricing.
- Manual price override (permissioned and audited).

## Implementation Status (PLAN-0004 Milestone E, 2026-07-05)

`ProductVariant.PriceDelta` and `Modifier.PriceDelta` (see `docs/modules/catalog.md`) are now configurable via the catalogue API, confirming this doc's "Modifier pricing (additional charge per modifier selection)" wording as a delta on the resolved base price — `+`/`-`, not an absolute price, and may legitimately be negative (a discount variant). No resolution logic exists yet: `PriceResolver` (Milestone F, `DaxaPos.Application.Pricing`) is what actually combines `Product.BasePrice` + variant delta + modifier deltas + location override into a final price.

## Implementation Status (PLAN-0004 Milestone F, 2026-07-05)

`PriceResolver.Resolve(Product, ProductVariant?, IReadOnlyList<Modifier>, ProductLocationOverride?, VenueTaxConfiguration?)` is implemented — pure, no EF/DB/HTTP dependency, TDD'd first per CLAUDE.md's Testing Rules (the second genuinely financial logic in the codebase after `TaxCalculationEngine`). Resolution order: `Product.BasePrice` + variant delta + modifier deltas, then `ProductLocationOverride.PriceOverride` (when set) **replaces that total outright** rather than adding to it — a deliberate Design Decision matching this doc's "location-specific price overrides" wording (an explicit venue-set price, not a further adjustment). `IsTaxInclusive` on the output comes from `VenueTaxConfiguration.TaxInclusivePricing`; a missing `VenueTaxConfiguration` fails closed (`PriceResolutionErrorCode.MissingVenueTaxConfiguration`) rather than silently assuming AU-style tax-inclusive pricing, mirroring the plan's approved Human Decision #5 one layer down at the pure-resolver boundary — the same pattern `TaxCalculationEngine` uses for missing tax configuration. The DB-touching resolution step (product/location/variant/modifier selection → the actual entities this function consumes) is still not built — that depends on `Order`, PLAN-0005.

- Entities: `ProductLocationOverride`, `VenueTaxConfiguration` (see `docs/modules/catalog.md`/`docs/modules/tax.md`).
- Tests: `tests/DaxaPos.UnitTests/Pricing/PriceResolverTests.cs` — base price only, variant delta, modifier deltas (combined and individually), negative/zero/positive deltas for both, location override replaces (not adds), no-override falls back to the computed total, tax-inclusive/exclusive mode from `VenueTaxConfiguration`, missing-configuration fail-closed, and determinism.

## Implementation Status (PLAN-0004 Milestone G, 2026-07-05)

The resolved-menu read endpoint (`docs/modules/menus.md`) is the first real caller of `PriceResolver.Resolve`. It always passes `variant: null` and `modifiers: []` — `MenuSectionItem` (Milestone G) carries only a `ProductId`, no variant/modifier selection, since that happens at order time (PLAN-0005), not on the menu display. `VenueTaxConfiguration` absence for the requested location fails the whole endpoint closed (404), matching this doc's already-recorded fail-closed behaviour rather than introducing a second one.

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
