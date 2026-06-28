# Configuration Overview — Daxa POS

## Purpose

This document defines the configuration areas for Daxa POS.

Daxa POS must support:

- Cloud deployment.
- Local/on-prem deployment.
- Hybrid deployment.
- Multi-tenant configuration.
- Multi-location configuration.
- Industry templates.
- Country/tax configuration.
- Payment provider configuration.
- Device/terminal configuration.
- Printer and cash drawer configuration.
- Sync configuration.
- Security configuration.

Configuration must be data-driven and auditable where operationally significant.

---

# Configuration Principles

## 1. Configuration is part of the product

Daxa POS should not require code changes for normal venue setup.

Products, menus, prices, taxes, surcharges, printers, devices, payment terminals, and roles must be configurable.

## 2. Dangerous changes require audit

Configuration changes affecting financial, tax, payment, security, or device behaviour must be audited.

## 3. Multi-location is default

Configuration should support:

- Organisation-level defaults.
- Region-level defaults later.
- Country-level rules.
- Location-level overrides.
- Terminal-level assignments.

## 4. Deployment mode is configuration/infrastructure

Daxa Cloud, Daxa Local, and Daxa Hybrid must be deployment modes, not separate products/codebases.

## 5. Industry templates are configuration

Daxa Hospitality and Daxa Retail are template/configuration sets, not separate codebases.

---

# Configuration Areas

## Tenant and organisation

- Tenant name.
- Organisation name.
- Regions.
- Countries.
- Locations/venues.
- Franchise/group structure.
- Support access configuration.
- Billing/licensing later.

## Location / venue settings

- Venue name.
- Address.
- Country.
- Time zone.
- Currency.
- Tax profile.
- Receipt profile.
- Payment profile.
- Device profile.
- Printer profile.
- Trading hours.
- Order types enabled.
- Surcharge rules.
- Offline/local mode settings.

## Device registrations

- Device name.
- Device type.
- Device registration token.
- Tenant.
- Organisation.
- Location.
- Terminal assignment.
- Customer display assignment.
- Printer assignment.
- Payment terminal assignment.
- KDS station assignment.
- Device status.
- App version.
- Last seen.

## User roles and permissions

- Roles.
- Permissions.
- Staff PIN.
- Admin access.
- Venue access.
- Location access.
- Manager override permissions.
- Support access.
- Accountant/reporting access.

## Products and menus

- Categories.
- Products.
- Product variants.
- SKU/barcode.
- Product images.
- Modifiers.
- Modifier groups.
- Menu layout.
- POS tile layout.
- Menu availability.
- Location-specific availability.
- Sold-out state.
- Product tax category.
- Print/prep routing.

## Pricing

- Base price.
- Modifier price.
- Size price.
- Location price.
- Customer group price.
- Time/day price.
- Happy hour price.
- Public holiday price.
- Promotion price.
- Manual override permission.

## Tax

- Country tax profile.
- Tax-inclusive/exclusive mode.
- Tax categories.
- Tax rates.
- GST/GST-free markers.
- Zero-rated/exempt categories.
- Surcharge taxability.
- Discount tax behaviour.
- Rounding mode.
- Receipt tax labels.

## Surcharges and fees

- Card surcharge.
- Sunday surcharge.
- Public holiday surcharge.
- Service charge.
- Delivery fee.
- Packaging fee.
- Bottle deposit.
- Environmental levy.
- Event surcharge.
- Taxable yes/no.
- Applies to order types.
- Applies to payment methods.
- Applies by day/time/date.
- Shown on receipt yes/no.

## Payment methods

- Cash.
- Manual external EFTPOS.
- Integrated EFTPOS.
- Gift card later.
- Store credit later.
- Voucher later.
- Account/customer charge.
- Deposits.
- Split payment.
- Tips later.

## Payment providers

- Provider name.
- Merchant/account ID.
- Location ID.
- Terminal IDs.
- API credentials.
- Webhook secrets.
- Refund capability.
- Terminal pairing status.
- Sandbox/production mode.
- Enabled locations.
- Default provider per location.
- Default terminal per POS terminal.

## Printers

- Printer name.
- Printer type.
- IP address/hostname.
- Port.
- Protocol.
- ESC/POS profile.
- Receipt printer assignment.
- Kitchen printer assignment.
- Bar printer assignment.
- Label printer assignment later.
- Cash drawer mapping.
- Print retry rules.

## Cash drawer

- Drawer name.
- Attached printer.
- Kick command profile.
- Allowed users/roles.
- No-sale permission.
- Audit configuration.

## Stations and KDS

- Station name.
- Station type.
- Station PIN.
- Routing rules.
- KDS device assignment.
- Ticket display rules.
- Bump/complete permissions.
- Reconnect behaviour.
- Print fallback.

## Inventory

- Stock tracking enabled.
- Stock item definitions.
- Product-to-stock mapping.
- Decrement rules.
- Waste/spoilage reasons.
- Low-stock thresholds.
- Stocktake settings.
- Daily production reset.
- Supplier settings later.

## Customer / loyalty

- Customer capture enabled.
- Required customer fields.
- Digital receipt settings.
- Loyalty enabled later.
- Gift card enabled later.
- Store credit enabled later.
- Cross-location customer profile setting.

## Sync

- Deployment mode.
- Cloud sync endpoint.
- Sync enabled.
- Sync direction.
- Sync frequency.
- Retry rules.
- Conflict rules.
- Offline queue limits.
- Backup/export endpoint.
- Last successful sync.
- Sync health alert settings.

## Backup

