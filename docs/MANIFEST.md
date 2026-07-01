# Manifest

All documentation files in `./docs`.

## Top-Level

- `docs/README.md`
- `docs/MANIFEST.md`
- `docs/CHANGELOG.md`
- `docs/00-product-vision.md`
- `docs/01-platform-principles.md`
- `docs/02-mvp-scope.md`
- `docs/03-phase-roadmap.md`
- `docs/daxa-pos-product-structure.md`
- `docs/eftpos-systems.md`

## ADRs

- `docs/adr/index.md`
- `docs/adr/templates/ADR-template.md`

### Accepted

- `docs/adr/accepted/ADR-0001-single-codebase.md`
- `docs/adr/accepted/ADR-0002-cloud-local-hybrid-deployment.md`
- `docs/adr/accepted/ADR-0003-multi-location-by-default.md`
- `docs/adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md`
- `docs/adr/accepted/ADR-0005-payment-provider-adapter-architecture.md`
- `docs/adr/accepted/ADR-0006-tax-line-based-tax-engine.md`
- `docs/adr/accepted/ADR-0007-local-hybrid-sync-principles.md`
- `docs/adr/accepted/ADR-0008-device-identity-vs-user-identity.md`
- `docs/adr/accepted/ADR-0010-financial-records-ledger-and-audit.md`
- `docs/adr/accepted/ADR-0011-receipt-tax-marker-strategy.md`
- `docs/adr/accepted/ADR-0012-docker-local-deployment-strategy.md`
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`

### Superseded

- `docs/adr/superseded/ADR-0009-keycloak-or-identity-provider-strategy.md`

## Open Issues

- `docs/issues/index.md`

### Closed Issues

- `docs/issues/closed/OI-0001-first-payment-provider.md`
- `docs/issues/closed/OI-0002-identity-provider-local-cloud-hybrid.md`
- `docs/issues/closed/OI-0003-local-server-reference-hardware.md`
- `docs/issues/closed/OI-0004-first-receipt-printer-reference-device.md`
- `docs/issues/closed/OI-0005-first-payment-terminal-reference-device.md`
- `docs/issues/closed/OI-0006-hybrid-sync-conflict-rules.md`
- `docs/issues/closed/OI-0007-tax-configuration-editing-permissions.md`
- `docs/issues/closed/OI-0008-cloud-data-region-strategy.md`
- `docs/issues/closed/OI-0009-maui-app-update-delivery.md`
- `docs/issues/closed/OI-0010-local-keycloak-vs-cloud-keycloak.md`

## Architecture

- `docs/architecture/overview.md`
- `docs/architecture/01-core-architecture.md`
- `docs/architecture/02-domain-primitives.md`
- `docs/architecture/03-payment-adapter-architecture.md`
- `docs/architecture/04-tax-pricing-model.md`
- `docs/architecture/deployment-modes.md`
- `docs/architecture/device-strategy.md`
- `docs/architecture/multi-location.md`
- `docs/architecture/payment-adapters.md`
- `docs/architecture/security.md`
- `docs/architecture/sync.md`
- `docs/architecture/tax-engine.md`
- `docs/architecture/tenancy.md`

## Modules

- `docs/modules/01-core-pos-sales-screen.md`
- `docs/modules/02-order-types.md`
- `docs/modules/03-hospitality.md`
- `docs/modules/04-bakery-cake-shop.md`
- `docs/modules/05-food-truck.md`
- `docs/modules/06-retail.md`
- `docs/modules/07-services-repair.md`
- `docs/modules/08-payments.md`
- `docs/modules/09-tax-engine.md`
- `docs/modules/10-pricing-surcharges-discounts.md`
- `docs/modules/11-customer-display-receipts-printing.md`
- `docs/modules/12-kitchen-routing-inventory.md`
- `docs/modules/13-multi-location-users-cash-audit.md`
- `docs/modules/14-reporting-customers-gift-cards.md`
- `docs/modules/15-offline-device-admin-product-management.md`
- `docs/modules/16-internationalisation-industry-templates.md`
- `docs/modules/audit.md`
- `docs/modules/catalog.md`
- `docs/modules/customer-display.md`
- `docs/modules/customers.md`
- `docs/modules/devices.md`
- `docs/modules/gift-cards.md`
- `docs/modules/inventory.md`
- `docs/modules/kds.md`
- `docs/modules/menus.md`
- `docs/modules/orders.md`
- `docs/modules/payments.md`
- `docs/modules/pricing.md`
- `docs/modules/printing.md`
- `docs/modules/receipts.md`
- `docs/modules/refunds.md`
- `docs/modules/reporting.md`
- `docs/modules/surcharges.md`
- `docs/modules/sync.md`
- `docs/modules/tax.md`

## Deployment

- `docs/deployment/cloud.md`
- `docs/deployment/docker.md`
- `docs/deployment/docker-deployment.md`
- `docs/deployment/hybrid.md`
- `docs/deployment/linux-kiosk-pwa.md`
- `docs/deployment/local.md`
- `docs/deployment/windows-terminal.md`

## Testing

- `docs/testing/testing-strategy.md`
- `docs/testing/tax-tests.md`
- `docs/testing/payment-tests.md`
- `docs/testing/receipt-tests.md`
- `docs/testing/security-tests.md`
- `docs/testing/sync-tests.md`

## Integrations

### Payment Providers

- `docs/integrations/payments/tyro.md`
- `docs/integrations/payments/zeller.md`
- `docs/integrations/payments/square-terminal.md`
- `docs/integrations/payments/stripe-terminal.md`
- `docs/integrations/payments/windcave.md`
- `docs/integrations/payments/adyen.md`
- `docs/integrations/payments/worldline.md`
- `docs/integrations/payments/global-payments.md`

### Printers

- `docs/integrations/printers/escpos.md`

## Regions

- `docs/regions/01-au-nz-tax.md`
- `docs/regions/02-global-tax.md`
- `docs/regions/03-payment-provider-roadmap.md`
- `docs/regions/04-square-terminal-notes.md`

## Plans

- `docs/plans/templates/PLAN-template.md`
- `docs/plans/active/PLAN-0000-repository-documentation-cleanup.md`
- `docs/plans/active/PLAN-0001-architecture-foundation.md`
- `docs/plans/active/PLAN-0002-platform-skeleton.md`
- `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md`
- `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`
- `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md`
- `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`
- `docs/plans/active/PLAN-0007-sync-local-hybrid-planning.md`
- `docs/plans/active/PLAN-0008-testing-security-deployment-planning.md`
- `docs/plans/active/PLAN-docs-consolidation.md`
- `docs/plans/active/planning-session-instructions.md`
- `docs/plans/active/planning-session-worker-notes.md`

## Planning (Legacy / Reference)

- `docs/planning/01-claude-code-ingestion-prompt.md`
- `docs/planning/02-initial-epics.md`
- `docs/planning/03-adr-candidates.md`
- `docs/planning/04-open-questions.md`
- `docs/planning/05-suggested-dotnet-solution-structure.md`

## Configuration

- `docs/configuration/configuration-overview.md`

## Security

- `docs/security/security-overview.md`

## Infrastructure

- `docs/infrastructure/hardware-layout.md`

## Project Plan

- `docs/project-plan/project-roadmap.md`

## Documentation Standards

- `docs/documentation/documentation-standards.md`

## GitHub Workflow

- `docs/github/github-workflow.md`

## Instructions

- `docs/instructions/claude-worker-cycle.md`

## Workers

- `docs/workers/worker-backlog.md`
