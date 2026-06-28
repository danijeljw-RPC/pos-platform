# Suggested .NET Solution Structure

This is a planning candidate, not final architecture.

```text
src/
  PosPlatform.Api/
  PosPlatform.AdminPwa/
  PosPlatform.PosMaui/
  PosPlatform.KdsPwa/
  PosPlatform.Workers/

  PosPlatform.Domain/
  PosPlatform.Application/
  PosPlatform.Infrastructure/
  PosPlatform.Persistence/

  PosPlatform.Modules.Catalog/
  PosPlatform.Modules.Orders/
  PosPlatform.Modules.Payments/
  PosPlatform.Modules.Tax/
  PosPlatform.Modules.Pricing/
  PosPlatform.Modules.Receipts/
  PosPlatform.Modules.Inventory/
  PosPlatform.Modules.Reporting/
  PosPlatform.Modules.Audit/
  PosPlatform.Modules.Devices/
  PosPlatform.Modules.Customers/

  PosPlatform.PaymentProviders.Tyro/
  PosPlatform.PaymentProviders.Zeller/
  PosPlatform.PaymentProviders.Square/
  PosPlatform.PaymentProviders.StripeTerminal/
  PosPlatform.PaymentProviders.Windcave/
  PosPlatform.PaymentProviders.Adyen/

tests/
  PosPlatform.UnitTests/
  PosPlatform.IntegrationTests/
  PosPlatform.Api.Tests/
  PosPlatform.PaymentProvider.Tests/
  PosPlatform.Tax.Tests/
```

## Shared libraries

```text
Domain
- Entities
- Value objects
- Domain services
- Domain events

Application
- Use cases
- Commands/queries
- Validation
- Interfaces

Infrastructure
- External integrations
- Payment adapters
- Printing
- Device integrations

Persistence
- EF Core
- Migrations
- Repository implementations
```

## Suggested bounded contexts

```text
Identity/Tenancy
Catalogue/Menu
Pricing
Tax
Orders
Payments/Refunds
Receipts/Printing
Inventory
Customers/Loyalty
Gift Cards/Vouchers
Reporting
Audit
Devices/Terminals
Sync/Offline
```
