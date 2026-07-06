# AGENTS.md — Daxa POS Build Instructions

## Purpose

This repository is for building **Daxa POS**, a configurable, enterprise-ready point-of-sale platform for hospitality, retail, food service, and service-based businesses.

Daxa POS must support:

- Cafes
- Bakeries
- Cake shops
- Food trucks
- Pubs and bars
- Restaurants
- Fast food
- Clothing stores
- Electronics stores
- General retail
- Computer repair stores
- Service businesses
- Multi-location chains
- Franchise-style organisations
- AU/NZ launch
- APAC expansion
- North America expansion
- EMEA expansion

Codex may assist with planning, implementation, documentation, testing, review, and maintenance, but the system must be built under human supervision.

Codex must not make uncontrolled architectural decisions, skip documentation, silently change agreed direction, or treat temporary implementation convenience as product direction.

The project must be built as a **single configurable platform** with multiple deployment modes:

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

The system must not become separate disconnected products for cloud, local, hospitality, retail, or service workflows.

---

## Product Identity

### Product name

```text
Daxa POS
```

### Domain

```text
daxapos.com
```

### Product positioning

```text
Daxa POS
Run it cloud, local, or hybrid.
```

Alternative positioning:

```text
Daxa POS
One platform. Cloud, local, or hybrid.
```

### Product principle

Daxa POS is not a narrow restaurant POS.

Daxa POS is a configurable commerce platform for counter-based operations, hospitality, retail, food service, and services.

---

## Product Line

### Daxa POS

Daxa POS is the umbrella product and core platform.

It includes:

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
- Cloud, local, and hybrid deployment modes

### Daxa Cloud

Daxa Cloud is the fully cloud-hosted deployment option.

The master application, API, database, reporting, tenant configuration, backups, and admin portal run in Daxa-managed cloud infrastructure.

### Daxa Local

Daxa Local is the local/on-premises deployment option.

The customer runs a local Daxa server onsite inside their own network. The local server can operate as the authoritative runtime system for that site during trading.

### Daxa Hybrid

Daxa Hybrid combines Daxa Cloud and Daxa Local.

The cloud provides central management, reporting, backups, updates, and multi-location visibility. The local server provides operational continuity, local device control, local printing, local payment routing, and resilience if internet access fails.

### Daxa Terminal

Daxa Terminal is the staff-facing POS application.

For Windows POS devices, Daxa Terminal should be a .NET MAUI application.

### Daxa Display

Daxa Display is the customer-facing second screen attached to the POS terminal.

It is used at the counter to show order items, totals, discounts, surcharges, payment prompts, payment result, receipt options, loyalty prompts, and idle branding.

Daxa Display is not the KDS.

### Daxa Back Office

Daxa Back Office is the admin and management portal.

It should normally be a web application/PWA.

### Daxa Payments

Daxa Payments is the provider-agnostic payment integration layer.

It is not necessarily a payment processor itself.

It allows venues to connect their own payment-provider account and terminal.

### Daxa Inventory

Daxa Inventory manages stock, product availability, stock movements, waste, purchase orders later, serial numbers later, and inventory reporting.

### Daxa KDS

Daxa KDS is the kitchen/bar/prep display system.

It should be separate from the customer-facing second display.

Daxa KDS should usually run as a PWA on separate devices.

### Daxa Sync

Daxa Sync is the local-to-cloud synchronisation layer.

It supports local-to-cloud and cloud-to-local data movement for Daxa Local and Daxa Hybrid deployments.

### Daxa Hospitality

Daxa Hospitality is a configuration/template set for hospitality workflows.

It is not a separate codebase.

### Daxa Retail

Daxa Retail is a configuration/template set for retail and service workflows.

It is not a separate codebase.

---

## Current Product Direction

Daxa POS is a full-stack, configurable POS platform with:

- Single codebase.
- Cloud, local, and hybrid deployment options.
- Multi-tenant architecture.
- Multi-location model by default.
- Windows MAUI POS app for Windows counter terminals.
- Windows MAUI customer-facing second display.
- PWA for non-Windows POS fallback, KDS, admin, tablets, Linux kiosks, and future self-order.
- ASP.NET Core API backend.
- PostgreSQL database.
- Optional local server for local/on-prem/hybrid venues.
- Optional cloud master system for cloud-only and hybrid customers.
- Optional sync layer for local-to-cloud and cloud-to-local data movement.
- Provider-agnostic payment integrations.
- AU/NZ tax model first.
- Global tax model planned from the beginning.
- Products, menus, prices, taxes, surcharges, devices, users, and venues configured through data.
- Enterprise auditability.
- Stock/inventory support.
- Receipt and printer support.
- Customer display support.
- Future KDS support.
- Future loyalty/gift card/customer account support.
- Future international rollout across APAC, NA, and EMEA.

---

## Important Product Decisions Already Made

The following decisions are established and must not be re-litigated without creating a new ADR.

### Platform and deployment

