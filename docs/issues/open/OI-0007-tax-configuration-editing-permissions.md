# OI-0007 — Tax Configuration Editing Permissions

## Status

Open

## Area

Tax / Identity / Security

## Summary

Who should be permitted to edit tax categories, tax rates, and tax configuration in Daxa POS, and under what conditions?

## Context

Tax configuration (GST rate, GST-free categories, tax-inclusive mode) affects every transaction in the system. An error in tax configuration has immediate and potentially retrospective financial impact.

Some venues may legitimately need to adjust tax settings (e.g. NZ vs AU GST, adding a new GST-free product category). However, unrestricted access to tax configuration is a compliance risk.

## Impact

- Determines the RBAC roles and permissions for tax configuration.
- Affects the admin portal UI (which screens are accessible by which roles).
- Affects audit logging requirements for tax configuration changes.
- Affects the sync direction (tax config is cloud-master in Hybrid mode).

## Options

1. **Super-admin only** — Only Daxa platform admins can change tax rates. Venues cannot change them.
2. **Organisation owner only** — Each tenant's organisation owner can manage their own tax configuration.
3. **Role-gated with approval** — Tax changes require a specific tax-management role, and changes are staged for review before activation.
4. **Country-template-locked** — Tax rates are set by country template. Venues may only select which template applies (AU, NZ, etc.). Rates are not editable by venues.

## Recommendation

Option 4 for tax rates (country-template-locked at launch). Tax category assignment to products should be organisation-owner-editable. Future: add a tax-management role for complex multi-jurisdiction scenarios.

## Decision Needed

- Who can change tax rates.
- Who can change product tax category assignments.
- Whether tax configuration changes are staged, immediate, or require approval.
- Audit requirements for tax configuration changes.

## Related ADRs

- [ADR-0006 — Tax-Line Based Tax Engine](../../adr/proposed/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../../adr/proposed/ADR-0010-financial-records-ledger-and-audit.md)

## Related Documents

- [Module: Tax](../../modules/tax.md)
- [Architecture: Security](../../architecture/security.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