- Backup enabled.
- Backup schedule.
- Backup destination.
- Retention.
- Encryption.
- Cloud backup endpoint.
- Customer data lake endpoint.
- Restore procedure.
- Last successful backup.

## Updates

- Update channel.
- Update endpoint.
- Auto-update enabled.
- Manual update required.
- Maintenance window.
- Rollback settings.
- MAUI app update method.
- PWA update behaviour.

## Security

- Identity provider.
- Keycloak realm/config if used.
- Token lifetimes.
- PIN policy.
- Rate limits.
- Device token policy.
- Support access policy.
- Audit retention.
- Secret storage.
- Password policy.
- MFA setting for admins later.

---

# Configuration Storage

## Database configuration

Most operational configuration should be stored in the database.

Examples:

- Venue settings.
- Device registrations.
- User roles and permissions.
- Product/menu/tax/pricing config.
- Printer destinations.
- Payment methods.
- KDS station definitions.
- Stock settings.
- Surcharge rules.
- Receipt templates.

## Environment configuration

Environment configuration should be used for infrastructure-level settings.

Examples:

- Database connection.
- Deployment mode.
- Local hostname.
- TLS/certificate settings.
- Cloud sync endpoint.
- Backup endpoint.
- Licence endpoint.
- Update endpoint.
- Logging level.
- Worker toggles.
- Secret references.

## Secret storage

Sensitive configuration must be protected.

Sensitive examples:

- Payment provider API keys.
- Webhook secrets.
- Database passwords.
- JWT/signing keys.
- Sync credentials.
- Backup credentials.
- SMTP/SMS keys.
- Keycloak secrets.

Do not store secrets in plaintext configuration files.

Do not commit secrets.

---

# Admin-Editable Configuration

Admin portal should expose safe operational configuration.

Examples:

- Products.
- Menus.
- Prices.
- Surcharges.
- Tax categories where permitted.
- Devices.
- Printers.
- Payment providers.
- Staff/users.
- Roles.
- Receipt templates.
- Stock settings.
- Trading hours.
- KDS routing.

Dangerous settings should require supervisor/admin permissions and audit logging.

Dangerous settings include:

- Tax settings.
- Payment provider credentials.
- Refund permissions.
- Cash drawer permissions.
- Device disable/reassignment.
- Sync settings.
- Backup settings.
- User role changes.

---

# Configuration Inheritance

Configuration should support inheritance.

Recommended model:

```text
Platform default
↓
Country default
↓
Industry template
↓
Organisation default
↓
Location override
↓
Terminal assignment
```

Example:

```text
Country: AU
Tax: GST-inclusive
Currency: AUD

Industry template: Bakery
Default products/modifiers/receipt markers

Organisation: Main Street Bakery Group
Shared catalogue

Location: Bondi Store
Specific prices and printers

Terminal: Front Counter 1
Specific payment terminal and receipt printer
```

---

# Deployment Mode Configuration

## Cloud

```text
DeploymentMode = Cloud
UsesLocalServer = false
CloudSyncEnabled = false
ApiBaseUrl = https://api.daxapos.com
```

## Local

```text
DeploymentMode = Local
UsesLocalServer = true
CloudSyncEnabled = false or optional backup only
ApiBaseUrl = https://daxa.local
```

## Hybrid

```text
DeploymentMode = Hybrid
UsesLocalServer = true
CloudSyncEnabled = true
LocalApiBaseUrl = https://daxa.local
CloudApiBaseUrl = https://api.daxapos.com
```

---

# Industry Template Configuration

## Cafe

- Coffee categories.
- Milk modifiers.
- Size modifiers.
- Takeaway/dine-in.
- GST.
- Customer display.
- Receipt printing.
- Quick payment.

## Bakery

- Daily stock.
- Cake orders.
- GST-free marker support.
- Production quantities.
- Pickup orders.
- Deposits later.
- Labels later.

## Pub/bar

- Bar tabs.
- Split bills.
- Table areas.
- Happy hour.
- Public holiday surcharge.
- Sunday surcharge.
- Tips later.
- Bar routing.

## Restaurant

- Tables/floor plan.
- Dine-in/takeaway.
- Split bills.
- Course firing later.
- KDS routing.
- Kitchen notes.
- Service charge.

## Food truck

- Offline-aware mode.
- Event/location tagging.
- Limited menu.
- Stock countdown.
- Quick payment.
- End-of-day event summary.

## Retail

- SKU/barcode.
- Variants.
- Returns.
- Exchanges.
- Gift receipts.
- Inventory.
- Stocktake later.

## Repair shop

- Service jobs.
- Customer device record.
- Fault description.
- Intake checklist.
- Quote approval.
- Parts/labour lines.
- Deposits.
- Job status lifecycle.

---

# Required Configuration Tests

Tests must cover:

- Tenant-level config isolation.
- Location override behaviour.
- Terminal assignment.
- Device disabled behaviour.
- Product tax category config.
- GST-free marker config.
- Payment provider config.
- Printer assignment.
- Cash drawer permission config.
- Surcharge config.
- Role/permission config.
- Sync config.
- Deployment mode config.

---

# Open Questions

Create open issues for:

- Which settings are organisation-level vs location-level for MVP?
- Which settings should be inherited vs copied from templates?
- Should tax configuration be editable by venues or locked by country templates?
- Should payment provider setup be restricted to owner/admin only?
- Should dangerous config changes require manager re-authentication?
- Should local/hybrid deployment mode be locked after installation?
- How should config changes sync from cloud to local in hybrid mode?
