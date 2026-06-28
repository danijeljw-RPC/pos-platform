# Project Roadmap — Daxa POS

## Purpose

This roadmap defines the planned build phases for **Daxa POS**.

Daxa POS is a new configurable POS platform. It should not inherit completion status from the previous POS project.

The platform direction is:

- One codebase.
- Multi-tenant.
- Multi-location by default.
- Daxa Cloud, Daxa Local, and Daxa Hybrid deployment modes.
- Windows MAUI for Daxa Terminal and Daxa Display.
- PWA for Back Office, KDS, tablets, Linux kiosks, and non-Windows devices.
- AU/NZ first.
- APAC, NA, and EMEA expansion later.
- Provider-agnostic payment architecture.
- Tax-line based global tax model.

---

# Phase 0 — Repository Foundation

## Goals

- Create documentation structure.
- Add Claude Code operating rules.
- Add ADR templates.
- Add issue templates.
- Add plan templates.
- Establish Daxa POS product naming.
- Establish cloud/local/hybrid direction.
- Establish single-codebase direction.
- Establish initial module boundaries.

## Outputs

```text
CLAUDE.md
docs/README.md
docs/adr/index.md
docs/issues/index.md
docs/plans/templates/PLAN-template.md
docs/modules/
docs/architecture/
docs/deployment/
docs/testing/
docs/integrations/
```

---

# Phase 1 — Architecture Foundation

## Goals

- Capture core architecture ADRs.
- Confirm deployment modes.
- Confirm device strategy.
- Confirm multi-location model.
- Confirm payment adapter model.
- Confirm tax engine model.
- Confirm sync/offline principles.
- Confirm identity/security direction.

## Key decisions to document

- Single codebase.
- Cloud/local/hybrid deployment.
- Multi-location by default.
- Windows MAUI + PWA device strategy.
- Payment provider adapter architecture.
- Tax-line based tax engine.
- Local/hybrid sync principles.
- Device identity vs user identity.
- Keycloak or equivalent identity direction.

---

# Phase 2 — Platform Skeleton

## Goals

- Create .NET solution.
- Add API project.
- Add domain/application/infrastructure/persistence projects.
- Add PostgreSQL.
- Add Docker Compose.
- Add health checks.
- Add migration foundation.
- Add test projects.
- Add initial CI/test commands.

## Suggested starting projects

```text
DaxaPos.Api
DaxaPos.Domain
DaxaPos.Application
DaxaPos.Infrastructure
DaxaPos.Persistence
DaxaPos.Workers
```

---

# Phase 3 — Identity, Tenancy, Locations, Devices

## Goals

- Tenant model.
- Organisation model.
- Region/country/location model.
- Terminal/device registration.
- User model.
- Role/permission model.
- Staff PIN login model.
- Admin login model.
- Audit foundation.
- Tenant/location isolation tests.

## Key principle

Every tenant supports multi-location by default.

A single-location business is simply one location.

---

# Phase 4 — Product Catalogue, Menus, Modifiers

## Goals

- Product categories.
- Products.
- Product variants foundation.
- Modifiers.
- Modifier groups.
- Menus.
- Menu availability.
- Location-specific menu overrides.
- Sold-out flag.
- Product tax category assignment.
- Print/prep routing metadata.
- Admin APIs.

## Key principle

Menu configuration must be data-driven.

---

# Phase 5 — Tax and Pricing Engine

## Goals

- AU GST 10%.
- AU GST-free items.
- NZ GST 15%.
- NZ zero-rated/exempt concepts.
- Tax-inclusive pricing.
- Tax snapshots on order lines.
- Order tax summary.
- Receipt tax markers.
- Pricing rules.
- Surcharge foundation.
- Discount foundation.

## Required mixed AU basket

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

---

# Phase 6 — Order Engine and POS Core

## Goals

- Create order.
- Add/remove items.
- Add modifiers.
- Add notes.
- Hold/resume order.
- Void line/order.
- Discounts.
- Surcharges.
- Calculate totals.
- Save order snapshots.
- Audit financial actions.

## Key principle

Financially meaningful records must not be silently edited. Use voids, refunds, reversals, and adjustment records.

---

# Phase 7 — Daxa Terminal MAUI Foundation

## Goals

- Create Daxa Terminal MAUI app.
- Staff POS screen.
- Terminal registration.
- Location/venue/terminal context.
- Product/category tile layout.
- Order panel.
- Payment action flow.
- API target configuration.
- Full-screen/borderless planning.
- Customer display state publishing foundation.

## Target

Windows counter POS terminals.

---

# Phase 8 — Daxa Display Customer Screen

## Goals

- Second MAUI window.
- Customer-facing display state.
- Idle branding.
- Order summary.
- Payment prompt.
- Approved/declined state.
- Receipt prompt placeholder.
- Display assignment config.

## Key principle

Daxa Display is not KDS.

It is the second customer-facing display at the point of sale.

---

# Phase 9 — Payments Foundation

## Goals

- Payment method model.
- Cash payment.
- Manual external EFTPOS.
- Split payments.
- Payment provider adapter interface.
- Fake/test payment provider.
- Payment terminal assignment.
- Payment idempotency.
- Refund model.
- Payment/refund audit.

## Future provider order

