# Architecture Overview — Daxa POS

Daxa POS is a single-codebase, configurable point-of-sale platform for hospitality, retail, food service, and service businesses.

See also: `docs/architecture/01-core-architecture.md` for the detailed service/module breakdown.

---

## Core Architecture

```text
Windows MAUI POS (Daxa Terminal)
├─ Staff POS Window
├─ Customer Display Window (Daxa Display)
└─ Local cache / sync layer
        ↓
Daxa API (ASP.NET Core)
├─ Identity / Tenancy
├─ Locations / Devices
├─ Product Catalogue
├─ Menu Service
├─ Tax Engine
├─ Pricing Engine
├─ Order Service
├─ Payment Service
├─ Refund Service
├─ Receipt Service
├─ Printer Service
├─ Inventory Service
├─ Reporting Service
└─ Audit Service
        ↓
PostgreSQL (EF Core)
        ↓
Payment Providers (via adapter)
├─ Tyro
├─ Zeller
├─ Square
├─ Stripe Terminal
├─ Windcave
├─ Adyen
└─ Worldline
```

---

## Solution Structure

```text
src/
  DaxaPos.Api/                   ASP.NET Core Web API host
  DaxaPos.Web/                   Standalone Blazor WebAssembly PWA (PLAN-0006): Terminal shell
                                 (sales, payments, customer display, KDS) and Back Office, all in
                                 one project under separate route trees/sessions — not split into
                                 per-surface projects as originally sketched below.
  DaxaPos.PosMaui/               Daxa Terminal (.NET MAUI, Windows) — future dedicated Windows
                                 terminal plan, not started; DaxaPos.Web's Terminal PWA is the
                                 interim implementation.
  DaxaPos.Workers/               Background workers (sync, reporting, jobs)

  DaxaPos.Domain/                Entities, value objects, domain events
  DaxaPos.Application/           Use cases, interfaces, DTOs
  DaxaPos.Infrastructure/        EF Core, external services, Keycloak client
  DaxaPos.Persistence/           DbContext, migrations

  DaxaPos.Modules.Catalog/       Products, categories, modifiers
  DaxaPos.Modules.Orders/        Orders, order lines
  DaxaPos.Modules.Payments/      Payments, ledger, refunds
  DaxaPos.Modules.Tax/           Tax engine, tax categories, tax rates
  DaxaPos.Modules.Pricing/       Pricing rules, surcharges, discounts
  DaxaPos.Modules.Receipts/      Receipt generation
  DaxaPos.Modules.Inventory/     Stock management
  DaxaPos.Modules.Reporting/     Reports and export
  DaxaPos.Modules.Audit/         Audit log
  DaxaPos.Modules.Devices/       Device registration, terminal config
  DaxaPos.Modules.Sync/          Local-to-cloud and cloud-to-local sync
  DaxaPos.Modules.Customers/     Customer profiles, loyalty (future)

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
  DaxaPos.Web.Tests/            bUnit component tests for DaxaPos.Web (PLAN-0006)
  DaxaPos.Tax.Tests/
  DaxaPos.Receipt.Tests/
  DaxaPos.Sync.Tests/
  DaxaPos.PaymentProvider.Tests/
```

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core Web API (.NET 9+) |
| Database | PostgreSQL (EF Core) |
| Identity | Keycloak (or equivalent) |
| Windows POS app | .NET MAUI (future; PWA is the interim Terminal implementation) |
| PWA (Terminal, Back Office, Display, KDS) | Standalone Blazor WebAssembly (decided PLAN-0006 Milestone A, 2026-07-06 — no React/Vue/Angular) |
| Realtime updates | SignalR / WebSockets |
| Background workers | ASP.NET Core hosted services |
| Local deployment | Docker + Docker Compose |
| Cloud deployment | TBD (see OI-0008) |

---

## Key Architecture Decisions

All decisions below are accepted ADRs unless noted otherwise. See [ADR Index](../adr/index.md).

| Decision | ADR |
|----------|-----|
| Single codebase | ADR-0001 |
| Cloud / Local / Hybrid modes | ADR-0002 |
| Multi-location by default | ADR-0003 |
| MAUI + PWA device strategy | ADR-0004 |
| Payment adapter architecture | ADR-0005 |
| Tax-line based tax engine | ADR-0006 |
| Sync principles | ADR-0007 |
| Device vs user identity | ADR-0008 |
| Financial records ledger | ADR-0010 |
| Receipt tax marker | ADR-0011 |
| Docker deployment | ADR-0012 |
| Cloud identity and local POS authentication | ADR-0013 (supersedes ADR-0009) |
| Inter-module communication pattern | ADR-0014 |
| Tenant isolation mechanism and POS session token format | ADR-0015 |
| Multi-language and localisation strategy | ADR-0016 (Accepted 2026-07-05 — planning-only, not yet implemented) |
