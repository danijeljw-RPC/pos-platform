# Tax Engine

The tax engine must support simple GST/VAT jurisdictions and complex stacked tax jurisdictions.

## Recommended limits

| Entity | Recommended max |
|---|---:|
| Tax components per item line | 10 |
| Tax components per order | 20 |
| Different tax rates per venue | Unlimited / configurable |
| Tax jurisdictions per venue | Unlimited / configurable |
| Tax categories per product | Unlimited / configurable |
| Active taxes applied to one item | Soft limit 10 |
| Receipt-visible tax summary lines | Soft limit 10–20 |

## Tax engine requirements

| Feature | Why |
|---|---|
| Tax-inclusive pricing | AU/NZ/UK/EU commonly show prices including GST/VAT |
| Tax-exclusive pricing | US commonly adds tax at checkout |
| Item-level tax category | Food, alcohol, clothing, service, repair, gift card, exempt item |
| Multiple tax components | US/Canada/local taxes |
| Compound tax | Some jurisdictions calculate one tax on top of another |
| Tax exemption | Wholesale, resale, charity, government, special customers |
| Zero-rated tax | Exports, special items |
| Tax-exempt items | Some food/services depending on country |
| Location-based tax | Store location / fulfilment location |
| Service charge taxability | Hospitality service fees may be taxable |
| Surcharge taxability | Card/public holiday/service surcharges may be taxable |
| Tip/gratuity tax handling | Needed for US hospitality |
| Rounding rules | Per-line vs per-order rounding differs |

## Taxes vs fees

Do not mix these together.

| Type | Example | Treat as |
|---|---|---|
| Tax | GST, VAT, sales tax | Tax line |
| Government fee | Bottle deposit, recycling levy | Fee/levy line, may or may not be taxable |
| Business surcharge | Card surcharge, Sunday surcharge | Charge line, may be taxable |
| Tip | US gratuity | Tip line, may have separate reporting |
| Discount | Promo, staff discount | Discount line, affects taxable base depending on rules |

## Suggested data model

```text
TaxRate
- Id
- CountryCode
- RegionCode
- Name
- RatePercent
- TaxType
- IsCompound
- AppliesToTaxInclusivePrices
- Priority
- IsActive

TaxCategory
- Id
- Name
- Code
- Description

ProductTaxCategory
- ProductId
- TaxCategoryId

VenueTaxConfiguration
- VenueId
- CountryCode
- RegionCode
- TaxInclusivePricing
- TaxCalculationMode

OrderLineTax
- OrderLineId
- TaxRateId
- TaxName
- RatePercent
- TaxableAmount
- TaxAmount
- JurisdictionName
- JurisdictionType
```

## Suggested enums

```csharp
public enum TaxPricingMode
{
    TaxInclusive,
    TaxExclusive
}

public enum TaxCalculationScope
{
    PerLine,
    PerOrder
}

public enum TaxComponentType
{
    Gst,
    Vat,
    SalesTax,
    CountyTax,
    CityTax,
    DistrictTax,
    ExciseTax,
    PreparedFoodTax,
    AlcoholTax,
    EnvironmentalFee,
    Other
}
```
