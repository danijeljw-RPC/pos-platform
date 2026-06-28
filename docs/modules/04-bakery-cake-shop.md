# Bakery and Cake Shop Features

Bakery and cake shop operations have specific workflows.

## Wishlist

| Feature | Notes |
|---|---|
| Daily production quantities | 30 croissants, 12 loaves, 8 cakes |
| Sold-out tracking | Decrement finished goods |
| Pre-order cakes | Pickup date/time |
| Custom cake orders | Flavour, size, message, notes |
| Deposits | Pay deposit now, balance later |
| Pickup workflow | Pending, preparing, ready, collected |
| Labels | Product labels, allergens, pickup labels |
| Batch production | Morning batch, afternoon batch |
| Shelf-life tracking | Optional |
| Waste/spoilage | End-of-day unsold items |
| Catering boxes | Mixed item bundles |
| Tax-category control | Bread/GST-free etc. |
| Modifier groups | Size, filling, icing, message |

## Example tax display

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

## Cake order model

```text
CakeOrder
- OrderId
- PickupDateTime
- CakeSize
- Flavour
- Filling
- Icing
- MessageText
- SpecialInstructions
- DepositAmount
- BalanceDue
- Status
```
