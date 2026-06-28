# Order Types

The POS should not assume every order is the same.

## Order type wishlist

| Order type | Useful for |
|---|---|
| Dine-in | Restaurants, pubs, cafes |
| Takeaway | Cafes, bakeries, fast food |
| Delivery | Restaurant delivery |
| Pickup | Pre-order / click-and-collect |
| Drive-through | Fast food |
| Table service | Restaurants/pubs |
| Bar tab | Pubs/bars |
| Room charge | Hotels, clubs, venues |
| Account sale | Business customers |
| Service job | Computer repair, electronics repair |
| Quote | Services/retail |
| Lay-by / deposit | Retail |
| Catering order | Bakery/cafe |
| Event order | Pubs/restaurants/function rooms |

## Recommended model

```text
Order
- OrderType
- ServiceMode
- FulfilmentMode
- TableId nullable
- CustomerId nullable
- DueAt nullable
- Status
```
