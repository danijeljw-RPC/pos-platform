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

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
