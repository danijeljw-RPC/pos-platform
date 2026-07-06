# Daxa POS Product Structure

## Product name

```text
Daxa POS
```

Daxa POS is the core point-of-sale platform.

It is designed as a single configurable POS system that can support:

- Hospitality
- Cafes
- Bakeries
- Cake shops
- Food trucks
- Pubs and bars
- Restaurants
- Fast food
- Retail stores
- Clothing stores
- Electronics stores
- Repair and service businesses
- Multi-location chains
- Franchise-style organisations

Core positioning:

```text
Daxa POS
Run it cloud, local, or hybrid.
```

The product should use one core codebase and one platform architecture, with deployment mode controlled by configuration and infrastructure setup.

---

## Product line

### Daxa POS

Daxa POS is the main product name for the platform.

It includes the overall POS system:

- Order entry
- Products and menus
- Payments
- Refunds
- Receipts
- Tax handling
- Discounts
- Surcharges
- Inventory
- Customer display
- Reporting
- Staff permissions
- Audit logging
- Multi-location support
- Cloud/local/hybrid deployment modes

Daxa POS is not a separate product from Daxa Cloud or Daxa Local. It is the umbrella product.

```text
Daxa POS
├─ Daxa Cloud
├─ Daxa Local
├─ Daxa Hybrid
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa Back Office
├─ Daxa Payments
├─ Daxa Inventory
├─ Daxa KDS
├─ Daxa Sync
├─ Daxa Hospitality
└─ Daxa Retail
```

---

## Deployment options

### Daxa Cloud

Daxa Cloud is the fully cloud-hosted deployment of Daxa POS.

In this model, the master system runs in Daxa-managed cloud infrastructure.

The customer does not need to run their own server onsite unless they want local resilience or local device integration.

### Daxa Cloud is best for

| Customer type | Reason |
| --- | --- |
| Small business | No onsite server required |
| Cafe / bakery / retail store | Simple deployment |
| Multi-location chain | Centralised management |
| Franchise group | Easier head-office visibility |
| Businesses with reliable internet | Lower local infrastructure burden |

### Daxa Cloud responsibilities

Daxa Cloud handles:

- Central application hosting
- Tenant data storage
- User management
- Product catalogue
- Venue/location configuration
- Reporting
- Payment-provider configuration
- Device registration
- Audit logs
- Backups
- Updates
- Monitoring
- Cloud API access

### Daxa Cloud topology

```text
Daxa Cloud
├─ Tenant / organisation data
├─ Product catalogue
├─ Orders
├─ Payments and refunds
├─ Reporting
├─ Audit logs
├─ Admin portal
└─ APIs

Venue devices
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa KDS / PWA
├─ Printers
└─ Payment terminals
```

### Daxa Cloud behaviour

```text
Venue device
↓
Connects to Daxa Cloud
↓
Downloads configuration
↓
Creates orders/payments/refunds
↓
Pushes events to cloud
↓
Receives updated menu/pricing/tax/device config
```

---

## Daxa Local

Daxa Local is the on-premises/local-server deployment of Daxa POS.

In this model, the customer runs a local Daxa server onsite inside their own network.

The local server becomes the master operational system for that venue or group of venues, depending on configuration.

### Daxa Local is best for

| Customer type | Reason |
| --- | --- |
| Pubs | Local resilience |
| Restaurants | Local order/payment continuity |
| Food trucks | Can operate with poor internet |
| Larger venues | Local device integrations |
| Remote sites | Internet may be unreliable |
| Customers wanting local control | Onsite server ownership |
| Customers with existing IT teams | Can manage local infrastructure |

### Daxa Local responsibilities

The local server can handle:

- Local order processing
- Local product/menu cache
- Local tax configuration
- Local pricing rules
- Local terminal registration
- Local printer routing
- Local payment terminal routing
- Local audit capture
- Local reporting
- Local user/PIN login
- Local offline resilience
- Optional cloud backup/sync

### Daxa Local topology

```text
Venue local network
├─ Daxa Local Server
│  ├─ Local API
│  ├─ Local database
│  ├─ Local sync service
│  ├─ Local reporting
│  └─ Local device registry
│
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa KDS / PWA
├─ Receipt printers
├─ Kitchen/bar printers
├─ Payment terminals
└─ Admin devices
```

### Daxa Local behaviour

```text
Daxa Terminal
↓
Connects to Daxa Local Server
↓
Processes orders locally
↓
Routes payments/printing locally
↓
Stores operational data locally
↓
Optionally syncs/backs up to Daxa Cloud or external storage
```

---

## Daxa Hybrid

