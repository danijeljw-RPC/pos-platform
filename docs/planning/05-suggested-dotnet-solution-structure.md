# Suggested .NET Solution Structure

This is a planning candidate, not final architecture. The canonical, up-to-date solution structure is [`docs/architecture/overview.md`](../architecture/overview.md) § Solution Structure — this file is preserved as legacy planning reference (see `docs/README.md` § Planning).

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
