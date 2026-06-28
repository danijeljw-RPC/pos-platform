# Core POS Sales Screen

This is the main counter/staff screen.

## Wishlist

| Feature | Notes |
|---|---|
| Fast item search | Search by name, SKU, barcode, PLU |
| Category tiles | Coffee, Cakes, Burgers, Drinks, Services, etc. |
| Item tiles | Image, name, price, availability |
| Modifier support | Milk type, size, toppings, cooking preference |
| Quantity adjustment | `+`, `-`, direct quantity entry |
| Remove item | Void line with permission/audit |
| Price override | Manager permission required |
| Discounts | Line discount, order discount, promo code |
| Notes per item | “No onion”, “extra hot”, “gift wrapped” |
| Notes per order | “Customer waiting”, “deliver to table 4” |
| Hold order | Park an order and return later |
| Resume order | Open held/suspended orders |
| Split order | By item, by seat/person, by amount |
| Merge orders | Combine two orders/tables/tabs |
| Duplicate order | Useful for repeat customers |
| Repeat last item | Fast bar/cafe operation |
| Reprint receipt | With audit trail |
| Refund from order history | Full or partial refund |
| Exchange item | Retail flow |
| No-sale cash drawer open | Permission/audit required |

## Core sales flow

```text
Staff logs in with PIN
↓
Selects items/modifiers
↓
Order totals update
↓
Customer display updates
↓
Staff chooses payment method
↓
Payment is processed or recorded
↓
Receipt generated
↓
Order closed
```
