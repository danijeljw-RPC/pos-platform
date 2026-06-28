# Customer Display, Receipts, and Printing

## Customer-facing display

For the second screen at the POS counter.

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

For Windows MAUI:

```text
Screen 1 = Staff POS
Screen 2 = Customer display
```

## Receipts and tax invoices

| Feature | Notes |
|---|---|
| Thermal receipt | ESC/POS printer support |
| Email receipt | Later |
| SMS receipt | Later |
| Reprint receipt | With audit trail |
| Tax invoice | ABN/GST/VAT details |
| Gift receipt | No price |
| Refund receipt | Linked to original sale |
| Kitchen docket | Prep station ticket |
| Bar docket | Drinks ticket |
| Item labels | Bakery/retail |
| QR receipt | Link to digital receipt |
| Custom logo | Thermal-compatible |
| Receipt templates | Per venue |
| Tax markers | `F = GST-free` etc. |

Receipt should support:

```text
Business name
ABN/NZBN/company number
Venue address
Order number
Terminal ID
Staff name/code
Date/time
Items
Discounts
Surcharges
Tax summary
Payment method
Provider transaction ID
Refund policy
```

## Correct mixed GST receipt example

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

## Printing wishlist

| Feature | Notes |
|---|---|
| Receipt printer | Thermal ESC/POS |
| Kitchen printer | Dockets |
| Bar printer | Drinks |
| Label printer | Bakery/retail |
| Network printer | Ethernet/Wi-Fi |
| USB printer | Windows terminal |
| Bluetooth printer | Later/mobile |
| Print routing | By item/category |
| Reprint | Permission/audit |
| Printer health | Online/offline/paper out if available |
| Template engine | Custom receipt/docket layout |
