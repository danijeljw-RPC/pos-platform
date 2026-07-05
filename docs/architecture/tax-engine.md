# Tax Engine Architecture — Daxa POS

The Daxa POS tax engine calculates tax per order line and stores immutable tax snapshots at sale time.

See also `docs/architecture/04-tax-pricing-model.md` for the detailed model.
See [ADR-0006](../adr/accepted/ADR-0006-tax-line-based-tax-engine.md) for the decision record.

---

## Design Principles

- Tax is calculated per order line, not per order.
- An order can contain a mix of taxable and tax-free items.
- Tax snapshots are stored on the order line at sale time.
- Tax snapshots are immutable after the order is finalised.
- Tax configuration changes do not retroactively affect completed orders.

---

## Tax Data Model

```text
TaxRate
├─ Id
├─ CountryCode
├─ RegionCode
├─ Name
├─ RatePercent
├─ TaxType
├─ IsCompound
├─ AppliesToTaxInclusivePrices
├─ Priority
└─ IsActive

TaxCategory
├─ Id
├─ Name
├─ Code            (e.g. AU_GST_10, AU_GST_FREE, NZ_GST_15)
├─ Description
└─ TaxTreatment    (Taxable, GSTFree, ZeroRated, Exempt)

OrderLineTax  (snapshot at sale time)
├─ OrderLineId
├─ TaxRateId
├─ TaxName
├─ RatePercent
├─ TaxableAmount
├─ TaxAmount
├─ JurisdictionName
└─ JurisdictionType
```

---

## AU/NZ Tax Categories

| Code | Description | Rate |
|------|-------------|------|
| AU_GST_10 | Australian GST 10% | 10% |
| AU_GST_FREE | Australian GST-free supply | 0% |
| NZ_GST_15 | New Zealand GST 15% | 15% |
| NZ_ZERO_RATED | NZ zero-rated supply | 0% |
| NZ_EXEMPT | NZ exempt supply | 0% |

---

## Mixed Basket Example (AU)

| Product | Price (incl.) | Tax Category | GST |
|---------|--------------|--------------|-----|
| Flat white | $5.50 | AU_GST_10 | $0.50 |
| Chocolate cake slice | $8.80 | AU_GST_10 | $0.80 |
| Loaf of bread | $6.00 | AU_GST_FREE | $0.00 |
| **Total** | **$20.30** | | **$1.30** |

---

## Receipt Presentation

```
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

---

## Design Limits

- Maximum 10 tax components per order line.
- Maximum 20 tax components per order.

---

## Implementation Status (PLAN-0004 Milestone B, 2026-07-04)

The `TaxRate`/`TaxCategory` model above was the original architecture-level sketch; the implemented schema (`src/DaxaPos.Domain/Entities/TaxDefinitionTemplate.cs`, `TaxDefinition.cs`, `TaxCategory.cs`, `TaxCategoryDefinition.cs`) splits "rate" into two tables rather than one:

- `TaxDefinitionTemplate` — the global, unfiltered, system-wide reference catalogue (this doc's 5-row AU/NZ table below, now seeded via EF Core `HasData`).
- `TaxDefinition` — a tenant-owned clone of a template, independently editable per OI-0007 (one tenant's rate edit must never leak into another's).
- `TaxCategoryDefinition` replaces the direct `TaxCategory` → rate link with a join that can be organisation-wide or location-specific, so one multi-location tenant spanning AU and NZ can share a single `Taxable` category resolving to different rates per location.

The calculation engine (`DaxaPos.Application.Tax.TaxCalculationEngine`) is pure and DB-independent, taking `TaxComponentSnapshot` value inputs rather than querying `TaxDefinition` directly — the DB-touching resolution step (product/location → applicable components) is a separate, later concern (Milestone C+). Fully unit-tested against this doc's exact AU mixed-basket example; see `tests/DaxaPos.UnitTests/Tax/TaxCalculationEngineTests.cs`.

## Implementation Status (PLAN-0004 Milestone C, 2026-07-05)

Tax configuration endpoints (`src/DaxaPos.Api/Endpoints/Tax/`) now let a tenant build the schema above through the API — cloning a `TaxDefinitionTemplate`, creating `TaxCategory` labels, and mapping them together via `TaxCategoryDefinition`. This milestone enforces this doc's per-line 10-component design limit at configuration time: creating an 11th active `TaxCategoryDefinition` for the same `(TaxCategoryId, LocationId)` pair is rejected with 400. The DB-touching resolution step (a product/location pair → the actual `TaxComponentSnapshot`s the pure engine consumes) is still not built — it depends on `Product` (Milestone D) and is a distinct concern from configuration CRUD. The per-order 20-component limit remains PLAN-0005's responsibility, unchanged from Milestone B's Architecture Assumptions.

## Planned: Localised Tax Labels (Deferred)

Tax label text ("GST", "Includes GST", tax-free marker legends) is planned to become translatable, extending the existing configurable-marker mechanism (ADR-0011) rather than replacing it. See [ADR-0016 — Multi-Language and Localisation Strategy](../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) (proposed, not yet implemented). AU/NZ wording remains the first concrete example; it is not hard-coded into the architecture.

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](../adr/accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [Architecture: 04-tax-pricing-model.md](04-tax-pricing-model.md)
- [Module: Tax](../modules/tax.md)
- [Region: AU/NZ Tax](../regions/01-au-nz-tax.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
