# Worker Backlog — Daxa POS

## Purpose

This document defines the planned worker backlog for Daxa POS.

Daxa POS is a new project and should not inherit the old project phase status.

All previous implementation completion states from older repositories are obsolete.

This backlog assumes the new Daxa POS direction:

- Single codebase.
- Daxa Cloud, Daxa Local, and Daxa Hybrid deployment modes.
- Multi-tenant and multi-location by default.
- Windows MAUI for Daxa Terminal and Daxa Display.
- PWA for Daxa Back Office, Daxa KDS, non-Windows devices, Linux kiosks, and tablets.
- AU/NZ first.
- APAC, NA, and EMEA expansion later.
- Provider-agnostic payment architecture.
- Tax-line based global tax model.
- AU/NZ GST and GST-free support first.

---

# Phase Overview

| Phase | Worker | Status | Target Branch |
|---:|---|---|---|
| Phase 0 | Repository Foundation | Planned | `docs/repository-foundation` |
| Phase 1 | Architecture Foundation | Planned | `docs/architecture-foundation` |
| Phase 2 | Platform Skeleton | Planned | `feature/platform-skeleton` |
| Phase 3 | Identity, Tenancy, Locations, Devices | Planned | `feature/identity-tenancy-devices` |
| Phase 4 | Product Catalogue, Menus, Modifiers | Planned | `feature/catalog-menu` |
| Phase 5 | Tax and Pricing Engine | Planned | `feature/tax-pricing` |
| Phase 6 | Order Engine and POS Core | Planned | `feature/order-engine` |
| Phase 7 | Daxa Terminal MAUI Foundation | Planned | `feature/daxa-terminal-maui` |
| Phase 8 | Daxa Display Customer Screen | Planned | `feature/daxa-display` |
| Phase 9 | Payments Foundation | Planned | `feature/payments-foundation` |
| Phase 10 | Receipts, Printing, Cash Drawer | Planned | `feature/receipts-printing` |
| Phase 11 | Admin / Back Office PWA | Planned | `feature/back-office` |
| Phase 12 | Reporting and Audit Hardening | Planned | `feature/reporting-audit` |
| Phase 13 | Local/Hybrid Sync Foundation | Planned | `feature/daxa-sync` |
| Phase 14 | Inventory Foundation | Planned | `feature/inventory-foundation` |
| Phase 15 | KDS Foundation | Planned | `feature/kds-foundation` |
| Phase 16 | Hospitality Templates | Planned | `feature/hospitality-templates` |
| Phase 17 | Retail/Service Templates | Planned | `feature/retail-service-templates` |
| Phase 18 | Cloud/Local/Hybrid Deployment Hardening | Planned | `feature/deployment-hardening` |
| Phase 19 | Payment Provider Integrations | Planned | `feature/payment-providers` |
| Phase 20 | Global Expansion Readiness | Future | `feature/global-expansion` |

---

# Phase 0 — Repository Foundation

## Worker

Documentation / Repository Worker

## Goal

Create the repository structure and documentation baseline for Daxa POS.

## Key actions

1. Add `CLAUDE.md`.
2. Add `docs/README.md`.
3. Add ADR folder structure.
4. Add issue folder structure.
5. Add plan folder structure.
6. Add module documentation folders.
7. Add deployment documentation folders.
8. Add testing documentation folders.
9. Add integration documentation folders.
10. Add changelog.
11. Add plan template.
12. Add issue template.
13. Add ADR template.

## Required docs

```text
docs/README.md
docs/adr/index.md
docs/adr/accepted/
docs/adr/accepted/
docs/adr/superseded/
docs/issues/index.md
docs/issues/open/
docs/issues/closed/
docs/plans/templates/PLAN-template.md
docs/plans/active/
docs/plans/completed/
```

## Exit criteria

- Repository has planning/documentation scaffold.
- Claude Code can start future workers without rediscovering project direction.
- Initial ADR candidates exist.

---

# Phase 1 — Architecture Foundation

## Worker

Architecture Worker

## Goal

Capture the main Daxa POS architecture before implementation starts.

## Key actions

1. Create ADRs for single codebase.
2. Create ADR for cloud/local/hybrid deployment modes.
3. Create ADR for multi-location by default.
4. Create ADR for MAUI Windows + PWA device strategy.
5. Create ADR for payment adapter architecture.
6. Create ADR for tax-line based tax architecture.
7. Create ADR for local/hybrid sync principles.
8. Create ADR for device identity vs user identity.
9. Create module boundary documentation.
10. Create initial domain model notes.

## Required ADRs