1. Daxa POS must use a single codebase.
2. Daxa Cloud, Daxa Local, and Daxa Hybrid are deployment modes, not separate codebases.
3. Deployment mode must be configuration/infrastructure driven.
4. The system must support cloud-only deployment.
5. The system must support local/on-prem deployment.
6. The system must support hybrid local + cloud deployment.
7. Local deployment is primarily for venues that want to run from a local server inside their onsite network.
8. Local deployment may optionally back up or sync data to Daxa Cloud, a data lake, or another configured remote destination.
9. Cloud-only deployment runs the master system in cloud infrastructure.
10. Hybrid deployment links a local server to the cloud so data can be pushed and pulled between local and cloud components.
11. Offline/local resilience must be designed early, even when cloud deployment is supported.
12. Internet loss must not automatically stop trading for properly configured local/hybrid venues.
13. The system must be built so cloud, local, and hybrid deployments use the same domain model and API concepts.

### Device strategy

1. Windows POS terminals should use .NET MAUI.
2. Windows customer-facing second display should use .NET MAUI, normally as a second window from the POS terminal app.
3. Linux devices should use PWA, not MAUI.
4. Android tablets should use PWA.
5. iPad devices should use PWA.
6. KDS screens should normally use PWA.
7. Admin/back-office should normally be web/PWA.
8. Self-ordering kiosks should normally use PWA with OS kiosk lockdown.
9. A Windows POS terminal can run in borderless full-screen mode.
10. True kiosk/lockdown behaviour is an operating system deployment concern, not only an app display concern.
11. Daxa Display is not a KDS.
12. Daxa Display is the customer-facing display at the point of sale.
13. KDS screens are separate devices/sessions.
14. Device identity and user identity must be separate.
15. Device registration must determine terminal role, mode, printer mapping, payment terminal mapping, and display configuration.

### Multi-tenancy and multi-location

1. The platform must be multi-tenant.
2. Every tenant should support multi-location by default.
3. A single-location customer is simply a tenant/organisation with one location.
4. Do not create special single-location business logic that conflicts with multi-location architecture.
5. Tenant isolation must be designed early.
6. Location isolation must be designed early.
7. Organisation, country, region, venue, and terminal hierarchy must be explicit.
8. Products, prices, taxes, payments, devices, and reports may need organisation-level defaults and location-level overrides.

### Industry scope

1. Daxa POS must not be hard-coded as a restaurant POS.
2. Daxa POS must support hospitality, retail, food, and service workflows through configuration/modules.
3. Industry templates are preferred over separate products.
4. Daxa Hospitality and Daxa Retail are configuration/module groupings, not separate codebases.
5. Food truck mode must consider offline/local resilience and event/location tagging.
6. Bakery/cake shop mode must support production counts, GST-free markers, pre-orders, deposits, and pickup workflows over time.
7. Retail mode must support SKU/barcode flows, variants, returns, and inventory.
8. Electronics/service mode must eventually support serial numbers, warranties, repair jobs, deposits, and service lifecycle.

### Tax

1. Tax must be calculated per order line.
2. Tax must support mixed baskets, including taxable and GST-free items in the same order.
3. Tax treatment is metadata, not a product name.
4. Receipts must show actual product names.
5. GST-free items may be marked with a code such as `F`.
6. Example receipt display:

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

1. Do not display generic item names like `GST-free item`.
2. AU/NZ tax support must be implemented first.
3. AU should support GST 10% and GST-free categories.
4. NZ should support GST 15%, zero-rated, and exempt supply concepts.
5. The global tax model must support multiple tax components per order line.
6. The system must not use a single `Order.TaxRate` model.
7. The system should support tax-inclusive and tax-exclusive pricing.
8. The system should support 10 tax components per item line and 20 tax components per order as safe design limits.
9. Tax snapshots must be stored on order lines at sale time.

### Payments

1. Daxa Payments must use a provider-agnostic adapter architecture.
2. The POS must be able to initiate payment amounts on integrated terminals.
3. Staff should not manually type payment amounts into terminals when integrated payment is configured.
4. Customers/venues should be able to connect their own payment-provider account.
5. Payment provider configuration is per tenant/organisation/location/terminal as appropriate.
6. Manual external EFTPOS must be supported.
7. Cash payments must be supported.
8. Split payments must support multiple payment records against one order.
9. Refunds must be linked to original payments where possible.
10. Payment, refund, gift card, and store credit activity must be ledgered.
11. Payment terminal mappings must support multiple payment terminals per venue.
12. AU/NZ first payment-provider targets include Tyro, Zeller, Square, Stripe Terminal, and Windcave.
13. Later provider targets include Adyen, Worldline, and Global Payments.
14. Square can be integrated behind Daxa POS using Square Terminal API so the amount is sent from Daxa POS to Square Terminal.
15. Square POS app must not become the main POS.
16. Stripe Terminal should be considered as a global SaaS-style baseline provider where supported.
17. Tyro and Zeller are strong AU-specific providers.
18. Windcave is useful for AU/NZ and regional payment depth.
19. Adyen is likely more suitable for enterprise/global expansion.