Daxa Hybrid is the deployment mode where both local and cloud components are used.

This is likely the most powerful model for serious venues and chains.

In this model:

- The cloud remains the central management/reporting layer.
- The local server keeps the venue operational if internet drops.
- Data syncs between the local server and the cloud.

### Daxa Hybrid is best for

| Customer type | Reason |
| --- | --- |
| Multi-location hospitality groups | Local resilience + head-office reporting |
| Larger pubs/restaurants | Avoid cloud dependency at the counter |
| Food trucks/events | Can sell locally, sync later |
| Chains/franchises | Central config with local execution |
| Remote/regional venues | Handles unreliable internet |
| Customers needing backups | Local + cloud data paths |

### Daxa Hybrid topology

```text
Daxa Cloud
├─ Tenant / organisation management
├─ Central product catalogue
├─ Central reporting
├─ Central audit log aggregation
├─ Payment provider configuration
├─ Backup/data lake/export layer
└─ Remote admin portal

        ⇅ sync

Daxa Local Server
├─ Local order processing
├─ Local database
├─ Local terminal/device control
├─ Local printer routing
├─ Local payment routing
├─ Local audit capture
└─ Local offline mode

        ⇅ local network

Venue devices
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa KDS / PWA
├─ Printers
└─ Payment terminals
```

### Daxa Hybrid behaviour

```text
Cloud defines master configuration
↓
Local server receives configuration
↓
Venue operates locally
↓
Orders/payments/refunds are captured locally
↓
Data syncs back to cloud
↓
Cloud provides reporting, backup, monitoring, and central management
```

### Hybrid sync examples

Data pushed from cloud to local:

- Products
- Categories
- Menus
- Prices
- Tax settings
- Surcharges
- Staff/users
- Permissions
- Receipt templates
- Payment provider settings
- Device configuration

Data pushed from local to cloud:

- Orders
- Payments
- Refunds
- Cash events
- Stock movements
- Audit logs
- Device health
- Shift summaries
- End-of-day reports

---

## Product components

### Daxa Terminal

Daxa Terminal is the staff-facing POS app.

For Windows devices, this is expected to be a .NET MAUI application.

### Daxa Terminal does

- Staff PIN login
- Product/category display
- Order entry
- Modifier selection
- Quantity changes
- Discounts
- Surcharges
- Hold/resume orders
- Split/merge orders later
- Payment initiation
- Refund initiation
- Receipt printing
- Cash drawer events
- Local/cloud sync awareness
- Customer display control
- Device health reporting

### Daxa Terminal target devices

| Device | App type |
| --- | --- |
| Windows POS terminal | .NET MAUI |
| Windows touchscreen counter terminal | .NET MAUI |
| Windows kiosk-style staff terminal | .NET MAUI |
| Tablet/browser fallback | PWA, if needed |

---

### Daxa Display

Daxa Display is the customer-facing second screen.

It is not the KDS.

It is used at the point of sale so the customer can see:

- Items being added
- Quantities
- Discounts
- Surcharges
- Total
- Tax markers where appropriate
- Payment prompt
- Payment approved/declined status
- Receipt QR/email/SMS option later
- Loyalty prompt later
- Branding/promotions when idle

### Daxa Display topology

```text
Windows POS machine
├─ Screen 1: Daxa Terminal
└─ Screen 2: Daxa Display
```

### Daxa Display states

| State | Display |
| --- | --- |
| Idle | Logo, promo, daily specials |
| Order building | Items, quantities, total |
| Discount applied | Discount line |
| Surcharge applied | Surcharge line |
| Payment started | “Please tap/insert/swipe” |
| Payment approved | “Payment approved” |
| Payment declined | “Payment declined, see staff” |
| Receipt | QR/email/SMS option later |
| Loyalty | Scan membership QR later |

---

### Daxa Back Office

Daxa Back Office is the admin and management portal.

This should normally be a web application/PWA.

### Daxa Back Office does

- Dashboard
- Product management
- Menu management
- Pricing
- Surcharges
- Tax configuration
- Payment provider setup
- Terminal/device setup
- Printer setup
- Staff/user management
- Role/permission management
- Customer management
- Inventory management
- Gift cards/vouchers later
- Reporting
- Audit log review
- Multi-location management
- Cloud/local/hybrid configuration

### Daxa Back Office users

| User type | Purpose |
| --- | --- |
| Owner | Whole organisation access |
| Admin | Configuration and reporting |
| Venue manager | Store-level management |
| Accountant | Reporting/export access |
| Support user | Limited support/debug access |
| Franchise manager | Own locations only |

---

### Daxa Payments

Daxa Payments is the payment integration layer.

