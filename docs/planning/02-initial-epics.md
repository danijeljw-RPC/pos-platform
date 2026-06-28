# Initial Epics

## Epic 1 — Tenant, organisation, venue, and terminal setup

Capabilities:

```text
Create tenant
Create organisation
Create region/country/venue
Register terminal
Assign terminal to venue
Configure device type
Assign printer/payment terminal
```

## Epic 2 — Product catalogue and menu

Capabilities:

```text
Categories
Products
Modifiers
Product images
Tax category assignment
Availability rules
Sold-out toggle
Industry templates
```

## Epic 3 — Order engine

Capabilities:

```text
Create order
Add/remove items
Apply modifiers
Hold/resume order
Void line/order
Split/merge later
Order notes
Order state lifecycle
```

## Epic 4 — Tax engine

Capabilities:

```text
AU GST 10%
NZ GST 15%
GST-free / zero-rated / exempt categories
Tax-inclusive pricing
Mixed baskets
Tax snapshots
Receipt tax summary
Global tax-line architecture
```

## Epic 5 — Pricing, surcharges, and discounts

Capabilities:

```text
Base pricing
Modifier pricing
Discounts
Surcharges
Sunday/public holiday/card/service charges
Taxable surcharge rules
Promotion rules later
```

## Epic 6 — Payments

Capabilities:

```text
Cash
Manual external EFTPOS
Payment provider abstraction
Provider credential setup
Terminal pairing
Start payment
Cancel payment
Refund payment
Payment status handling
```

## Epic 7 — Receipts and printing

Capabilities:

```text
Thermal receipts
Tax invoices
Refund receipts
Receipt templates
Printer assignment
Reprint with audit
Tax markers like F = GST-free
```

## Epic 8 — Customer display

Capabilities:

```text
Second MAUI window
Idle branding
Order summary
Payment prompt
Payment result
Receipt QR later
Loyalty prompt later
```

## Epic 9 — Users, roles, permissions

Capabilities:

```text
Staff PIN login
Admin login
Roles
Permissions
Manager approval
Audit linked to user
```

## Epic 10 — Audit logging

Capabilities:

```text
Void
Refund
Discount
Price override
Cash drawer
Tax changes
Payment provider changes
Receipt reprints
Stock adjustments
```

## Epic 11 — Reporting

Capabilities:

```text
Daily sales
Payment method report
Tax report
Refund report
Void report
Discount report
Surcharge report
Cash reconciliation
Product/category sales
```

## Epic 12 — Offline and sync

Capabilities:

```text
Local menu cache
Local tax config cache
Local price cache
Order queue
Idempotency keys
Sync status
Conflict handling
```