### Pricing, discounts, surcharges

1. Pricing must be data-driven.
2. Surcharges must be configurable.
3. Card surcharge, Sunday surcharge, public holiday surcharge, service charge, delivery fee, packaging fee, bottle deposit, and environmental levy should be representable.
4. Surcharges need taxability rules.
5. Discounts must support line-level and order-level application.
6. Discounts and overrides should be permissioned and audited.
7. Tips/gratuity must be supported later for US/CA markets.
8. AU/NZ surcharge workflows must be supported.

### Orders and receipts

1. Financially meaningful records must not be silently edited.
2. Use reversals, voids, refunds, or adjustment records.
3. Order state must be reconstructable from the server/database.
4. Receipts must support thermal printing.
5. Receipts must support tax summaries.
6. Receipt reprints must be audited.
7. Refund receipts must link to original orders/refunds.
8. Customer display state must reflect current order and payment state.

### Realtime and sync

1. Realtime updates may use SignalR/WebSockets or equivalent.
2. Missed realtime messages must not break correctness.
3. Server state must remain authoritative.
4. Pushed events are convenience notifications, not the source of truth.
5. KDS screens must be able to rebuild current state from the server after reconnect.
6. Local sync must use idempotency keys.
7. Local-to-cloud and cloud-to-local sync must be resilient and auditable.
8. Sync conflicts must be explicit and recoverable.

### Documentation

1. Every meaningful change must update documentation.
2. Every architectural decision must be captured in an ADR.
3. Every unresolved question must be captured as an open issue.
4. Every worker must leave notes so future workers can continue without rediscovery.

---

## Target Deployment Models

### Daxa Cloud

Daxa Cloud is the fully cloud-hosted model.

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

Cloud behaviour:

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

### Daxa Local

Daxa Local is the local/on-prem model.

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

Local behaviour:

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

### Daxa Hybrid

Daxa Hybrid combines Daxa Cloud and Daxa Local.

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

Hybrid behaviour:

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

---

## Multi-Tenant and Multi-Location Model

Every tenant must support multiple locations by default.

A single location is simply one location record.

### Recommended hierarchy

```text
Tenant
└─ Organisation
   └─ Region
      └─ Country
         └─ Location / Venue
            └─ Terminal
```

### Single-location example

```text
Tenant: Main Street Bakery
Organisation: Main Street Bakery
Country: Australia
Location: Main Street Bakery
Terminal: Front Counter 1
```

### Multi-location example

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

### Multi-location requirements

The platform must support:

- Organisation-level configuration.
- Region-level grouping.
- Country-level configuration.
- Location-level overrides.
- Terminal-level configuration.
- Location-specific products.
- Location-specific menu availability.
- Location-specific prices.
- Location-specific taxes.
- Location-specific payment providers.
- Location-specific printer routing.
- Central reporting.
- Store-level reporting.
- Franchise-style access restrictions.
- Cross-location gift cards later.
- Cross-location customer profiles later.

---

## Technology Direction

Initial preferred direction:

### Backend

- ASP.NET Core Web API.
- .NET 9 or newer unless explicitly changed.
- No `Startup.cs`; use modern `Program.cs` style.
- Modular architecture.
- PostgreSQL preferred.
- EF Core preferred unless an ADR chooses otherwise.
- Background workers for sync, reporting, jobs, print queues, and scheduled tasks.
- SignalR/WebSockets for live updates where needed.
- Full-state reload APIs must exist for correctness after reconnect.

### Frontend

- Windows POS terminal: .NET MAUI.
- Windows customer display: .NET MAUI second window.
- Admin/back office: PWA/web.
- KDS: PWA.
- Non-Windows POS fallback: PWA.
- Linux kiosk: PWA in Chromium kiosk mode.
- Android/iPad: PWA.
- Self-order kiosk later: PWA.

### Database

- PostgreSQL.
- Database schema must support tenant, organisation, location, terminal, order, payment, tax, inventory, audit, and sync requirements.
- Migrations must be tested.
- Financial records must be append-only or reversal-based where appropriate.

### Infrastructure

- Docker and Docker Compose for local/dev deployment.
- Cloud deployment must be planned but does not need to be fully implemented in early MVP.
- Local server deployment must be supported.
- Hybrid sync must be designed early.
- Secrets must not be committed.
- Payment provider credentials must be stored securely.

### Identity and access

- Keycloak or similar identity management may be used.
- Identity architecture must support cloud, local, and hybrid deployments.
- Local PIN workflows are required for POS staff speed.
- Device identity and user identity must be separate.
- Role-based and permission-based authorization is required.
- Tenant and location boundaries must be enforced.
- Support/admin access must be auditable.

### Printing and hardware

- ESC/POS support should be planned.
- Network printers should be supported.
- USB printers may be supported for Windows terminals.
- Cash drawer kick via printer command should be supported.
- Barcode scanners should be supported as keyboard-wedge input first.
- Payment terminals must be mapped to POS terminals.

