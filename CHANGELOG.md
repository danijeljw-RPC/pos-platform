# Changelog

All meaningful changes to Daxa POS documentation, planning, and code are recorded here.

Format follows: Added / Changed / Fixed / Documentation / ADRs / Issues / Planning.

---

## Unreleased

### Documentation

- Created `docs/README.md` — top-level documentation index linking all sections.
- Created `docs/plans/templates/PLAN-template.md` — standard plan template.
- Created `docs/architecture/overview.md` — architecture overview and solution structure.
- Created `docs/architecture/deployment-modes.md` — Cloud, Local, and Hybrid deployment mode descriptions.
- Created `docs/architecture/tenancy.md` — multi-tenant hierarchy and tenant isolation.
- Created `docs/architecture/multi-location.md` — multi-location architecture and examples.
- Created `docs/architecture/sync.md` — Daxa Sync architecture and principles.
- Created `docs/architecture/security.md` — identity, authentication, authorisation, and audit.
- Created `docs/architecture/payment-adapters.md` — payment provider adapter model.
- Created `docs/architecture/tax-engine.md` — tax-line based tax engine design.
- Created `docs/architecture/device-strategy.md` — MAUI + PWA device strategy.
- Created `docs/modules/catalog.md` — product catalogue module.
- Created `docs/modules/menus.md` — menu service module.
- Created `docs/modules/orders.md` — order service module.
- Created `docs/modules/payments.md` — payment service module.
- Created `docs/modules/refunds.md` — refund service module.
- Created `docs/modules/tax.md` — tax engine module.
- Created `docs/modules/pricing.md` — pricing engine module.
- Created `docs/modules/surcharges.md` — surcharges module.
- Created `docs/modules/receipts.md` — receipt service module.
- Created `docs/modules/printing.md` — printer service module.
- Created `docs/modules/inventory.md` — inventory module.
- Created `docs/modules/customers.md` — customer service module.
- Created `docs/modules/gift-cards.md` — gift card module (Phase 2+).
- Created `docs/modules/devices.md` — device and terminal service module.
- Created `docs/modules/reporting.md` — reporting service module.
- Created `docs/modules/audit.md` — audit log service module.
- Created `docs/modules/sync.md` — sync module.
- Created `docs/modules/kds.md` — KDS module (Phase 2).
- Created `docs/modules/customer-display.md` — customer display module.
- Created `docs/deployment/cloud.md` — Daxa Cloud deployment.
- Created `docs/deployment/local.md` — Daxa Local deployment.
- Created `docs/deployment/hybrid.md` — Daxa Hybrid deployment.
- Created `docs/deployment/docker.md` — Docker Compose deployment summary (links to `docker-deployment.md`).
- Created `docs/deployment/windows-terminal.md` — Daxa Terminal MAUI deployment.
- Created `docs/deployment/linux-kiosk-pwa.md` — Linux kiosk PWA deployment.
- Created `docs/testing/tax-tests.md` — required tax engine test cases.
- Created `docs/testing/payment-tests.md` — required payment and refund test cases.
- Created `docs/testing/sync-tests.md` — required sync test cases.
- Created `docs/testing/receipt-tests.md` — required receipt rendering test cases.
- Created `docs/testing/security-tests.md` — required security and isolation test cases.
- Created `docs/integrations/payments/tyro.md` — Tyro integration stub.
- Created `docs/integrations/payments/zeller.md` — Zeller integration stub.
- Created `docs/integrations/payments/square-terminal.md` — Square Terminal integration stub.
- Created `docs/integrations/payments/stripe-terminal.md` — Stripe Terminal integration stub.
- Created `docs/integrations/payments/windcave.md` — Windcave integration stub.
- Created `docs/integrations/payments/adyen.md` — Adyen integration stub.
- Created `docs/integrations/payments/worldline.md` — Worldline integration stub.
- Created `docs/integrations/payments/global-payments.md` — Global Payments integration stub.
- Created `docs/integrations/printers/escpos.md` — ESC/POS printer protocol integration.

### ADRs

- Created `docs/adr/proposed/ADR-0001-single-codebase.md` — single codebase for all deployment modes.
- Created `docs/adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md` — three deployment modes.
- Created `docs/adr/proposed/ADR-0003-multi-location-by-default.md` — multi-location as baseline.
- Created `docs/adr/proposed/ADR-0004-windows-maui-and-pwa-device-strategy.md` — MAUI for Windows, PWA for other devices.
- Created `docs/adr/proposed/ADR-0005-payment-provider-adapter-architecture.md` — provider-agnostic adapters.
- Created `docs/adr/proposed/ADR-0006-tax-line-based-tax-engine.md` — tax per order line with snapshots.
- Created `docs/adr/proposed/ADR-0007-local-hybrid-sync-principles.md` — idempotent sync, explicit conflicts.
- Created `docs/adr/proposed/ADR-0008-device-identity-vs-user-identity.md` — separation of device and user identity.
- Created `docs/adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md` — Keycloak as proposed IAM.
- Created `docs/adr/proposed/ADR-0010-financial-records-ledger-and-audit.md` — immutable financial records.
- Created `docs/adr/proposed/ADR-0011-receipt-tax-marker-strategy.md` — GST-free marker on receipts.
- Created `docs/adr/proposed/ADR-0012-docker-local-deployment-strategy.md` — Docker Compose deployment.
- Updated `docs/adr/index.md` — now lists all 12 proposed ADRs grouped by status.

### Issues

- Created `docs/issues/open/OI-0001-first-payment-provider.md`
- Created `docs/issues/open/OI-0002-identity-provider-local-cloud-hybrid.md`
- Created `docs/issues/open/OI-0003-local-server-reference-hardware.md`
- Created `docs/issues/open/OI-0004-first-receipt-printer-reference-device.md`
- Created `docs/issues/open/OI-0005-first-payment-terminal-reference-device.md`
- Created `docs/issues/open/OI-0006-hybrid-sync-conflict-rules.md`
- Created `docs/issues/open/OI-0007-tax-configuration-editing-permissions.md`
- Created `docs/issues/open/OI-0008-cloud-data-region-strategy.md`
- Created `docs/issues/open/OI-0009-maui-app-update-delivery.md`
- Created `docs/issues/open/OI-0010-local-keycloak-vs-cloud-keycloak.md`
- Created `docs/issues/index.md` — issue index grouped by area.

### Planning

- Created `docs/plans/active/PLAN-0000-repository-documentation-cleanup.md`
- Created `docs/plans/active/PLAN-0001-architecture-foundation.md`
- Created `docs/plans/active/PLAN-0002-platform-skeleton.md`
- Created `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md`
- Created `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`
- Created `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md`
- Created `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`
- Created `docs/plans/active/PLAN-0007-sync-local-hybrid-planning.md`
- Created `docs/plans/active/PLAN-0008-testing-security-deployment-planning.md`
- Created `docs/plans/templates/PLAN-template.md`
- Created `docs/plans/active/planning-session-worker-notes.md`

---

*This changelog was started during the documentation planning session on 2026-06-29.*
