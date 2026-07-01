# Documentation Changelog — Daxa POS

Changes are listed in reverse chronological order.

---

## 2026-07-01

### Summary

- Applied all accepted ADR and resolved Open Issue decisions consistently across the documentation set.
- Fixed critical index errors: ADR index and Issues index were both completely out of date.
- Replaced all stale `adr/proposed/` links with `adr/accepted/` or `adr/superseded/` as appropriate.
- Replaced all stale `issues/open/` links with `issues/closed/`.
- Updated architecture, deployment, and module docs to reflect ADR-0013 authentication model.
- Expanded MANIFEST.md to cover all files in the docs directory.
- Created PLAN-docs-consolidation.md to track this work.

### Files Changed

- `docs/adr/index.md` — corrected Proposed/Accepted/Superseded sections, added ADR-0013
- `docs/issues/index.md` — all 10 OIs moved to Closed, status updated
- `docs/README.md` — removed stale "no accepted ADRs" claim, updated issues section, added PLAN-docs-consolidation
- `docs/architecture/security.md` — full rewrite to reflect ADR-0013 mixed auth model
- `docs/architecture/sync.md` — fixed `adr/accepted/` link
- `docs/architecture/tax-engine.md` — fixed `adr/accepted/` link
- `docs/modules/tax.md` — fixed `adr/accepted/` links
- `docs/deployment/local.md` — removed Keycloak from Docker Compose stack, updated hardware spec, updated OI-0003 link
- `docs/adr/superseded/ADR-0009-keycloak-or-identity-provider-strategy.md` — fixed typo "Superseeded" → "Superseded"
- `docs/issues/closed/OI-0008-cloud-data-region-strategy.md` — fixed Status field from "Open" to "Closed"
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md` — fixed OI links to `issues/closed/`
- `docs/issues/closed/OI-0001-first-payment-provider.md` — fixed ADR link
- `docs/issues/closed/OI-0002-identity-provider-local-cloud-hybrid.md` — fixed ADR link
- `docs/issues/closed/OI-0003-local-server-reference-hardware.md` — fixed ADR links
- `docs/issues/closed/OI-0005-first-payment-terminal-reference-device.md` — fixed ADR link
- `docs/issues/closed/OI-0006-hybrid-sync-conflict-rules.md` — fixed ADR links
- `docs/issues/closed/OI-0007-tax-configuration-editing-permissions.md` — fixed ADR links
- `docs/issues/closed/OI-0008-cloud-data-region-strategy.md` — fixed ADR links
- `docs/issues/closed/OI-0009-maui-app-update-delivery.md` — fixed ADR link
- `docs/issues/closed/OI-0010-local-keycloak-vs-cloud-keycloak.md` — fixed ADR links
- All remaining docs with `adr/proposed/` or `issues/open/` references — bulk replaced via sed
- `docs/MANIFEST.md` — expanded to cover all ~120 files
- `docs/CHANGELOG.md` — created (this file)
- `docs/plans/active/PLAN-docs-consolidation.md` — created

### Decisions Applied

- ADR-0013 — Cloud Identity and Local POS Authentication Strategy (supersedes ADR-0009)
- OI-0001 — First Payment Provider: Stripe Terminal selected
- OI-0002 — Identity Provider: resolved by ADR-0013
- OI-0003 — Local Server Reference Hardware: baseline defined
- OI-0004 — First Receipt Printer: Epson TM-T88VI selected
- OI-0005 — First Payment Terminal: Stripe BBPOS WisePOS E selected
- OI-0006 — Hybrid Sync Conflict Rules: category-based model adopted
- OI-0007 — Tax Configuration Permissions: manager-level + catalogue permission
- OI-0008 — Cloud Data Region: configurable per-tenant region strategy
- OI-0009 — MAUI App Updates: operator-controlled via Daxa Local server
- OI-0010 — Local Keycloak: resolved by ADR-0013, no local Keycloak for MVP

### Follow-Up Items

- None. No new ADRs or OIs were created during this pass.