---

## Core Domain Primitives

Build around these primitives:

```text
Tenant
Organisation
Region
Country
Venue / Location
Terminal
Device
User
Role
Permission
Product
ProductCategory
ProductVariant
ModifierGroup
Modifier
Menu
MenuAvailabilityRule
TaxCategory
TaxRate
PriceRule
SurchargeRule
DiscountRule
Order
OrderLine
OrderLineModifier
OrderLineTax
OrderSurcharge
OrderDiscount
Payment
Refund
Receipt
Printer
PaymentTerminal
Customer
GiftCard
Voucher
StoreCredit
InventoryItem
StockMovement
AuditEvent
SyncEvent
```

---

## Core Modules

### Identity / tenancy

Responsible for:

- Tenant isolation.
- Organisation hierarchy.
- User accounts.
- Roles.
- Permissions.
- Staff PIN login.
- Admin login.
- Support access.
- Device registration identity.
- Audit-linked identities.

### Venue/location management

Responsible for:

- Countries.
- Regions.
- Locations/venues.
- Venue settings.
- Time zone.
- Currency.
- Tax profile.
- Payment profile.
- Device profile.
- Receipt profile.
- Printer profile.
- Menu availability.

### Product catalogue

Responsible for:

- Categories.
- Products.
- Product images.
- SKU/barcode.
- Variants.
- Modifiers.
- Tax category assignment.
- Print routing.
- Availability.
- Sold-out state.
- Cost price later.
- Product templates.

### Menu service

Responsible for:

- Menus.
- Menu sections.
- Location-specific menus.
- Time/day availability.
- Hospitality menus.
- Retail catalog views.
- Food truck event menus.
- Sold-out visibility.
- POS tile configuration.

### Pricing engine

Responsible for:

- Base price.
- Modifier prices.
- Size prices.
- Location-specific prices.
- Customer group prices.
- Time-based prices.
- Day-based prices.
- Happy hour.
- Public holiday pricing.
- Promotion pricing.
- Price overrides.
- Tax-inclusive/exclusive mode.

### Tax engine

Responsible for:

- AU GST.
- NZ GST.
- GST-free markers.
- Zero-rated/exempt categories.
- Tax-inclusive pricing.
- Tax-exclusive pricing.
- Multiple tax components.
- Tax snapshots.
- Tax summaries.
- Surcharge taxability.
- Discount tax effects.
- Rounding rules.
- Country/regional tax configuration.

### Order service

Responsible for:

- Creating orders.
- Adding/removing items.
- Modifiers.
- Order notes.
- Item notes.
- Voids.
- Holds.
- Resume order.
- Split bill later.
- Merge orders later.
- Table/tab assignment.
- Order lifecycle.
- State rebuild.

### Payment service

Responsible for:

- Cash.
- Manual external EFTPOS.
- Integrated EFTPOS.
- Gift card payments later.
- Store credit later.
- Account sale later.
- Deposits.
- Split payments.
- Provider requests.
- Provider responses.
- Payment status.
- Payment ledger.
- Idempotency.

### Refund service

Responsible for:

- Full refunds.
- Partial refunds.
- Linked provider refunds.
- Manual refunds.
- Refund reason.
- Refund permissions.
- Refund receipt.
- Refund audit.
- Reversal records.

### Receipt service

Responsible for:

- Thermal receipts.
- Tax invoices.
- Refund receipts.
- Gift receipts later.
- Digital receipts later.
- Receipt templates.
- Tax markers.
- Reprint audit.
- Provider payment details.

### Printer service

Responsible for:

- Receipt printer routing.
- Kitchen/bar docket routing.
- Label printer routing later.
- ESC/POS commands.
- Cash drawer kick.
- Printer health.
- Print queue.
- Reprint flow.
- Print failure retry.

### Inventory service

Responsible for:

- Finished item stock.
- Sold-out state.
- Stock decrement.
- Stock adjustments.
- Waste/spoilage.
- Low-stock alerts.
- Daily stock reset.
- Ingredient inventory later.
- BOM/recipe later.
- Serial tracking later.
- Purchase orders later.

### Customer / loyalty service

Responsible for:

- Customer profile.
- Purchase history.
- Loyalty later.
- Store credit.
- Gift cards.
- Vouchers.
- Digital receipts.
- Tax exemption later.
- Cross-location profile later.

### Reporting service

Responsible for:

- Daily sales.
- Sales by payment method.
- Sales by category.
- Sales by product.
- Sales by staff.
- Sales by terminal.
- Sales by location.
- Refunds.
- Voids.
- Discounts.
- Surcharges.
- Tax reports.
- Cash reconciliation.
- Stock movement.
- Gift card liability later.
- Tips later.
- Executive dashboards later.

### Audit log service

Responsible for:

