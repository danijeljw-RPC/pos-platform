# Global Tax Handling

## Practical maximum

For normal POS retail/hospitality/service sales:

```text
Most common: 1 tax
Complex normal case: 3–5 taxes
Rare edge case: 6–10 tax components
Safe system design: 20 tax lines
```

## Recommended limits

```text
Maximum tax lines per order: 20
Maximum tax lines per order item: 10
```

## Market expectations

| Market | Typical tax lines on an order | Design capacity |
|---|---:|---:|
| Australia | 0–1 | 10+ |
| New Zealand | 0–1 | 10+ |
| UK | 0–1 VAT line, sometimes different VAT rates per item | 10+ |
| Singapore | 0–1 GST line | 10+ |
| Hong Kong | usually 0 sales tax/VAT | 10+ |
| USA | 1–5+ stacked taxes possible | 10+ |
| Canada | 1–3 | 10+ |
| EU | usually VAT, but can vary by item/country | 10+ |

## US-style stacked taxes

US sales tax can stack by jurisdiction:

```text
State sales tax
County sales tax
City sales tax
Special district tax
Transit / tourism / stadium / local authority tax
Restaurant / prepared food tax
Alcohol tax
Bottle deposit / recycling fee
```

Example:

```text
Item: Burger
Base price:              $10.00

State sales tax:          $0.60
County sales tax:         $0.15
City sales tax:           $0.10
Restaurant district tax:  $0.05

Total tax:                $0.90
Total:                   $10.90
```

## Required support

```text
Tax-inclusive pricing
Tax-exclusive pricing
Item-level tax category
Multiple tax components
Compound tax
Tax exemption
Zero-rated tax
Tax-exempt items
Location-based tax
Service charge taxability
Surcharge taxability
Tip/gratuity tax handling
Rounding rules
```
