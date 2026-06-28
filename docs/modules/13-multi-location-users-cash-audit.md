# Multi-Location, Users, Cash, and Audit

## Multi-location and chains

| Feature | Notes |
|---|---|
| Organisation | Parent company |
| Region | APAC/NA/EMEA |
| Country | AU/NZ/SG/HK/UK/US/CA |
| Location/venue | Individual store |
| Terminal | POS device |
| Station | Bar/kitchen/customer display |
| Shared product catalogue | Chain-level products |
| Location-specific menu | Store-specific availability |
| Location-specific pricing | Different prices |
| Location-specific tax | Important globally |
| Central reporting | Head office |
| Store-level reporting | Venue manager |
| Franchise support | Franchisee access limits |
| Cross-location customer profile | Optional |
| Cross-location gift cards | Optional |

## Users, roles, permissions

| Role | Permissions |
|---|---|
| Owner | Everything |
| Admin | Configuration/reporting |
| Venue manager | Store-level management |
| Supervisor | Refunds, voids, discounts |
| Staff/cashier | Sales |
| Bar staff | Bar orders/payments |
| Kitchen staff | Prep only |
| Accountant | Reports/export |
| Support user | Limited support access |
| Franchise manager | Own stores only |

Permission examples:

```text
Can refund
Can void item
Can void order
Can apply discount
Can open cash drawer
Can change price
Can access reports
Can edit menu
Can manage tax settings
Can configure payment provider
Can view audit log
```

## Staff workflows

| Feature | Notes |
|---|---|
| Staff PIN login | Fast terminal access |
| User login | Admin/back office |
| Clock in/out | Timesheets |
| Break tracking | Later |
| Shift management | Open/close shifts |
| Cash float | Starting cash |
| Cash up | End-of-shift reconciliation |
| Blind cash count | Optional |
| Manager approval | Refunds/voids/discounts |
| Staff sales reporting | Sales by staff |
| Tip allocation | US/CA hospitality |

## Cash management

| Feature | Notes |
|---|---|
| Open cash drawer | Sale/no-sale |
| Starting float | Shift start |
| Cash payments | Change calculation |
| Paid in/out | Petty cash |
| Cash drop | Remove excess cash |
| End-of-day cash count | Reconciliation |
| Variance reporting | Expected vs counted |
| Drawer assignment | Per terminal/staff |
| Cash audit log | Every drawer event |

## Audit logging

Mandatory for a serious POS.

Audit events:

```text
Login/logout
Failed login
Open drawer
Void item
Void order
Refund
Price override
Discount applied
Tax setting changed
Payment provider changed
Receipt reprinted
Order reopened
Staff role changed
Menu item changed
Stock adjusted
Cash counted
Terminal paired/unpaired
```

Audit record fields:

```text
Who
What
When
Where
Terminal
Venue
Before value
After value
Reason
Linked order/payment/refund ID
```
