# ADR-0006 — Tax-Line Based Tax Engine

## Status

Proposed

## Context

Daxa POS must handle mixed-tax baskets: for example, a cafe selling GST-applicable coffee alongside GST-free items (e.g. a loaf of bread). A single `Order.TaxRate` or `Order.TaxAmount` field cannot represent this correctly.

Additionally, the platform must eventually support US-style stacked taxes (state + county + city), VAT in Europe, and other multi-component tax jurisdictions.

## Decision

Tax is calculated **per order line** and stored as one or more tax lines per order line.

- Each product has a `TaxCategory` (e.g. `AU_GST_10`, `AU_GST_FREE`, `NZ_GST_15`).
- At sale time, the tax engine calculates tax for each order line and captures a tax snapshot directly on the order line.
- Tax snapshots are immutable after the order is finalised.
- The order-level tax summary is derived by aggregating line-level tax snapshots.
- There is no single `Order.TaxRate` field.

**Design limits:**
- Maximum 10 tax components per order line.
- Maximum 20 tax components per order.

**Tax snapshot per order line includes:**

```
TaxRateId
TaxName
RatePercent
TaxableAmount
TaxAmount
JurisdictionName
JurisdictionType
```

**Receipt example (AU mixed basket):**

```
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

## Consequences

**Positive:**
- Supports AU/NZ mixed baskets correctly.
- Supports future US stacked taxes, VAT, and other multi-component tax jurisdictions.
- Historical order records remain accurate even if tax rates change later.
- Tax reports are derived from immutable snapshots.

**Negative:**
- More complex data model than a single `Order.TaxRate` field.
- Tax engine implementation requires careful rounding handling.
- Test coverage for mixed baskets and global tax scenarios must be comprehensive.

## Alternatives Considered

1. **Single `Order.TaxRate` field** — Rejected. Cannot represent mixed-tax baskets or multi-jurisdiction tax.
2. **Order-level tax only (no line snapshots)** — Rejected. Cannot provide accurate per-item tax reporting or correct historical records when tax rates change.

## Open Questions

- See [OI-0007 — Tax Configuration Editing Permissions](../../issues/open/OI-0007-tax-configuration-editing-permissions.md)
- Should AU GST rounding use standard ATO rounding rules per line?
- Should NZ GST calculation use the same engine with a 15% rate?

## Related Documents

- [Architecture: Tax Engine](../../architecture/tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](ADR-0011-receipt-tax-marker-strategy.md)
- [Module: Tax](../../modules/tax.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [Region: AU/NZ Tax](../../regions/01-au-nz-tax.md)
