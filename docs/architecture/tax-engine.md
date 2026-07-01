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

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](../adr/accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [Architecture: 04-tax-pricing-model.md](04-tax-pricing-model.md)
- [Module: Tax](../modules/tax.md)
- [Region: AU/NZ Tax](../regions/01-au-nz-tax.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
