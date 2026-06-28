# Product Vision

## Goal

Build a new POS platform that can operate across retail, hospitality, food service, and service-based businesses.

The product should work for:

| Business type | Examples |
|---|---|
| Hospitality | Cafe, pub, restaurant, bar |
| Food retail | Bakery, cake counter, food truck, fast food |
| General retail | Clothing, electronics, gifts |
| Services | Computer repair, electronics repair, appointment/service counter |
| Chains | Multi-location retail, franchise, group venues |

The system should be one configurable platform, not a separate product per vertical.

## Target deployment model

```text
One POS platform
├─ Hospitality
├─ Retail
├─ Food truck
├─ Bakery
├─ Pub/bar
├─ Restaurant
├─ Fast food
├─ Electronics store
├─ Repair/service business
└─ Multi-location chains
```

## Device strategy

| Device / OS | Recommended app type |
|---|---|
| Windows POS terminal | .NET MAUI |
| Windows customer-facing second display | .NET MAUI second window |
| Windows admin/back-office terminal | MAUI or browser |
| Linux kiosk / mini PC / Raspberry Pi-style device | PWA in Chromium kiosk mode |
| Android tablet | PWA |
| iPad | PWA |
| Customer self-ordering kiosk | PWA + OS kiosk lockdown |
| KDS / kitchen display | Separate PWA or separate device app |

## Point-of-sale screen model

For a counter POS:

```text
One Windows POS machine
├─ Staff-facing touch screen
└─ Customer-facing display
```

The customer display is not a KDS. It is a customer-facing display for order summary, total, payment state, receipt QR, loyalty prompt, and branding.

## Core customer display states

| State | Display |
|---|---|
| Idle | Logo, promo, daily specials |
| Order building | Items, quantities, total |
| Item removed | Updated basket |
| Discount applied | Discount line |
| Surcharge applied | Surcharge line |
| Payment started | “Please tap/insert/swipe” |
| Payment approved | “Payment approved” |
| Payment declined | “Payment declined, see staff” |
| Receipt | QR/email/SMS option |
| Loyalty | Scan membership QR |
| Tip prompt | Mainly US/CA |
