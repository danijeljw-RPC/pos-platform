# Domain Primitives

Build around these primitives:

```text
Tenant
Organisation
Region
Country
Venue
Terminal
User
Product
TaxCategory
PriceRule
SurchargeRule
Order
OrderLine
Payment
Refund
Receipt
AuditEvent
```

## Organisation hierarchy

```text
Tenant
└─ Organisation
   └─ Region
      └─ Country
         └─ Venue
            └─ Terminal
```

## Core commerce entities

```text
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
```

## Order entities

```text
Order
OrderLine
OrderLineModifier
OrderLineTax
OrderSurcharge
OrderDiscount
Payment
Refund
Receipt
AuditEvent
```

## Terminal/device entities

```text
Device
Terminal
CustomerDisplay
Printer
PaymentTerminal
DeviceAssignment
TerminalHealth
```

## Customer/loyalty entities

```text
Customer
CustomerAccount
LoyaltyAccount
GiftCard
Voucher
StoreCredit
```
