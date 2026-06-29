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

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [ADR-0006 — Tax-Line Based Tax Engine](../adr/proposed/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/proposed/ADR-0011-receipt-tax-marker-strategy.md)