```text
Tyro
Zeller
Square Terminal
Stripe Terminal
Windcave
Adyen
Worldline
Global Payments
```

---

# Phase 10 — Receipts, Printing, Cash Drawer

## Goals

- Receipt model.
- Thermal receipt rendering.
- Tax invoice layout.
- GST-free markers.
- Refund receipts.
- Print queue.
- Printer configuration.
- ESC/POS abstraction.
- Cash drawer kick support.
- Reprint audit.

---

# Phase 11 — Admin / Back Office PWA

## Goals

- Admin shell.
- Tenant/location selection.
- Product management.
- Menu management.
- Tax configuration.
- Pricing/surcharge configuration.
- Payment provider setup.
- Terminal/device setup.
- User/role management.
- Reporting placeholders.
- Audit log view.

---

# Phase 12 — Reporting and Audit Hardening

## Goals

- Daily sales.
- Sales by payment method.
- Sales by category/product.
- Sales by staff.
- Sales by terminal/location.
- Tax report.
- Refunds report.
- Voids report.
- Discounts report.
- Surcharges report.
- Cash events report.
- Audit event filtering.
- Export foundation.

---

# Phase 13 — Daxa Sync Foundation

## Goals

- Local-to-cloud sync queue.
- Cloud-to-local sync queue.
- Sync event model.
- Idempotency.
- Retry.
- Conflict detection.
- Sync status.
- Sync audit.
- Backup/export hooks.

## Applies to

- Daxa Local.
- Daxa Hybrid.

---

# Phase 14 — Inventory Foundation

## Goals

- Inventory items.
- Stock movements.
- Product-to-stock links.
- Stock adjustment.
- Waste/spoilage.
- Sold-out handling.
- Daily production counts.
- Low-stock alert foundation.

## Industry relevance

- Bakery.
- Food truck.
- Retail.
- Electronics.
- Repair/service.
- Hospitality.

---

# Phase 15 — Daxa KDS Foundation

## Goals

- Station model.
- Station login.
- Station routing.
- KDS ticket generation.
- KDS PWA shell.
- Realtime updates.
- Full-state reload after reconnect.
- Bump/complete workflow.
- Void/cancel propagation.

## Key principle

KDS screens are separate from Daxa Display.

---

# Phase 16 — Hospitality Templates

## Goals

Add configuration templates for:

- Cafe.
- Bakery.
- Pub/bar.
- Restaurant.
- Fast food.
- Food truck.

Templates may include:

- Menus.
- Modifiers.
- Surcharges.
- Routing defaults.
- Receipt defaults.
- Tax defaults.
- Device role defaults.

---

# Phase 17 — Retail and Service Templates

## Goals

Add configuration templates for:

- General retail.
- Clothing.
- Electronics.
- Repair shop.
- Service counter.

Templates may include:

- SKU/barcode defaults.
- Variant defaults.
- Returns/exchanges.
- Warranty placeholders.
- Service job placeholders.
- Inventory defaults.

---

# Phase 18 — Cloud, Local, Hybrid Deployment Hardening

## Goals

- Cloud deployment docs.
- Local deployment docs.
- Hybrid deployment docs.
- Docker Compose stack.
- Environment variable reference.
- Backup/restore docs.
- Upgrade/rollback docs.
- Health checks.
- Logging.
- Monitoring.
- TLS/cert docs.
- Deployment smoke tests.

---

# Phase 19 — Payment Provider Integrations

## Goals

Implement real integrated payment providers.

Priority:

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

Each provider requires:

- Documentation.
- Credential configuration.
- Terminal pairing.
- Payment initiation.
- Payment status handling.
- Refunds.
- Test/sandbox support.
- Audit logging.
- Provider-specific manual test checklist.
- Certification/onboarding notes where applicable.

---

# Phase 20 — Global Expansion Readiness

## Goals

- Singapore readiness.
- Hong Kong readiness.
- UK readiness.
- US/Canada readiness.
- Tax-exclusive pricing.
- Multiple stacked tax components.
- Tips/gratuity.
- Multi-currency.
- Locale formats.
- Country-specific receipt requirements.
- Provider availability by country.
- Regional deployment notes.
- Localisation foundation.

---

# Long-Term Product Areas

## Loyalty

- Customer profiles.
- Points.
- Stamp cards.
- Membership QR.
- Discounts.
- Offers.
- Customer history.

## Gift cards

- Stored-value accounts.
- QR token model.
- Activation.
- Reload.
- Redemption.
- Customer email/phone linkage.
- Lost-card replacement.
- Balance check.

## Service jobs

- Device intake.
- Fault description.
- Quote approval.
- Parts/labour.
- Deposits.
- Repair lifecycle.
- Warranty.

## Advanced inventory

- Recipes/BOM.
- Suppliers.
- Purchase orders.
- Receiving.
- Batch/expiry.
- Serial/IMEI.
- Stocktake.
- Margin reports.

## External ordering

- Public menu API.
- Website/mobile ordering.
- External order source model.
- External payment model.
- Order acceptance/rejection.
- Pickup/delivery lifecycle.

---

# Roadmap Rule

Claude Code must not mark a phase as complete unless:

- Implementation is complete.
- Tests pass.
- Documentation is updated.
- ADRs are updated.
- Open issues are updated.
- Human review requirements are clear.