- Security audit.
- Financial audit.
- Configuration audit.
- Operational audit.
- Support access audit.
- Before/after values.
- Reason capture.
- Linked entity IDs.

### Device / terminal service

Responsible for:

- Device registration.
- Terminal assignment.
- Device type.
- POS terminal mode.
- Customer display assignment.
- Printer assignment.
- Payment terminal assignment.
- Health status.
- App version.
- Remote config.

### Sync / offline service

Responsible for:

- Local cache.
- Local order queue.
- Local-to-cloud sync.
- Cloud-to-local sync.
- Idempotency keys.
- Conflict handling.
- Retry.
- Sync status.
- Backup/export.
- Audit sync.

---

## Tax Requirements

### AU/NZ first

The first tax implementation should support:

- AU GST 10%.
- AU GST-free items.
- NZ GST 15%.
- NZ zero-rated items.
- NZ exempt supply concepts.
- Tax-inclusive pricing.
- Mixed baskets.
- Tax markers on receipts.
- Tax snapshots per order line.

### Mixed AU basket example

Products:

```text
Flat white                    $5.50    AU_GST_10
Chocolate cake slice          $8.80    AU_GST_10
Loaf of bread                 $6.00    AU_GST_FREE
```

Receipt:

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

Calculation:

```text
Flat white GST:           $5.50 / 11 = $0.50
Chocolate cake GST:       $8.80 / 11 = $0.80
Loaf of bread GST:        $0.00
Total GST:                $1.30
```

### Recommended tax data shape

```text
TaxRate
- Id
- CountryCode
- RegionCode
- Name
- RatePercent
- TaxType
- IsCompound
- AppliesToTaxInclusivePrices
- Priority
- IsActive

TaxCategory
- Id
- Name
- Code
- Description
- TaxTreatment

ProductTaxCategory
- ProductId
- TaxCategoryId

VenueTaxConfiguration
- VenueId
- CountryCode
- RegionCode
- TaxInclusivePricing
- TaxCalculationMode

OrderLineTax
- OrderLineId
- TaxRateId
- TaxName
- RatePercent
- TaxableAmount
- TaxAmount
- JurisdictionName
- JurisdictionType
```

### Global tax design limits

```text
Maximum tax components per item line: 10
Maximum tax components per order: 20
```

---

## Payment Provider Roadmap

### AU/NZ launch

Initial provider priority:

```text
1. Manual external EFTPOS
2. Cash
3. Tyro
4. Zeller
5. Square
6. Stripe Terminal
7. Windcave
```

### APAC expansion

Singapore:

```text
1. Stripe Terminal
2. Adyen
3. Windcave
4. Worldline
5. Global Payments
```

Hong Kong:

```text
1. Adyen
2. Windcave
3. Worldline
4. Global Payments
```

### UK

```text
1. Stripe Terminal
2. Square
3. Adyen
4. Worldline
5. Global Payments
```

### US/Canada

```text
1. Stripe Terminal
2. Square
3. Adyen
4. Global Payments
```

### Payment adapter architecture

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

### Payment provider definition

Each provider must define:

```text
Provider name
Supported countries
Supported currencies
Supported devices
Supported payment types
Terminal pairing method
Credential fields
Webhook model
Refund capability
Partial refund capability
Tip capability
Surcharge capability
Offline capability
Settlement/reconciliation support
Certification requirements
```

### Integrated payment flow

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

## Device and App Strategy

### Windows POS terminal

Use Daxa Terminal as a .NET MAUI app.

Expected capabilities:

- Staff-facing POS screen.
- Full-screen/borderless mode.
- Optional Windows kiosk/assigned access deployment.
- Payment terminal pairing.
- Printer assignment.
- Cash drawer support through printer where available.
- Barcode scanner input.
- Customer display second window.
- Local/cloud/hybrid connection modes.

### Customer second screen

Use Daxa Display as a second .NET MAUI window where supported.

Do not stretch one giant app window across two monitors.

Use:

```text
Window 1: Staff POS UI
Window 2: Customer display UI
```

### Linux devices

Use PWA in browser kiosk mode.

Do not rely on .NET MAUI Linux for commercial production.

### Android/iPad

Use PWA.

### KDS

Use PWA.

KDS must be able to rebuild state from the server after reconnect.

### Admin

Use web/PWA.

---

## Industry Templates

### Cafe

Should preconfigure:

- Coffee categories.
- Milk modifiers.
- Size modifiers.
- Takeaway/dine-in.
- GST.
- Customer display.
- Receipt printing.
- Quick payment flow.

### Bakery

Should preconfigure:

- Daily stock.
- Cake orders.
- GST-free marker support.
- Production quantities.
- Pickup orders.
- Deposits later.
- Labels later.

### Pub/bar

Should preconfigure:

- Bar tabs.
- Split bills.
- Table areas.
- Happy hour.
- Public holiday surcharge.
- Sunday surcharge.
- Tips later.
- Bar routing.

### Restaurant

Should preconfigure:

