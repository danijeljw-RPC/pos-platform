# Tax and Pricing Model

## Product tax category

Each product gets a tax category.

```text
Product
- Name
- Price
- TaxCategoryId
```

Example:

```text
Coffee
- Price: 5.50
- TaxCategory: AU_GST_10

Cake Slice
- Price: 8.80
- TaxCategory: AU_GST_10

Loaf of Bread
- Price: 6.00
- TaxCategory: AU_GST_FREE
```

## Tax snapshot on order line

At sale time, capture tax result onto the line.

```text
OrderLine
- ProductId
- ProductName
- Quantity
- UnitPriceInclTax
- LineTotalInclTax
- TaxCategoryCode
- TaxRatePercent
- TaxableAmount
- TaxAmount
- TaxTreatment
```

## Example line calculation

| Line | Total inc GST | Tax category | Taxable amount | GST |
| --- | ---: | --- | ---: | ---: |
| Flat white | $5.50 | AU_GST_10 | $5.00 | $0.50 |
| Chocolate cake slice | $8.80 | AU_GST_10 | $8.00 | $0.80 |
| Loaf of bread | $6.00 | AU_GST_FREE | $6.00 | $0.00 |

Order summary:

```text
Subtotal excluding GST:       $19.00
GST:                           $1.30
Total including GST:          $20.30
GST-free sales:                $6.00
```

## Pricing rules

Pricing rules should be applied before final tax calculation where jurisdiction rules require.

Supported pricing rules:

```text
Base price
Tax-inclusive/exclusive mode
Modifier pricing
Bundle pricing
Combo pricing
Time-based pricing
Day-based pricing
Public holiday pricing
Customer group pricing
Location-specific pricing
Promotion pricing
Manual override
```

## Rounding

The tax engine should explicitly support:

```text
Per-line rounding
Per-order rounding
Currency-specific rounding
Cash rounding where applicable
```
