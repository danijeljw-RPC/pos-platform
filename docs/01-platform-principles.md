# Platform Principles

## 1. Configurable commerce engine

The POS should not be built as a single-purpose restaurant POS.

Build a commerce engine with modules for:

- Products
- Menus
- Pricing
- Tax
- Orders
- Payments
- Refunds
- Receipts
- Inventory
- Customers
- Staff
- Terminals
- Reporting
- Audit logs

Industry-specific behaviour should be configured through templates and rules.

## 2. Product names stay product names

Tax treatment is metadata, not a product label.

Correct:

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

Incorrect:

```text
GST-free item                 $6.00
```

## 3. Per-item tax category

An order can contain mixed taxable and GST-free items. Tax must be calculated per order line.

Do not use:

```text
Order -> one GST flag
Order -> one tax rate
Venue -> all items use same GST rule
```

Use:

```text
Product -> TaxCategory -> TaxRate/Treatment
OrderLine -> captured tax snapshot
Order -> tax summary
```

## 4. Tax-line based system

Do not model tax as one field.

Avoid:

```csharp
Order.TaxRate
Order.TaxAmount
```

Use tax lines:

```text
Order
 └─ OrderLines
     └─ TaxLines
```

## 5. Provider-agnostic payments

Payment integrations should use a provider adapter model.

```text
PaymentProvider
├─ TyroAdapter
├─ ZellerAdapter
├─ SquareAdapter
├─ StripeTerminalAdapter
├─ WindcaveAdapter
├─ AdyenAdapter
├─ WorldlineAdapter
└─ GlobalPaymentsAdapter
```

The POS order flow should not care which provider is active.

## 6. Device-aware, not device-locked

- Windows POS terminals: MAUI
- Other devices: PWA
- Linux kiosk: PWA in browser kiosk mode
- Customer display on Windows: second MAUI window
- KDS: separate device/session

## 7. Audit everything serious

Audit events must capture:

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

## 8. Offline-aware from the beginning

Food trucks, cafes, pubs, and retail counters need resilience.

The platform should support:

- Local menu cache
- Local tax config cache
- Local price cache
- Local order queue
- Sync when online
- Idempotency keys
- Clear sync/offline status

Integrated EFTPOS offline behaviour is provider-dependent.
