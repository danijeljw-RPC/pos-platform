# ADR-0006 — Tax-Line Based Tax Engine

## Status

Accepted

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

- See [OI-0007 — Tax Configuration Editing Permissions](../../issues/closed/OI-0007-tax-configuration-editing-permissions.md)
- Should AU GST rounding use standard ATO rounding rules per line?
- Should NZ GST calculation use the same engine with a 15% rate?

## Related Documents

- [Architecture: Tax Engine](../../architecture/tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](ADR-0011-receipt-tax-marker-strategy.md)
- [Module: Tax](../../modules/tax.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [Region: AU/NZ Tax](../../regions/01-au-nz-tax.md)

---

## Acceptance Addendum

ADR-0006 is accepted.

The tax engine will remain **tax-line based** and **configuration-driven**.

The system will not hard-code tax behaviour specifically for AU GST, NZ GST, VAT, US sales tax, or any other named tax. Instead, each tax is configured as a tax definition with its own calculation rules, rate, rounding behaviour, display name, and reporting metadata.

## Resolution of Open Questions

### Should AU GST rounding use standard ATO rounding rules per line?

The system will not make a hard-coded AU GST-specific decision inside the tax engine.

AU GST should be configured as a tax definition.

That tax definition may be configured with the required GST rate, rounding behaviour, calculation basis, receipt display behaviour, and reporting metadata.

For example, an AU GST tax definition may include:

```text
TaxName: GST
RatePercent: 10
JurisdictionName: Australia
JurisdictionType: Country
RoundingMode: NearestCent
CalculationScope: PerLine
IncludedInPrice: true
ReceiptMarker: GST
```

The important rule is that the rounding behaviour belongs to the configured tax definition, not to hard-coded application logic.

If AU GST requires standard rounding behaviour, that behaviour is configured on the AU GST tax definition.

### Should NZ GST calculation use the same engine with a 15% rate?

Yes.

NZ GST will use the same tax-line based tax engine.

The engine does not need to know that the tax is “NZ GST” as a special case. NZ GST is simply configured as another tax definition with its own rate and rules.

For example:

```text
TaxName: GST
RatePercent: 15
JurisdictionName: New Zealand
JurisdictionType: Country
RoundingMode: NearestCent
CalculationScope: PerLine
IncludedInPrice: true
ReceiptMarker: GST
```

The same engine must support AU GST, NZ GST, GST-free items, VAT, stacked taxes, and future tax types by reading the configured tax definition instead of branching on country-specific tax names.

## Accepted Design Direction

Tax rules are configured by authorised admin or manager-level catalogue users.

Each tax definition should support configuration for:

- Tax name.
- Tax rate percentage.
- Jurisdiction name.
- Jurisdiction type.
- Whether the tax is included in the displayed product price.
- Whether the tax is added on top of the product price.
- Rounding mode.
- Rounding precision.
- Calculation scope, such as per-line or per-component.
- Receipt display marker.
- Reporting category.
- Whether the tax is active or archived.

The tax engine must calculate tax from these configured rules.

The tax engine must not contain hard-coded logic such as:

```text
if country == "AU" then use AU GST rules
if country == "NZ" then use NZ GST rules
if taxName == "GST" then apply special GST behaviour
```

Instead, the configured tax definition determines how the tax behaves.

## Product Tax Assignment

Products are assigned to tax categories or tax definitions.

At sale time, the order line captures an immutable tax snapshot based on the product’s tax configuration at that moment.

If the tax configuration later changes, historical order lines must not be recalculated.

Historical sales, receipts, reports, and tax summaries must continue to use the tax snapshot captured at the time of sale.

## Relationship to OI-0007

OI-0007 defines who can edit tax configuration and how tax changes take effect.

ADR-0006 defines how the tax engine calculates and stores tax.

Together, the accepted rule is:

- Tax configuration is managed by authorised catalogue-management users.
- Tax changes are immediate from that point forward.
- Existing product records affected by tax changes are archived.
- New product records are created with the updated tax configuration.
- Historical order tax snapshots remain immutable.
- The tax engine calculates tax from configured tax definitions, not hard-coded country rules.

## Consequence

This keeps the tax engine generic and reusable.

AU GST and NZ GST are both supported, but they are not special engine modes. They are tax configurations.

This allows Daxa POS to support additional tax systems later without redesigning the tax engine.

Examples include:

- AU GST.
- AU GST-free items.
- NZ GST.
- VAT.
- US state, county, and city stacked taxes.
- Hospitality-specific local taxes.
- Future tax rules that require different rounding or reporting behaviour.

## Status Update

Status: **Accepted**
