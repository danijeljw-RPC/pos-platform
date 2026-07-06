# MVP Scope

The MVP should prove the new POS platform as a configurable retail/hospitality/service POS.

## MVP should include

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

## MVP payment modes

```text
Cash
Manual external EFTPOS
Integrated payment adapter foundation
```

## MVP payment providers to consider

For AU/NZ direction:

```text
Manual EFTPOS first
Stripe Terminal or Square for clean API learning
Tyro/Zeller for AU commercial fit
Windcave for AU/NZ bridge
```

## MVP business types to support

The first version should support these with configuration, not separate code branches:

| Business type | MVP support |
| --- | --- |
| Cafe | Yes |
| Bakery/cake shop | Yes |
| Pub/bar | Basic |
| Restaurant | Basic counter/table start |
| Food truck | Basic, offline-aware |
| Retail | Basic SKU/barcode support |
| Repair/service shop | Basic service item/order notes |

## MVP exclusions / later items

| Feature | Phase |
| --- | --- |
| Full floor plan | Phase 2 |
| KDS | Phase 2 |
| Advanced inventory/BOM | Phase 3 |
| Multi-location franchise controls | Phase 3 |
| US/CA complex tax/tipping | Phase 4 |
| Global payment providers | Phase 4 |
