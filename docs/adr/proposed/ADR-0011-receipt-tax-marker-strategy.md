# ADR-0011 — Receipt Tax Marker Strategy

## Status

Proposed

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
