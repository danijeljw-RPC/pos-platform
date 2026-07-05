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

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [ADR-0006 — Tax-Line Based Tax Engine](../adr/accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [ADR-0016 — Multi-Language and Localisation Strategy](../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) (proposed, unchanged) — tax/legal label localisation is planned but deferred, not part of PLAN-0004 Milestone B or the initial MVP. The new `Name`/`ReceiptMarkerLabel` columns added by Milestone B are already mapped as plain invariant/fallback text per this ADR's pre-recorded constraint, so no rework is anticipated if/when it is accepted and implemented.
