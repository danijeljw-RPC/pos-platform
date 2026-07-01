# ADR-0011 — Receipt Tax Marker Strategy

## Status

Accepted

## Context

AU and NZ GST receipts must indicate which items are GST-free. The product name must remain the product name — it must not be replaced with a generic label like "GST-free item".

A receipt marker approach (e.g. `F` next to the item, with a footer legend) is consistent with ATO guidance for tax invoices containing mixed taxable and GST-free supplies.

## Decision

Daxa POS uses a **single-character tax marker** (`F`) on receipt lines for GST-free items.

- Tax markers are printed next to the price on the receipt line.
- The receipt includes a footer legend: `F = GST-free`.
- Product names are never replaced with tax descriptions.
- Multiple marker codes may be supported in future for other tax treatments.
- The GST summary section shows total GST included and total GST-free sales.

**Receipt example:**

```text
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
- Compliant with ATO mixed-supply tax invoice requirements.
- Product names remain product names.
- Clear to the customer which items are GST-free.
- Extensible to additional markers for other tax treatments.

**Negative:**
- Requires receipt layout to accommodate marker column.
- Thermal receipt line length must accommodate the marker without truncating product names.

## Alternatives Considered

1. **Print "(GST-free)" after product name** — Considered. Adds clutter to product name column.
2. **Separate section for GST-free items** — Rejected. Breaks natural order of items as entered.
3. **No marker, only footer summary** — Rejected. Customer cannot identify which specific items are GST-free.

## Open Questions

- What marker code should be used for NZ GST zero-rated items?
- Should the marker be configurable by tax category?

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](ADR-0006-tax-line-based-tax-engine.md)
- [Module: Tax](../../modules/tax.md)
- [Module: Receipts](../../modules/receipts.md)
- [Region: AU/NZ Tax](../../regions/01-au-nz-tax.md)

---

## Acceptance Addendum

ADR-0011 is accepted with a country-agnostic receipt tax marker strategy.

The receipt marker system is not specific to AU GST, NZ GST, or any other named tax regime.

The default marker code for a tax-free line item is:

```text
F
```

The meaning of `F` is:

```text
F = Tax-free
```

This marker indicates that the receipt line item has been treated as tax-free according to the configured tax rules for that location.

## Revised Decision

Daxa POS will support configurable receipt tax markers.

Receipt tax markers may be assigned by:

- Product category.
- Product item.
- Tax category.
- Tax definition.
- Location-level receipt settings.

The marker code displayed on the receipt must be configurable in the location settings.

The default tax-free marker is `F`, but the system must not hard-code `F` as the only possible marker.

Different locations, jurisdictions, or customers may choose another marker if required.

Examples:

```text
F = Tax-free
Z = Zero-rated
E = Exempt
N = Non-taxable
```

These examples are not hard-coded rules. They are configurable marker labels.

## Receipt Behaviour

When a product line is tax-free, the receipt may print the configured marker beside the line item.

Example:

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Tax included                  $1.30

F = Tax-free
```

Product names must remain product names.

The product name must not be replaced with a tax description such as:

```text
GST-free item
Tax-free item
Zero-rated item
```

The marker is a display aid only. The source of truth is the tax snapshot recorded against the sale line.

## Configuration Rule

Each location should be able to configure the receipt marker settings independently.

This supports multi-location clients where different locations may operate under different tax, receipt, or reporting expectations.

A location-level configuration may include:

```text
TaxFreeMarkerCode: F
TaxFreeMarkerLabel: Tax-free
ShowTaxMarkerOnReceiptLines: true
ShowTaxMarkerLegendInFooter: true
```

The marker code should be short enough for thermal receipt layouts. A single character is preferred, but the configuration should not prevent future support for short multi-character markers if required.

## Relationship to Tax Configuration

Tax marker display is separate from tax calculation.

Tax calculation is handled by ADR-0006 — Tax-Line Based Tax Engine.

Receipt marker display is handled by this ADR.

The tax engine determines whether a line item has tax applied, no tax applied, zero-rated tax, exempt tax, or another configured treatment.

The receipt renderer uses the captured tax result and location receipt settings to decide which marker, if any, should be printed.

## Category and Item Overrides

Tax-free receipt markers should be configurable at category level and item level.

Category-level configuration allows a whole group of products to inherit the same marker behaviour.

Item-level configuration allows a specific product to override the category default.

Recommended precedence:

```text
Item receipt tax marker override
    ↓
Product tax category marker
    ↓
Tax definition marker
    ↓
Location default marker
```

This allows simple configuration for most products while still supporting exceptions.

## Historical Receipts

Receipt tax marker meaning must be preserved for historical receipts.

When a sale is completed, the tax marker code and marker label used for each receipt line should be captured as part of the sale/receipt snapshot.

If the location later changes the marker from `F = Tax-free` to another code, historical receipt reprints must continue to show the marker meaning that applied at the time of sale.

## Resolution of Open Questions

### What marker code should be used for NZ GST zero-rated items?

The system will not hard-code a NZ-specific marker.

NZ GST zero-rated items should use the marker configured for that location, tax definition, tax category, or item.

If a location wants to use `F`, it can configure:

```text
F = Tax-free
```

If a location wants a different marker, such as `Z`, it can configure:

```text
Z = Zero-rated
```

### Should the marker be configurable by tax category?

Yes.

The marker should be configurable by tax category and optionally overridden by product item.

This allows one marker strategy to apply across a group of products while still supporting specific product-level exceptions.

## Consequence

ADR-0011 is accepted as a configurable, country-agnostic receipt tax marker strategy.

The default implementation uses `F = Tax-free`, but the system must allow the marker code and legend text to be configured per location.

This keeps the receipt layout simple while avoiding hard-coded AU/NZ assumptions.

## Status Update

Status: **Accepted**