- Tables/floor plan.
- Dine-in/takeaway.
- Split bills.
- Course firing later.
- KDS routing.
- Kitchen notes.
- Service charge.

### Fast food

Should preconfigure:

- Quick order.
- Combos.
- Takeaway.
- Drive-through later.
- KDS routing.
- Customer display.

### Food truck

Should preconfigure:

- Offline-aware mode.
- Event/location tagging.
- Limited menu.
- Stock countdown.
- Quick payment.
- End-of-day event summary.

### Retail

Should preconfigure:

- SKU/barcode.
- Variants.
- Returns.
- Exchanges.
- Gift receipts.
- Inventory.
- Stocktake later.

### Electronics

Should preconfigure:

- Serial numbers later.
- IMEI tracking later.
- Warranty tracking later.
- Returns/exchanges.
- Repairs later.

### Repair shop

Should preconfigure:

- Service jobs.
- Customer device record.
- Fault description.
- Intake checklist.
- Quote approval.
- Parts/labour lines.
- Deposits.
- Job status lifecycle.

---

## Codex Operating Rules

Codex must follow these rules every time it runs.

### Autonomous planning behaviour

When Codex is asked to plan, triage, investigate, or prepare fixes, it must work autonomously from the repository contents.

Codex must not ask the human for file paths, issue details, commands, or project structure that can be discovered by reading the repository.

Codex must proceed with reasonable assumptions when the repository provides enough context.

Assumptions must be written into the active plan or worker notes.

Codex may only stop for human input when:

- Credentials or secrets are required.
- A destructive operation is required.
- A production-impacting operation is required.
- A product or architecture decision is genuinely ambiguous.
- Required source files are missing and no further useful inspection can be completed.

Planning work must still update documentation.

For every planning or triage session, Codex must create or update:

- `docs/plans/active/<plan-name>.md`
- `docs/plans/active/<worker-notes-name>.md`
- relevant files under `docs/issues/open/`
- `docs/issues/index.md`

`docs/issues/index.md` must link to every open issue and group issues by area.

Codex must leave enough notes that a later worker can continue without re-discovering the same information.

---

## Required Context Review

Before changing code, Codex must read:

- `AGENTS.md`
- `docs/README.md`
- `docs/adr/index.md`
- Relevant accepted ADRs
- Relevant open issues
- Relevant project plan files
- Relevant module documentation
- Relevant deployment documentation
- Relevant test documentation

If any of these files do not exist, Codex must create a documentation issue or include the missing file in the plan.

---

## Planning Before Work

Before making changes, Codex must create or update a short implementation plan.

Use:

- `docs/plans/templates/PLAN-template.md`
- `docs/plans/active/`

The plan must include:

- Goal
- Scope
- Non-goals
- Files likely to change
- Architecture assumptions
- Domain assumptions
- Risks
- Tests to run
- Documentation to update
- Commit sequence
- Rollback notes if relevant

---

## No More Than Three Changes Without a Plan Refresh

Codex must not make more than three meaningful changes without pausing and updating the active plan.

A meaningful change includes:

- Adding a new module.
- Changing database schema.
- Changing API contract.
- Adding or changing authentication behaviour.
- Adding or changing authorization behaviour.
- Adding or changing tenant isolation behaviour.
- Adding or changing location isolation behaviour.
- Adding or changing deployment behaviour.
- Adding or changing sync behaviour.
- Adding or changing payment behaviour.
- Adding or changing tax behaviour.
- Adding or changing pricing/surcharge behaviour.
- Adding or changing tests.
- Changing documentation structure.
- Adding major UI workflow behaviour.

---

## Commit Rules

Codex must commit each completed logical change.

Commit messages should be clear and scoped.

Examples:

```text
docs: add Daxa deployment ADRs
feat(catalog): add product tax category model
feat(tax): calculate AU GST-inclusive mixed baskets
feat(payments): add payment provider abstraction
test(tax): add GST-free receipt marker tests
infra: add local docker compose stack
docs: add Daxa Hybrid sync plan
```

Do not batch unrelated changes into one commit.

---

## Documentation Rules

Documentation is part of the work.

Every implementation change must update relevant documentation.

Documentation may include:

- ADRs
- API docs
- Configuration docs
- Module docs
- Test notes
- Deployment docs
- Open issues
- Project plan status
- Changelog
- Data model notes
- Security notes
- Sync notes
- Payment integration notes
- Tax rules

### Docs folder requirements

Codex must keep `./docs` current.

If a question is unresolved, create an open issue:

```text
docs/issues/open/OI-xxxx-title.md
```

If a decision is proposed, create an ADR:

```text
docs/adr/proposed/ADR-xxxx-title.md
```

Move ADRs to accepted only after human approval:

```text
docs/adr/accepted/
```

If a decision is superseded, move/update it under:

```text
docs/adr/superseded/
```

### Markdown linting

All Markdown files must pass `npx markdownlint-cli2 "**/*.md"` with zero errors. Repo rules are in `.markdownlint-cli2.jsonc` at the repository root (line length and table pipe alignment are intentionally disabled there; duplicate-heading checks apply per-parent, not document-wide).