```text
ADR-0001-single-codebase.md
ADR-0002-cloud-local-hybrid-deployment.md
ADR-0003-multi-location-by-default.md
ADR-0004-windows-maui-and-pwa-device-strategy.md
ADR-0005-payment-provider-adapter-architecture.md
ADR-0006-tax-line-based-tax-engine.md
ADR-0007-local-hybrid-sync-principles.md
ADR-0008-device-identity-vs-user-identity.md
```

## Exit criteria

- Architecture direction is documented.
- Accepted/proposed ADRs are available for workers.
- No implementation starts without these decisions captured.

---

# Phase 2 — Platform Skeleton

## Worker

Infrastructure / API / Database Worker

## Goal

Create the initial technical skeleton.

## Key actions

1. Create .NET solution.
2. Create API project.
3. Create domain/application/infrastructure/persistence projects.
4. Create PostgreSQL database setup.
5. Create Docker Compose development stack.
6. Add basic health checks.
7. Add migration foundation.
8. Add basic test projects.
9. Add CI test command.
10. Add initial module folders.

## Suggested projects

```text
src/
  DaxaPos.Api/
  DaxaPos.Domain/
  DaxaPos.Application/
  DaxaPos.Infrastructure/
  DaxaPos.Persistence/
  DaxaPos.Workers/

tests/
  DaxaPos.UnitTests/
  DaxaPos.IntegrationTests/
  DaxaPos.Api.Tests/
```

## Exit criteria

- `dotnet build` passes.
- `dotnet test` passes.
- Docker stack starts.
- Health endpoint works.
- Database connection works.
- Migrations can apply.

---

# Phase 3 — Identity, Tenancy, Locations, Devices

## Worker

Identity / Security / Device Worker

## Goal

Implement the foundation for tenant isolation, multi-location, user identity, staff roles, and device registration.

## Key actions

1. Add tenant model.
2. Add organisation model.
3. Add region/country/location model.
4. Add terminal/device model.
5. Add role/permission model.
6. Add staff PIN login model.
7. Add admin user login model.
8. Add device registration token model.
9. Add terminal assignment.
10. Add location-level access control.
11. Add audit log foundation.
12. Add tests for tenant/location isolation.

## Exit criteria

- Tenant and location boundaries are enforced.
- Device and user identity are separate.
- A single-location tenant is represented as one location.
- Multi-location tenants are supported from the beginning.

---

# Phase 4 — Product Catalogue, Menus, Modifiers

## Worker

Catalogue / Menu Worker

## Goal

Implement products, categories, modifiers, menus, and POS tile configuration.

## Key actions

1. Add product category model.
2. Add product model.
3. Add product variant foundation.
4. Add modifier group model.
5. Add modifier model.
6. Add menu model.
7. Add menu availability model.
8. Add location-specific availability.
9. Add sold-out flag.
10. Add product tax category link.
11. Add print/prep routing metadata.
12. Add admin APIs.
13. Add tests.

## Exit criteria

- Products are data-driven.
- Menus are data-driven.
- Product tax category is assigned per item.
- Menu can render POS buttons dynamically.

---

# Phase 5 — Tax and Pricing Engine

## Worker

Tax / Pricing Worker

## Goal

Implement AU/NZ tax and the global-ready tax architecture.

## Key actions

1. Add tax category model.
2. Add tax rate model.
3. Add AU GST 10%.
4. Add AU GST-free category.
5. Add NZ GST 15%.
6. Add NZ zero-rated/exempt concepts.
7. Add tax-inclusive calculation.
8. Add line-level tax snapshots.
9. Add order tax summary.
10. Add GST-free receipt marker support.
11. Add pricing rule model.
12. Add surcharge rule model foundation.
13. Add discount rule foundation.
14. Add tests for mixed baskets.

## Must-pass test

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

## Exit criteria

- AU/NZ mixed baskets work.
- GST-free markers are supported.
- Product names remain product names.
- Tax snapshots are stored on order lines.
- Tax engine can later support multiple tax lines.

---

# Phase 6 — Order Engine and POS Core

## Worker

Order Worker

## Goal

Implement the core order lifecycle.

## Key actions

1. Create order.
2. Add item.
3. Add modifiers.
4. Remove item.
5. Void item.
6. Hold order.
7. Resume order.
8. Add order notes.
9. Add item notes.
10. Apply discount.
11. Apply surcharge.
12. Calculate totals.
13. Save order snapshot.
14. Audit meaningful changes.
15. Add tests.

## Exit criteria

- Orders can be created and paid later.
- Order totals are correct.
- Tax/pricing/surcharge logic is integrated.
- Financial records are not silently edited.

---

# Phase 7 — Daxa Terminal MAUI Foundation

## Worker

MAUI POS Worker

## Goal

Create the Windows staff-facing Daxa Terminal app.

## Key actions

