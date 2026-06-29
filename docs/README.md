# Daxa POS — Documentation

This folder contains all planning, architecture, decision, and module documentation for **Daxa POS**.

---

## Top-Level Documents

- [Product Vision](00-product-vision.md)
- [Platform Principles](01-platform-principles.md)
- [MVP Scope](02-mvp-scope.md)
- [Phase Roadmap](03-phase-roadmap.md)
- [Product Structure](daxa-pos-product-structure.md)
- [MANIFEST](MANIFEST.md)

---

## Architecture Decisions (ADRs)

- [ADR Index](adr/index.md)

Proposed ADRs are under `adr/proposed/`. No ADRs have been accepted yet — all require human review.

---

## Active Plans

- [PLAN-0000 — Repository Documentation Cleanup](plans/active/PLAN-0000-repository-documentation-cleanup.md)
- [PLAN-0001 — Architecture Foundation](plans/active/PLAN-0001-architecture-foundation.md)
- [PLAN-0002 — Platform Skeleton](plans/active/PLAN-0002-platform-skeleton.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [PLAN-0005 — Payments, Receipts, Printing](plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [PLAN-0006 — Terminal, Display, PWA](plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [PLAN-0007 — Sync, Local, Hybrid](plans/active/PLAN-0007-sync-local-hybrid-planning.md)
- [PLAN-0008 — Testing, Security, Deployment](plans/active/PLAN-0008-testing-security-deployment-planning.md)

---

## Open Issues

- [Issue Index](issues/index.md)

Open issues are under `issues/open/`. Issues require human decision before closing.

---

## Architecture

- [Overview](architecture/overview.md)
- [Deployment Modes](architecture/deployment-modes.md)
- [Tenancy](architecture/tenancy.md)
- [Multi-Location](architecture/multi-location.md)
- [Sync](architecture/sync.md)
- [Security](architecture/security.md)
- [Payment Adapters](architecture/payment-adapters.md)
- [Tax Engine](architecture/tax-engine.md)
- [Device Strategy](architecture/device-strategy.md)

---

## Modules

- [Product Catalogue](modules/catalog.md)
- [Menus](modules/menus.md)
- [Orders](modules/orders.md)
- [Payments](modules/payments.md)
- [Refunds](modules/refunds.md)
- [Tax](modules/tax.md)
- [Pricing](modules/pricing.md)
- [Surcharges](modules/surcharges.md)
- [Receipts](modules/receipts.md)
- [Printing](modules/printing.md)
- [Inventory](modules/inventory.md)
- [Customers](modules/customers.md)
- [Gift Cards](modules/gift-cards.md)
- [Devices](modules/devices.md)
- [Reporting](modules/reporting.md)
- [Audit](modules/audit.md)
- [Sync](modules/sync.md)
- [KDS](modules/kds.md)
- [Customer Display](modules/customer-display.md)

---

## Deployment

- [Cloud](deployment/cloud.md)
- [Local](deployment/local.md)
- [Hybrid](deployment/hybrid.md)
- [Docker](deployment/docker.md)
- [Windows Terminal](deployment/windows-terminal.md)
- [Linux Kiosk PWA](deployment/linux-kiosk-pwa.md)

---

## Testing

- [Strategy](testing/strategy.md)
- [Tax Tests](testing/tax-tests.md)
- [Payment Tests](testing/payment-tests.md)
- [Sync Tests](testing/sync-tests.md)
- [Receipt Tests](testing/receipt-tests.md)
- [Security Tests](testing/security-tests.md)

---

## Integrations

### Payment Providers

- [Tyro](integrations/payments/tyro.md)
- [Zeller](integrations/payments/zeller.md)
- [Square Terminal](integrations/payments/square-terminal.md)
- [Stripe Terminal](integrations/payments/stripe-terminal.md)
- [Windcave](integrations/payments/windcave.md)
- [Adyen](integrations/payments/adyen.md)
- [Worldline](integrations/payments/worldline.md)
- [Global Payments](integrations/payments/global-payments.md)

### Printers

- [ESC/POS](integrations/printers/escpos.md)

---

## Planning (Legacy / Reference)

These files contain early planning content used to build this documentation structure:

- [Claude Code Ingestion Prompt](planning/01-claude-code-ingestion-prompt.md)
- [Initial Epics](planning/02-initial-epics.md)
- [ADR Candidates](planning/03-adr-candidates.md)
- [Open Questions](planning/04-open-questions.md)
- [Suggested .NET Solution Structure](planning/05-suggested-dotnet-solution-structure.md)

---

## Regions

- [AU/NZ Tax](regions/01-au-nz-tax.md)
- [Global Tax](regions/02-global-tax.md)
- [Payment Provider Roadmap](regions/03-payment-provider-roadmap.md)
- [Square Terminal Notes](regions/04-square-terminal-notes.md)

---

## Other

- [EFTPOS Systems Overview](eftpos-systems.md)
- [Configuration Overview](configuration/configuration-overview.md)
- [Documentation Standards](documentation/documentation-standards.md)
- [GitHub Workflow](github/github-workflow.md)
- [Hardware Layout](infrastructure/hardware-layout.md)
- [Claude Worker Cycle](instructions/claude-worker-cycle.md)
- [Security Overview](security/security-overview.md)
- [Project Roadmap](project-plan/project-roadmap.md)
- [Worker Backlog](workers/worker-backlog.md)
