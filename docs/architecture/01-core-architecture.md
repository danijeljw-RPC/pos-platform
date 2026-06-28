# Core Architecture

## Core services/modules

```text
Identity / tenancy
Venue/location management
Product catalogue
Menu service
Pricing engine
Tax engine
Order service
Payment service
Refund service
Receipt service
Printer service
Inventory service
Customer/loyalty service
Gift card service
Reporting service
Audit log service
Device/terminal service
Sync/offline service
```

## Apps

```text
Windows MAUI POS app
Windows MAUI customer display
PWA POS fallback
PWA KDS
PWA admin portal
PWA self-order kiosk later
API backend
Worker services
Reporting/export jobs
```

## High-level architecture

```text
Windows MAUI POS
├─ Staff POS Window
├─ Customer Display Window
└─ Local cache/sync layer
        ↓
POS API / Backend
├─ Orders
├─ Payments
├─ Products
├─ Tax
├─ Pricing
├─ Receipts
├─ Inventory
├─ Reporting
└─ Audit
        ↓
Payment Providers
├─ Tyro
├─ Zeller
├─ Square
├─ Stripe Terminal
├─ Windcave
├─ Adyen
├─ Worldline
└─ Global Payments
```

## Multi-window POS model

```text
MAUI app process
├─ Staff POS window
└─ Customer display window
```

Both windows use the same order/session state.

```text
CurrentOrderService
├─ Staff window reads/writes order
└─ Customer window displays current order
```

## Do not stretch one app across two monitors

Avoid:

```text
One giant app window spread across both screens
```

Use:

```text
Window 1: Staff POS UI
Window 2: Customer display UI
```