It is not necessarily a payment processor itself.

It provides a provider-agnostic adapter system that allows venues to connect their own payment-provider account and terminal.

### Daxa Payments should support

- Cash
- Manual external EFTPOS
- Integrated EFTPOS
- Gift card
- Store credit
- Voucher
- Account/customer charge
- Deposit
- Split payment
- Refunds
- Tips/gratuity later
- Card/public holiday/Sunday/service surcharges

### Daxa Payments provider roadmap

AU/NZ first:

```text
1. Manual EFTPOS
2. Tyro
3. Zeller
4. Square
5. Stripe Terminal
6. Windcave
```

International expansion:

```text
7. Adyen
8. Worldline
9. Global Payments
```

### Payment adapter model

```text
Daxa Payments
├─ Tyro Adapter
├─ Zeller Adapter
├─ Square Adapter
├─ Stripe Terminal Adapter
├─ Windcave Adapter
├─ Adyen Adapter
├─ Worldline Adapter
└─ Global Payments Adapter
```

### Payment flow

```text
Staff presses Pay
↓
Daxa creates payment request
↓
Selected provider adapter sends amount to terminal
↓
Customer taps/inserts/swipes
↓
Provider returns approved/declined/cancelled
↓
Daxa records payment result
↓
Order closes or retries
```

---

### Daxa Inventory

Daxa Inventory manages stock and product availability.

It should start simple and support more advanced inventory later.

### Simple inventory

- Track finished items
- Sold-out flag
- Stock decrement on sale
- Stock adjustment
- Waste/spoilage
- Low stock warning
- Daily stock reset

### Advanced inventory later

- Ingredient inventory
- Recipes/BOM
- Purchase orders
- Supplier invoices
- Supplier management
- Stock transfer
- Batch/expiry tracking
- Serial number tracking
- Stocktake
- Margin reporting

### Industry examples

| Business | Inventory need |
| --- | --- |
| Bakery | Daily production quantities, waste, sold-out |
| Food truck | Event stock countdown |
| Retail | SKU/barcode stock |
| Electronics | Serial number/IMEI tracking |
| Repair shop | Parts used on jobs |
| Restaurant | Ingredient/BOM later |

---

### Daxa KDS

Daxa KDS is the kitchen display system.

It should be separate from the POS counter customer display.

Daxa KDS should usually run as a PWA on separate devices.

### Daxa KDS does

- Shows kitchen/bar/prep tickets
- Receives routed items
- Supports station views
- Supports item/order status
- Supports pending/preparing/ready states
- Handles void/cancel notifications
- Supports multiple prep stations
- Supports kitchen/bar/coffee routing

### KDS topology

```text
Daxa Terminal
↓
Daxa API / Local Server
↓
Daxa KDS devices
├─ Kitchen
├─ Bar
├─ Coffee
└─ Dessert / Bakery prep
```

---

### Daxa Sync

Daxa Sync is the local-to-cloud synchronisation layer.

It is important for Daxa Local and Daxa Hybrid.

### Daxa Sync does

- Pushes local sales data to cloud
- Pulls cloud configuration to local server
- Syncs audit logs
- Syncs product/menu changes
- Syncs price/tax/surcharge settings
- Syncs terminal/device configuration
- Handles retry
- Handles conflicts
- Uses idempotency keys
- Tracks sync status
- Supports backup/export flows

### Sync directions

Cloud to local:

```text
Products
Menus
Prices
Tax settings
Surcharges
Staff
Permissions
Receipt templates
Payment provider settings
Device configuration
```

Local to cloud:

```text
Orders
Payments
Refunds
Cash events
Stock movements
Audit logs
Device health
Shift summaries
End-of-day reports
```

---

### Daxa Hospitality

Daxa Hospitality is a configured industry template/module for hospitality venues.

It is not a separate codebase.

It enables relevant workflows for:

- Cafes
- Restaurants
- Pubs
- Bars
- Fast food
- Bakeries
- Cake shops
- Food trucks

### Daxa Hospitality features

- Tables/floor plan
- Dine-in/takeaway
- Bar tabs
- Split bills
- Modifiers
- Course firing later
- Send/hold/fire later
- Kitchen/bar routing
- Surcharges
- Tips later for US/CA
- KDS integration
- Prep notes
- Sold-out items
- Customer display
- Receipt printing

---

### Daxa Retail

Daxa Retail is a configured industry template/module for retail and service businesses.

It is not a separate codebase.

It enables relevant workflows for:

- Clothing stores
- Electronics stores
- General retail
- Gift stores
- Repair shops
- Service counters

### Daxa Retail features