When creating or editing any `.md` file:

- Use exactly one top-level `#` heading (the document title). Every other heading is `##` or deeper.
- For documents with a repeated structure (phases, worker notes, changelog entries), nest each repeated block's subheadings one level below that block's own heading, e.g. `## Phase 3` → `### Goal` / `### Key actions` / `### Exit criteria`. Do not reuse the same heading text as a sibling under the same parent.
- Give every fenced code block a language hint. Use `` ```text `` for plain output, ASCII diagrams, or file paths — never a bare `` ``` ``.
- Run `npx markdownlint-cli2 "**/*.md"` before considering documentation work complete, and `npx markdownlint-cli2 --fix "**/*.md"` first to clear anything auto-fixable.

---

## Testing Rules

Codex must add or update tests for behaviour it changes.

Expected test categories:

- Unit tests.
- Integration tests.
- API tests.
- Database migration tests.
- Authorization tests.
- Tenant isolation tests.
- Location isolation tests.
- Tax calculation tests.
- GST/GST-free mixed basket tests.
- Receipt rendering tests.
- Payment adapter tests.
- Refund tests.
- Pricing/surcharge tests.
- KDS routing tests.
- Printer queue tests.
- Stock movement tests.
- Gift card ledger tests later.
- Sync/backup tests where applicable.
- Offline/reconnect tests where applicable.

Tests must include normal cases and failure cases.

Financial, tax, payment, refund, and audit logic must not be changed without tests.

---

## Specialist Worker Workflow

The preferred workflow is to use specialised Codex workers sequentially.

Do not run multiple broad workers trying to alter the same area at the same time.

Worker examples:

- Architecture worker.
- Database worker.
- API worker.
- MAUI POS worker.
- Customer display worker.
- PWA admin worker.
- PWA KDS worker.
- Tax worker.
- Pricing/surcharge worker.
- Payments worker.
- Refunds worker.
- Printing worker.
- Inventory worker.
- Customer/loyalty worker.
- Gift card worker.
- Sync/offline worker.
- Infrastructure/Docker worker.
- Testing worker.
- Documentation worker.
- Security worker.

Each worker must:

1. Read current documentation.
2. Create/update a plan.
3. Make focused changes.
4. Run relevant tests.
5. Update documentation.
6. Commit changes.
7. Leave notes for the next worker.

---

## GitHub Integration

Codex should integrate with GitHub for repository tracking where available.

Expected behaviour:

- Create GitHub issues from `docs/issues/open/` where appropriate.
- Reference issue numbers in commits.
- Keep issue status aligned with documentation.
- Use branches for meaningful feature work.
- Open pull requests when work is ready for review.
- Never close architectural issues without an ADR or explicit human decision.
- Do not close issues just because code was changed; verify tests/docs first.

---

## Human Supervision

Codex must clearly identify:

- What it changed.
- What it did not change.
- What requires human review.
- What assumptions were made.
- What tests passed.
- What tests failed.
- What remains unresolved.
- What documentation was updated.
- What ADRs or open issues were created or modified.

Codex must not claim production readiness unless the relevant tests, deployment checks, security checks, and documentation are complete.

---

## Strict Implementation Principles

- Single codebase.
- Cloud, local, and hybrid are deployment modes.
- Multi-location by default.
- Single-location customers use one location.
- Tenant isolation must be enforced.
- Location isolation must be enforced.
- Device identity and user identity must be separate.
- Financial records must be auditable.
- Financially meaningful records must not be silently edited.
- Use reversals, voids, refunds, or adjustment records.
- Product names must remain product names.
- Tax treatment must be metadata.
- Menu configuration must be data-driven.
- Tax configuration must be data-driven.
- Pricing configuration must be data-driven.
- Surcharge configuration must be data-driven.
- Station and printer routing must be data-driven.
- Payment provider integrations must use adapters.
- Provider credentials must be protected.
- Staff should not manually enter integrated payment amounts into payment terminals.
- Order state must be recoverable from authoritative server/database state.
- Realtime messages are convenience notifications only.
- Missed realtime messages must not break correctness.
- KDS must rebuild state after reconnect.
- Offline/local resilience must be designed early.
- Sync must use idempotency.
- Every meaningful change must update documentation.
- Every architectural decision must have an ADR.
- Every unresolved decision must have an open issue.

---

## Initial MVP Direction

MVP should include:

```text
Products/categories
Modifiers
Tax categories
AU/NZ GST + GST-free
Basic order creation
Cash payment
External EFTPOS manual payment
Integrated payment adapter foundation
Receipt printing
Order history
Refunds
Users/roles/PIN login
Basic reporting
Audit log
Venue settings
Terminal registration
Windows MAUI POS
Customer-facing second screen
Admin portal
```

MVP should avoid overbuilding:

