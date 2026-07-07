# Daxa POS

**Run it cloud, local, or hybrid.**

Daxa POS is a configurable, enterprise-ready point-of-sale platform for hospitality, retail, food service, and service-based businesses. It is built as a single codebase with cloud, local, and hybrid deployment modes, multi-location tenancy by default, and a provider-agnostic payment integration layer — not a narrow restaurant POS bolted onto every other vertical.

[![CI](https://github.com/danijeljw-RPC/pos-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/danijeljw-RPC/pos-platform/actions/workflows/ci.yml)

---

## Status

This repository is under active development and is **not production-ready**. Core identity/tenancy, product catalogue, tax/pricing, and payments/receipts/printing foundations are built and tested; terminal, display, and PWA device experiences are the current work in progress.

For the live, authoritative state of the project — accepted architecture decisions, open issues, and active plans — see [`docs/README.md`](docs/README.md). It links every ADR, module doc, and plan and is kept current as work lands; nothing below should be treated as a substitute for it.

## What Daxa POS is

Daxa POS must support cafes, bakeries, cake shops, food trucks, pubs and bars, restaurants, fast food, clothing and electronics retail, computer repair, general service businesses, and multi-location/franchise organisations — starting with AU/NZ and expanding into APAC, North America, and EMEA.

It is organised as one platform with several products layered on top of a shared domain model:

| Product | Role |
| --- |---|
| Daxa Cloud | Fully cloud-hosted deployment: master API, database, reporting, tenant config |
| Daxa Local | On-premises deployment: authoritative local server for a single site |
| Daxa Hybrid | Cloud for central management/reporting, local server for operational continuity |
| Daxa Terminal | Staff-facing POS app (Windows: .NET MAUI) |
| Daxa Display | Customer-facing second screen at the counter (not the KDS) |
| Daxa Back Office | Admin/management portal (web/PWA) |
| Daxa Payments | Provider-agnostic payment adapter layer (Tyro, Zeller, Square, Stripe Terminal, Windcave, Adyen, …) |
| Daxa Inventory | Stock, availability, movements, waste |
| Daxa KDS | Kitchen/bar/prep display (PWA, separate from Daxa Display) |
| Daxa Sync | Local-to-cloud and cloud-to-local synchronisation |
| Daxa Hospitality / Daxa Retail | Configuration/template sets, not separate codebases |

Cloud, Local, and Hybrid are **deployment modes**, not separate products, and every tenant supports multiple locations by default — a single-location customer is simply a tenant with one location.

## Architecture at a glance

```text
Daxa Cloud                              Daxa Local Server
├─ Tenant/organisation data              ├─ Local API + database
├─ Central product catalogue      ⇅      ├─ Local order/payment/print routing
├─ Central reporting              sync   ├─ Local device registry
├─ Payment provider configuration        └─ Offline-resilient trading
└─ Remote admin portal
                                              ⇅ local network
                                    Venue devices
                                    ├─ Daxa Terminal (MAUI, Windows)
                                    ├─ Daxa Display (MAUI second window)
                                    ├─ Daxa KDS / Back Office (PWA)
                                    ├─ Printers (ESC/POS)
                                    └─ Payment terminals
```

Internet loss must not stop trading for a properly configured local/hybrid venue — the local server is authoritative during service, and realtime pushes (SignalR) are convenience notifications on top of state that can always be rebuilt from the server after reconnect.

See [`docs/architecture/overview.md`](docs/architecture/overview.md) and [`docs/architecture/deployment-modes.md`](docs/architecture/deployment-modes.md) for the full picture.

## Tech stack

- **Backend:** ASP.NET Core Web API on .NET 10, PostgreSQL via EF Core, background workers for sync/reporting/print queues.
- **Admin / device PWA:** Blazor WebAssembly PWA (`DaxaPos.Web`).
- **Windows POS terminal & customer display:** .NET MAUI (planned — see PLAN-0006).
- **KDS, non-Windows POS fallback, kiosks:** PWA.
- **Infrastructure:** Docker Compose for local/dev; cloud deployment is planned but not yet built out.
- **Identity:** tenant-scoped session tokens today; Keycloak reserved for future cloud/admin identity (ADR-0013) — not required for local/offline operation.

## Repository layout

```text
src/
  DaxaPos.Api             ASP.NET Core Web API (host)
  DaxaPos.Application     Use cases / application services
  DaxaPos.Domain          Domain entities and rules
  DaxaPos.Infrastructure  Cross-cutting infrastructure concerns
  DaxaPos.Persistence     EF Core DbContext, migrations, repositories
  DaxaPos.Workers         Background worker host
  DaxaPos.Web             Blazor WASM PWA (admin / device setup / login)

tests/
  DaxaPos.UnitTests       Domain and application unit tests
  DaxaPos.Api.Tests       API integration tests (real PostgreSQL)
  DaxaPos.Web.Tests       Web project tests

docs/                     Product vision, ADRs, plans, module docs, deployment docs — see docs/README.md
deploy/                   Older infra-only (db + keycloak) Compose stack
scripts/                  Local demo/setup helper scripts
compose.yaml              Full local dev stack (db, keycloak, migrations, api, worker, web)
```

## Getting started

Prerequisites: Docker and Docker Compose, or the .NET 10 SDK (with `net10.0` runtime roll-forward) if running services outside containers.

### Fastest path — full stack in Docker

```bash
cp .env.example .env
docker compose up -d --build
./scripts/setup-local-demo.sh
```

`setup-local-demo.sh` authenticates as the bootstrap admin, creates or reuses a demo location and staff member, and prints a device registration PIN, staff code, and staff PIN so you can sign into the PWA immediately. It's safe to rerun.

For PLAN-0006 `/sales` manual UI testing, run the sales-ready helper instead:

```bash
./scripts/setup-local-sales-demo.sh
```

It builds on the base helper and creates the local-dev-only terminal, Staff-role grant, tax,
product, required modifier, and menu records needed for a product tile to appear on `/sales`.
It prints the exact device setup, Back Office terminal-assignment, staff login, payment, and
display steps.

Once running:

- API: `http://localhost:5118` (`GET /health`)
- Web/PWA: `http://localhost:8080`
- PostgreSQL: `localhost:5432`

### Manual / outside Docker

For running the API, applying EF Core migrations by hand, and exercising individual endpoints directly, follow [`docs/testing/local-smoke-test.md`](docs/testing/local-smoke-test.md) — it is the authoritative, current walkthrough (including the one deliberate quirk: Keycloak is not required and is left stopped).

## Testing

```bash
dotnet build DaxaPos.sln
dotnet test DaxaPos.sln
```

`DaxaPos.Api.Tests` runs against a real PostgreSQL database (see `.github/workflows/ci.yml` for the exact service/migration sequence CI uses) — mocking the database is deliberately avoided for this suite.

## Documentation

Start at [`docs/README.md`](docs/README.md) — it indexes:

- **[ADRs](docs/adr/index.md)** — every accepted architecture decision (single codebase, cloud/local/hybrid, multi-location-by-default, tax-line-based tax engine, payment adapter architecture, etc.)
- **[Active plans](docs/plans/active/)** — the current build phases and their worker notes
- **[Open issues](docs/issues/index.md)** — unresolved questions and known gaps
- **[Module docs](docs/modules/)** — catalogue, orders, payments, refunds, tax, pricing, receipts, printing, inventory, reporting, audit, sync, KDS
- **[Deployment docs](docs/deployment/)** — cloud, local, hybrid, Docker, Windows terminal, Linux kiosk PWA

## Contributing

This project is built under human supervision with an AI coding agent following explicit build rules in [`CLAUDE.md`](CLAUDE.md) / [`AGENTS.md`](AGENTS.md): every architectural decision needs an ADR, every unresolved question needs an open issue, and every meaningful change updates documentation and tests alongside code. See [`docs/github/github-workflow.md`](docs/github/github-workflow.md) for the branch/commit/PR workflow.

All Markdown in this repository must pass `npx markdownlint-cli2 "**/*.md"` (config in `.markdownlint-cli2.jsonc`).

## License

No open-source license has been published for this repository yet. All rights reserved unless a `LICENSE` file says otherwise.