- SKU/barcode scanning
- Product variants
- Returns/exchanges
- Gift receipts
- Stock control
- Serial numbers later
- IMEI tracking later
- Warranty tracking later
- Lay-by/deposit later
- Special orders later
- Service jobs later
- Customer history
- Store credit
- Gift cards/vouchers

---

## Single codebase approach

Daxa POS should remain one codebase.

Avoid creating separate products such as:

```text
Daxa Cloud Codebase
Daxa Local Codebase
Daxa Restaurant Codebase
Daxa Retail Codebase
```

Instead, use:

```text
Single Daxa POS platform
├─ Deployment mode config
├─ Tenant/location config
├─ Industry template config
├─ Payment provider config
├─ Tax/country config
└─ Device/terminal config
```

### Configuration-based deployment

Example deployment mode config:

```text
DeploymentMode = Cloud
DeploymentMode = Local
DeploymentMode = Hybrid
```

Example industry config:

```text
IndustryTemplate = Cafe
IndustryTemplate = Bakery
IndustryTemplate = Pub
IndustryTemplate = Restaurant
IndustryTemplate = FoodTruck
IndustryTemplate = Retail
IndustryTemplate = Electronics
IndustryTemplate = RepairShop
```

Example country config:

```text
Country = AU
TaxMode = GST
TaxInclusivePricing = true
Currency = AUD
```

Example location config:

```text
Organisation = Example Group
Location = Main Street Bakery
Terminal = Front Counter 1
PaymentProvider = Zeller
ReceiptPrinter = Epson TM-T88
CustomerDisplay = Enabled
```

---

## Multi-tenant and multi-location model

Every tenant should support multi-location by default.

A single-location customer simply has one location.

Do not create separate logic for single-location tenants.

### Recommended hierarchy

```text
Tenant
└─ Organisation
   └─ Region
      └─ Country
         └─ Location / Venue
            └─ Terminal
```

### Example: single-location business

```text
Tenant: Main Street Bakery
Organisation: Main Street Bakery
Country: Australia
Location: Main Street Bakery
Terminal: Front Counter 1
```

### Example: multi-location chain

```text
Tenant: Example Hospitality Group
Organisation: Example Hospitality Group
Region: APAC
Country: Australia
Locations:
- Sydney CBD
- Bondi
- Parramatta
- Newcastle
Terminals:
- Sydney CBD / Bar 1
- Sydney CBD / Bar 2
- Bondi / Front Counter
- Parramatta / Restaurant POS
```

### Why everything should be multi-location

| Reason | Benefit |
| --- | --- |
| Single-location is just one location | No special-case code |
| Businesses can grow | No migration problem |
| Chains/franchises are supported | Better enterprise path |
| Reporting can aggregate properly | Location, country, region, organisation |
| Tax/payment settings can vary | Different locations may need different config |
| Device management is cleaner | Terminals belong to locations |

---

## Product naming summary

### Primary product

```text
Daxa POS
```

### Deployment options

| Name | Meaning |
| --- | --- |
| Daxa Cloud | Fully cloud-hosted POS |
| Daxa Local | On-prem/local-server deployment |
| Daxa Hybrid | Local server plus cloud sync/management |
| Daxa Sync | Local-to-cloud sync/backup layer |

### Apps/components

| Name | Meaning |
| --- | --- |
| Daxa Terminal | Staff-facing POS app |
| Daxa Display | Customer-facing second screen |
| Daxa Back Office | Admin/management portal |
| Daxa KDS | Kitchen display system |
| Daxa Payments | Payment-provider integration layer |
| Daxa Inventory | Inventory/stock module |

### Industry templates

| Name | Meaning |
| --- | --- |
| Daxa Hospitality | Hospitality configuration/template |
| Daxa Retail | Retail/service configuration/template |

---

## Short positioning copy

### Option 1

```text
Daxa POS is a configurable point-of-sale platform for hospitality, retail, food service, and service businesses. It can run cloud-hosted, locally on-premises, or in a hybrid model with local resilience and cloud management.
```

### Option 2

```text
Daxa POS gives venues and stores a flexible POS system that can run in the cloud, on a local server, or both. It supports counter sales, customer displays, payments, receipts, tax, inventory, reporting, and multi-location operations.
```

### Option 3

```text
Daxa POS is a cloud, local, and hybrid POS platform for modern retail and hospitality operations.
```

### Short tagline

```text
Daxa POS
Run it cloud, local, or hybrid.
```

Alternative:

```text
Daxa POS
One platform. Cloud, local, or hybrid.
```

Alternative:

```text
Daxa POS
Point of sale for retail, hospitality, and everything at the counter.
```