1. Create MAUI project.
2. Add login/terminal registration screen.
3. Add venue/location/terminal context.
4. Add POS order screen.
5. Add product/category tile layout.
6. Add order panel.
7. Add basic payment action flow.
8. Add local/cloud/hybrid API target configuration.
9. Add full-screen/borderless mode planning.
10. Add barcode scanner keyboard-wedge support planning.
11. Add tests/manual test notes.

## Exit criteria

- Daxa Terminal can connect to API.
- Terminal can create an order.
- Terminal can display product/menu data.
- Terminal can show calculated totals.

---

# Phase 8 — Daxa Display Customer Screen

## Worker

Customer Display Worker

## Goal

Create customer-facing second display support.

## Key actions

1. Add customer display state model.
2. Add second MAUI window.
3. Add order summary display.
4. Add idle display.
5. Add payment started display.
6. Add payment approved/declined display.
7. Add receipt prompt placeholder.
8. Add display assignment configuration.
9. Add tests/manual test notes.

## Exit criteria

- Staff screen and customer display use same order/payment state.
- No stretched-window design.
- Customer display is separate from KDS.

---

# Phase 9 — Payments Foundation

## Worker

Payments Worker

## Goal

Implement payment model and provider adapter foundation.

## Key actions

1. Add payment method model.
2. Add cash payment.
3. Add manual external EFTPOS.
4. Add split payment model.
5. Add payment request model.
6. Add provider adapter interface.
7. Add fake/test payment provider.
8. Add payment terminal assignment.
9. Add payment idempotency.
10. Add refund model.
11. Add payment/refund audit.
12. Add tests.

## Exit criteria

- Orders can be paid by cash/manual EFTPOS.
- Payment adapter architecture exists.
- Payment records are auditable.
- Split payment model works.

---

# Phase 10 — Receipts, Printing, Cash Drawer

## Worker

Printing / Receipt Worker

## Goal

Implement receipts and printer queue foundation.

## Key actions

1. Add receipt model.
2. Add receipt rendering.
3. Add tax invoice layout.
4. Add GST-free marker display.
5. Add refund receipt.
6. Add print queue.
7. Add printer configuration.
8. Add ESC/POS abstraction.
9. Add cash drawer kick command support.
10. Add reprint audit.
11. Add tests.

## Exit criteria

- Receipt shows actual product names.
- Receipt shows GST summary.
- Receipt supports `F = GST-free`.
- Reprints are audited.
- Print queue exists.

---

# Phase 11 — Admin / Back Office PWA

## Worker

Back Office Worker

## Goal

Create the management/admin application.

## Key actions

1. Add admin shell.
2. Add tenant/location selector.
3. Add product management.
4. Add menu management.
5. Add tax configuration.
6. Add pricing/surcharge configuration.
7. Add payment provider setup screen.
8. Add device/terminal setup.
9. Add user/role management.
10. Add reporting placeholders.
11. Add audit log view.
12. Add tests.

## Exit criteria

- Admin users can configure core POS data.
- Configuration is location-aware.
- Dangerous settings require permissions and audit.

---

# Phase 12 — Reporting and Audit Hardening

## Worker

Reporting / Audit Worker

## Goal

Implement basic operational reporting and harden audit trails.

## Key actions

1. Daily sales report.
2. Sales by payment method.
3. Sales by category.
4. Sales by product.
5. Tax report.
6. Refunds report.
7. Voids report.
8. Discounts report.
9. Surcharges report.
10. Cash events report.
11. Audit event filtering.
12. Export foundation.

## Exit criteria

- Venue manager can see daily trading report.
- Tax summary is reportable.
- Financial actions are traceable.

---

# Phase 13 — Local/Hybrid Sync Foundation

## Worker

Sync / Offline Worker

## Goal

Create the foundation for Daxa Local and Daxa Hybrid.

## Key actions

1. Define sync event model.
2. Add local-to-cloud queue.
3. Add cloud-to-local queue.
4. Add idempotency.
5. Add sync status.
6. Add retry logic.
7. Add conflict detection.
8. Add sync audit.
9. Add backup/export hooks.
10. Add tests.

## Exit criteria

- Local server can queue data for cloud.
- Cloud can push configuration to local.
- Duplicate sync is prevented.
- Sync failures are visible.

---

# Phase 14 — Inventory Foundation

## Worker

Inventory Worker

## Goal

Implement simple inventory and stock movement foundation.

## Key actions

1. Add inventory item.
2. Add stock movement.
3. Add product-to-stock link.
4. Add stock adjustment.
5. Add waste/spoilage.
6. Add stock decrement on sale.
7. Add sold-out behaviour.
8. Add daily production count support.
9. Add low stock alert foundation.
10. Add tests.

## Exit criteria

- Sales can decrement stock.
- Adjustments are auditable.
- Bakery/food truck stock countdown is possible.

---

# Phase 15 — KDS Foundation

## Worker

