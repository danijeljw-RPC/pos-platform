# Offline, Devices, Admin Portal, and Product Management

## Offline/local resilience

Important for food trucks, pubs, cafes, and bad internet.

| Feature | Notes |
|---|---|
| Local order queue | Store unsynced orders |
| Offline sales mode | Limited but functional |
| Local menu cache | POS still usable |
| Local tax config cache | Important |
| Local price cache | Important |
| Sync when online | Push queued events |
| Conflict handling | Duplicate order/payment prevention |
| Idempotency keys | Mandatory |
| Device sync status | Clear indicator |
| Offline payment rules | Configurable |
| Local network mode | Venue server later if needed |

Integrated EFTPOS offline behaviour is provider-dependent. Some terminals support offline card processing, some do not. The POS should expose provider capability.

## Device and terminal management

| Feature | Notes |
|---|---|
| Register device | POS terminal registration |
| Device name | “Front Counter 1” |
| Device type | POS, customer display, admin, kiosk |
| Assign to venue | Location mapping |
| Assign payment terminal | EFTPOS pairing |
| Printer assignment | Receipt/bar/kitchen |
| Full-screen mode | Windows MAUI |
| Auto-start | POS opens on boot |
| Kiosk mode support | Windows Assigned Access later |
| Health status | Online/offline |
| App version tracking | Support |
| Remote config | Change settings centrally |

## Admin portal

| Area | Features |
|---|---|
| Dashboard | Sales, orders, alerts |
| Products | Items, categories, modifiers |
| Menus | Venue/menu availability |
| Pricing | Price lists, surcharges |
| Tax | Tax categories/rates |
| Payments | Provider setup |
| Terminals | Device/payment/printer setup |
| Staff | Users/roles/PINs |
| Customers | Loyalty/profile |
| Inventory | Stock/products/suppliers |
| Reports | Sales/tax/audit |
| Locations | Venues/regions |
| Settings | Branding, receipts, ordering rules |

## Product/menu management

| Feature | Notes |
|---|---|
| Categories | Food, drinks, cakes, services |
| Subcategories | Coffee > Hot/Iced |
| Products | Name, price, tax category |
| Product images | POS tiles |
| SKU/barcode | Retail |
| Variants | Size/colour/model |
| Modifier groups | Required/optional |
| Modifier limits | Min/max selection |
| Combo meals | Meal deals |
| Bundles | Retail/catering |
| Availability | By time/day/location |
| Sold-out toggle | Staff/admin |
| Product tags | Vegan, gluten-free, alcohol |
| Allergens | Hospitality |
| Print routing | Kitchen/bar/etc. |
| Tax category | GST/GST-free/VAT/etc. |
| Cost price | Margin reporting |
