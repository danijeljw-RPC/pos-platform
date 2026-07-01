# OI-0007 — Tax Configuration Editing Permissions

## Status

Closed

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

- [ADR-0006 — Tax-Line Based Tax Engine](../../adr/accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)

## Related Documents

- [Module: Tax](../../modules/tax.md)
- [Architecture: Security](../../architecture/security.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)

---

## Decision

Tax configuration changes are permitted for users with **manager-level permission or higher**, provided they also have permission to create, import, update, or delete products in the catalogue.

This applies to:

- Changing tax rates.
- Changing product tax category assignments.
- Creating new taxable or tax-exempt product configuration.
- Updating catalogue items where the tax treatment changes.

## Permission Rule

A user may change tax configuration only when they have catalogue management permission.

The relevant permission should cover users who can:

- Create products.
- Import products.
- Update products.
- Delete or archive products.
- Assign products to tax categories.
- Update product tax category assignments.

This keeps tax configuration aligned with catalogue management instead of making it a separate super-admin-only function.

## Change Behaviour

Tax configuration changes are **immediate from that point forward**.

They do not require staging or approval before becoming active.

However, changes must not mutate historical product or transaction records.

When a product-level tax change is made:

1. The existing product record is archived.
2. The archived product remains available for historical orders, receipts, reports, and audit records.
3. A new product record is created with the updated tax configuration.
4. The new product record becomes available for future sales.
5. Historical records continue to reference the archived product/tax snapshot that applied at the time of sale.

This prevents tax changes from corrupting historical data or changing the meaning of previous transactions.

## Rationale

Tax settings affect financial reporting and compliance, so they must be restricted to users who already have trusted catalogue management permissions.

The system should not allow casual staff or general POS users to change tax configuration.

Immediate activation is acceptable because tax changes should apply from the moment the catalogue is updated. Historical accuracy is preserved by archiving the old product record and creating a new product record rather than modifying the existing product in place.

## Audit Requirements

All tax configuration changes must be audit logged.

The audit log should record:

- The user who made the change.
- The date and time of the change.
- The old product or tax configuration.
- The new product or tax configuration.
- Whether a product was archived and replaced.
- The reason for change, if supplied by the user.

## Outcome

- Tax rates can be changed by manager-level users or higher with catalogue management permissions.
- Product tax category assignments can be changed by the same permission group.
- Tax configuration changes are immediate.
- Historical data is protected by archiving the old product record and creating a new product record.
- Existing sales, receipts, reports, and tax snapshots remain immutable.

## Status Update

This open issue is resolved by allowing tax configuration changes for manager-level or higher catalogue-management users, with immediate forward-only effect and archival replacement of changed product records.

Status: **Closed**