KDS Worker

## Goal

Implement kitchen/bar/prep display foundation.

## Key actions

1. Add station model.
2. Add station login.
3. Add station routing rules.
4. Add KDS ticket model.
5. Add KDS PWA shell.
6. Add realtime updates.
7. Add full-state reload.
8. Add bump/complete workflow.
9. Add void/cancel propagation.
10. Add tests.

## Exit criteria

- KDS can display routed tickets.
- KDS can rebuild state after reconnect.
- KDS is separate from Daxa Display.

---

# Phase 16 — Hospitality Templates

## Worker

Hospitality Worker

## Goal

Add configured templates for hospitality venues.

## Key actions

1. Cafe template.
2. Bakery template.
3. Pub/bar template.
4. Restaurant template.
5. Fast food template.
6. Food truck template.
7. Surcharge defaults.
8. Modifier defaults.
9. Routing defaults.
10. Receipt defaults.
11. Tests.

## Exit criteria

- New venues can start from a hospitality template.
- Templates are configuration, not separate code.

---

# Phase 17 — Retail/Service Templates

## Worker

Retail / Service Worker

## Goal

Add configured templates for retail and service businesses.

## Key actions

1. Retail template.
2. Clothing template.
3. Electronics template.
4. Repair shop template.
5. SKU/barcode defaults.
6. Return/exchange defaults.
7. Warranty placeholders.
8. Service job placeholders.
9. Tests.

## Exit criteria

- New venues can start from retail/service templates.
- Templates are configuration, not separate code.

---

# Phase 18 — Cloud/Local/Hybrid Deployment Hardening

## Worker

Infrastructure / Deployment Worker

## Goal

Harden deployment models.

## Key actions

1. Cloud deployment docs.
2. Local deployment docs.
3. Hybrid deployment docs.
4. Docker Compose stack.
5. Environment variable reference.
6. Backup/restore docs.
7. Upgrade/rollback docs.
8. Health checks.
9. Logging.
10. Monitoring.
11. TLS/cert docs.
12. Smoke tests.

## Exit criteria

- Local dev deployment works.
- Local server deployment is documented.
- Cloud deployment is documented.
- Hybrid deployment is documented.
- Backup/restore is documented.

---

# Phase 19 — Payment Provider Integrations

## Worker

Payment Provider Worker

## Goal

Add real payment providers.

## Priority

```text
1. Tyro
2. Zeller
3. Square Terminal
4. Stripe Terminal
5. Windcave
6. Adyen later
7. Worldline later
8. Global Payments later
```

## Key actions

1. Create provider-specific docs.
2. Confirm certification/onboarding requirements.
3. Implement sandbox/test integration.
4. Add credential configuration.
5. Add terminal pairing.
6. Add payment request.
7. Add payment status polling/webhooks.
8. Add refunds.
9. Add test harness.
10. Add audit events.
11. Add manual test checklist.

## Exit criteria

- At least one integrated provider can be tested end-to-end.
- Provider credentials are protected.
- Terminal payment amount is sent from Daxa POS.

---

# Phase 20 — Global Expansion Readiness

## Worker

Globalisation Worker

## Goal

Prepare for APAC, NA, and EMEA.

## Key actions

1. Tax-exclusive pricing support.
2. US/CA stacked tax model.
3. Tips/gratuity model.
4. Multi-currency.
5. Locale formats.
6. Country-specific receipt fields.
7. Provider availability by country.
8. Regional deployment docs.
9. Language/localisation foundation.
10. Tests.

## Exit criteria

- Model can support SG/HK/UK/US/CA.
- US/CA tax and tipping architecture is documented.
- No AU/NZ hard-coding blocks expansion.

---

# Worker Type Reference

| Worker Type | Relevant Phases |
|---|---|
| Documentation worker | All phases |
| Architecture worker | Phase 0–1 |
| Infrastructure/Docker worker | Phase 2, 18 |
| Database worker | All phases |
| API worker | Phase 2–20 |
| MAUI POS worker | Phase 7 |
| Customer Display worker | Phase 8 |
| PWA Admin worker | Phase 11 |
| PWA KDS worker | Phase 15 |
| Tax worker | Phase 5, 20 |
| Pricing/Surcharge worker | Phase 5–6 |
| Payments worker | Phase 9, 19 |
| Printing worker | Phase 10 |
| Inventory worker | Phase 14 |
| Sync/offline worker | Phase 13 |
| Hospitality worker | Phase 16 |
| Retail/service worker | Phase 17 |
| Testing worker | All phases |
| Security worker | All phases |

---

# Current Next Worker

For a new Daxa POS repository, the next worker should be:

```text
Phase 0 — Repository Foundation
```

If the repository scaffold already exists, proceed to:

```text
Phase 1 — Architecture Foundation
```
