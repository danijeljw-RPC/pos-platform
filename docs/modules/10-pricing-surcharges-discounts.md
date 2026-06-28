# Pricing, Surcharges, Discounts, Promotions, Loyalty

## Pricing engine wishlist

| Feature | Notes |
|---|---|
| Base price | Standard item price |
| Tax-inclusive/exclusive price | Per country/venue |
| Time-based pricing | Happy hour |
| Day-based pricing | Weekend pricing |
| Public holiday pricing | AU/NZ hospitality |
| Customer group pricing | Staff/wholesale/VIP |
| Location-specific pricing | Different stores |
| Size pricing | Small/medium/large |
| Modifier pricing | Extra shot +$1 |
| Bundle pricing | Meal deal |
| Combo pricing | Burger + chips + drink |
| Price override | Permissioned |
| Promotional price | Start/end dates |
| Multi-currency later | Global expansion |

## Surcharges and fees

Do not hard-code this. Make it configurable.

| Surcharge type | Notes |
|---|---|
| Card surcharge | By provider/card type if supported |
| Sunday surcharge | Hospitality |
| Public holiday surcharge | Hospitality |
| Service charge | Restaurants/functions |
| Delivery fee | Delivery orders |
| Packaging fee | Takeaway containers |
| Bottle deposit | Region-specific |
| Environmental levy | Region-specific |
| Minimum spend fee | Optional |
| Late-night surcharge | Bars/venues |
| Event surcharge | Stadium/festival/venue mode |

Each surcharge needs:

```text
Name
Type
Rate or fixed amount
Taxable yes/no
Applies to order types
Applies to payment methods
Applies by day/time/date
Shown on receipt yes/no
```

## Discount wishlist

| Feature | Notes |
|---|---|
| Line discount | Discount one item |
| Order discount | Discount whole order |
| Percentage discount | 10% off |
| Fixed discount | $5 off |
| Staff discount | Permissioned |
| Manager comp | Free item/meal |
| Promo code | Manual code |
| Automatic promotion | Buy 2 get 1 |
| Happy hour discount | Time-based |
| Loyalty discount | Customer-based |
| Coupon/voucher | Single-use or reusable |
| Reason required | Audit trail |

## Loyalty wishlist

| Feature | Notes |
|---|---|
| Customer profile | Name, email, phone |
| Points earning | Per dollar/item |
| Points redemption | Discount/payment |
| Stamp card | Buy 9 coffees get 10th free |
| Membership QR code | Scan customer |
| Birthday offer | Later |
| Store credit | Refund/store credit |
| Gift cards | Sell/redeem/top-up |
| Customer history | Purchases/returns |
