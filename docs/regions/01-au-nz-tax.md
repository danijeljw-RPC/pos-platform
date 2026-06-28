# AU/NZ Tax Handling

## Australia

Australia uses GST at 10% for most taxable supplies.

Common POS setup:

```text
TaxInclusive = true
TaxCode = GST
TaxRate = 10%
```

## New Zealand

New Zealand uses GST at 15% for most taxable supplies.

Common POS setup:

```text
TaxInclusive = true
TaxCode = GST
TaxRate = 15%
```

## Mixed GST basket

An order can contain both GST-taxable and GST-free items.

Example:

| Product | Price | Tax treatment |
|---|---:|---|
| Flat white | $5.50 | GST 10% |
| Chocolate cake slice | $8.80 | GST 10% |
| Loaf of bread | $6.00 | GST-free |

Receipt:

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

## Calculation

For AU GST-inclusive prices:

```text
GST amount = GST-inclusive price / 11
```

Example:

```text
Flat white:           $5.50 / 11 = $0.50 GST
Chocolate cake slice: $8.80 / 11 = $0.80 GST
Loaf of bread:        $0.00 GST
Total GST:            $1.30
```

## Internal configuration

```text
Product: Flat white
TaxCategory: AU_GST_10

Product: Chocolate cake slice
TaxCategory: AU_GST_10

Product: Loaf of bread
TaxCategory: AU_GST_FREE
```

Tax categories:

```text
AU_GST_10
- Country: AU
- Rate: 10%
- PricingMode: TaxInclusive
- ReportAs: TaxableSale

AU_GST_FREE
- Country: AU
- Rate: 0%
- PricingMode: TaxInclusive
- ReportAs: GSTFreeSale

NZ_GST_15
- Country: NZ
- Rate: 15%
- PricingMode: TaxInclusive
- ReportAs: TaxableSale

NZ_ZERO_RATED
- Country: NZ
- Rate: 0%
- PricingMode: TaxInclusive
- ReportAs: ZeroRatedSale

NZ_EXEMPT
- Country: NZ
- Rate: 0%
- PricingMode: NoGST
- ReportAs: ExemptSupply
```

## Store tax snapshots

At sale time, copy tax calculation onto the order line.

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