```text
Full floor plan
Advanced KDS
Advanced inventory/BOM
Full multi-location franchise control
US/CA complex tax/tipping
Global payment integrations
Advanced loyalty
Advanced gift cards
Repair job lifecycle
Serial number tracking
```

These can be phased in after the core architecture is stable.

---

## Phase Roadmap

### Phase 1 — MVP

```text
Products/categories
Modifiers
Tax categories
AU/NZ GST + GST-free
Basic order creation
Cash payment
External EFTPOS manual payment
Integrated payment adapter foundation
Receipt printing
Order history
Refunds
Users/roles/PIN login
Basic reporting
Audit log
Venue settings
Terminal registration
Windows MAUI POS
Customer-facing second screen
Admin portal
```

### Phase 2 — Hospitality and operations depth

```text
Tables/floor plan
Split bills
Bar tabs
Kitchen routing
KDS PWA
Stock countdown
Customer profiles
Gift cards
Discount engine
Surcharge engine
Public holiday/Sunday surcharge
Cash drawer management
End-of-day reconciliation
```

### Phase 3 — Multi-location and advanced business workflows

```text
Multi-location chains
Advanced inventory
Purchase orders
Supplier management
Loyalty/stamps/points
Digital receipts
SMS/email notifications
Cake pre-orders
Service/repair jobs
Serial number tracking
Advanced reporting
Accounting exports
```

### Phase 4 — Global expansion

```text
US/CA tax engine
Tips/gratuity
Tax-exclusive pricing
Multiple stacked tax lines
Stripe Terminal global rollout
Square where available
Adyen enterprise
Worldline/global payments
Region-specific receipt rules
Multi-currency
Multi-language
```

---

## Suggested Repository Documentation Structure

Codex should maintain a docs structure similar to:

```text
docs/
  README.md

  adr/
    index.md
    proposed/
    accepted/
    superseded/

  plans/
    templates/
      PLAN-template.md
    active/
    completed/

  issues/
    index.md
    open/
    closed/

  architecture/
    overview.md
    deployment-modes.md
    tenancy.md
    multi-location.md
    sync.md
    security.md
    payment-adapters.md
    tax-engine.md

  modules/
    catalog.md
    menus.md
    orders.md
    payments.md
    refunds.md
    tax.md
    pricing.md
    surcharges.md
    receipts.md
    printing.md
    inventory.md
    customers.md
    gift-cards.md
    devices.md
    reporting.md
    audit.md
    sync.md

  deployment/
    cloud.md
    local.md
    hybrid.md
    docker.md
    windows-terminal.md
    linux-kiosk-pwa.md

  testing/
    strategy.md
    tax-tests.md
    payment-tests.md
    sync-tests.md
    receipt-tests.md

  integrations/
    payments/
      tyro.md
      zeller.md
      square-terminal.md
      stripe-terminal.md
      windcave.md
      adyen.md
    printers/
      escpos.md
```

---

## Suggested .NET Solution Structure

This is a planning candidate, not final architecture.

```text
src/
  DaxaPos.Api/
  DaxaPos.AdminPwa/
  DaxaPos.PosMaui/
  DaxaPos.KdsPwa/
  DaxaPos.Workers/

  DaxaPos.Domain/
  DaxaPos.Application/
  DaxaPos.Infrastructure/
  DaxaPos.Persistence/

  DaxaPos.Modules.Catalog/
  DaxaPos.Modules.Orders/
  DaxaPos.Modules.Payments/
  DaxaPos.Modules.Tax/
  DaxaPos.Modules.Pricing/
  DaxaPos.Modules.Receipts/
  DaxaPos.Modules.Inventory/
  DaxaPos.Modules.Reporting/
  DaxaPos.Modules.Audit/
  DaxaPos.Modules.Devices/
  DaxaPos.Modules.Sync/
  DaxaPos.Modules.Customers/

  DaxaPos.PaymentProviders.Tyro/
  DaxaPos.PaymentProviders.Zeller/
  DaxaPos.PaymentProviders.Square/
  DaxaPos.PaymentProviders.StripeTerminal/
  DaxaPos.PaymentProviders.Windcave/
  DaxaPos.PaymentProviders.Adyen/

tests/
  DaxaPos.UnitTests/
  DaxaPos.IntegrationTests/
  DaxaPos.Api.Tests/
  DaxaPos.PaymentProvider.Tests/
  DaxaPos.Tax.Tests/
  DaxaPos.Receipt.Tests/
  DaxaPos.Sync.Tests/
```

---

## Final Instruction To Codex

When in doubt:

1. Read the docs.
2. Preserve the single-codebase direction.
3. Preserve cloud/local/hybrid deployment support.
4. Preserve multi-location-by-default design.
5. Preserve tax-line based tax architecture.
6. Preserve provider-agnostic payment architecture.
7. Preserve MAUI-for-Windows and PWA-for-other-devices strategy.
8. Write an ADR for decisions.
9. Write an open issue for unresolved questions.
10. Update tests and documentation with every meaningful change.
