# Security Overview — Daxa POS

## Purpose

This document defines the security goals and baseline security model for Daxa POS.

Daxa POS must support:

- Daxa Cloud
- Daxa Local
- Daxa Hybrid
- Multi-tenant cloud environments
- Local/on-prem venue environments
- Hybrid local-to-cloud sync
- Windows MAUI POS terminals
- PWA-based admin/KDS/tablet/kiosk devices
- Integrated payment providers
- Multi-location tenants
- Staff PIN workflows
- Admin user workflows
- Device registration
- Financial auditability

Security must be designed into the platform early. Do not treat security as a later add-on.

---

# Security Goals

Daxa POS must ensure:

- Tenant data is isolated.
- Location data access is controlled.
- Local trading can continue without internet where configured.
- Only authorised users can perform POS/admin actions.
- Only registered devices can act as terminals.
- Staff permissions are enforced.
- Device access is controlled.
- Payment provider credentials are protected.
- Financial actions are audited.
- Tax, payment, refund, discount, and void actions are traceable.
- Cloud/local sync connections are authenticated and encrypted.
- Customer data is protected.
- Gift card and store credit tokens are not predictable.
- Support/admin access is auditable.
- Lost/stolen devices can be disabled.
- Configuration changes are audited.

---

# Threat Model Summary

## Main threats

| Threat | Control |
|---|---|
| Cross-tenant data access | Tenant isolation and authorization tests |
| Cross-location access | Location scoped authorization |
| Lost/stolen POS device | Device disable/revoke |
| Staff abusing refunds/voids | Permission checks and audit logs |
| Staff opening cash drawer | Permission checks and cash drawer audit |
| Payment provider credential exposure | Secret storage and restricted access |
| Sync replay/duplicate events | Idempotency keys |
| Fake device registration | Registration flow with server-side approval |
| Guest Wi-Fi attacking POS network | Network segmentation |
| Printer/cash drawer exposure | Keep devices off guest network |
| Cloud outage affecting local venue | Daxa Local/Hybrid resilience |
| Local server compromise | OS, network, TLS, backups, least privilege |
| Gift card guessing | Random high-entropy tokens and rate limits |

---

# Tenant and Location Isolation

Daxa POS is multi-tenant.

Every request that accesses tenant data must be scoped to the authenticated tenant and authorised location set.

## Rules

- Tenant ID must not be trusted from client input alone.
- Location access must be checked server-side.
- Admin users may have organisation-wide access.
- Venue managers may have location-scoped access.
- Staff should normally have terminal/location-scoped access.
- Support access must be explicit and audited.
- Cross-tenant access must be blocked and tested.
- Cross-location access must be blocked and tested unless explicitly permitted.

## Required tests

- User from Tenant A cannot access Tenant B.
- User from Location A cannot access Location B unless permitted.
- Terminal registered to Location A cannot post orders to Location B.
- Payment terminal mapping cannot be used across locations unless configured.
- Report queries are location-scoped.

---

# Identity and Authentication

## Identity options

Daxa POS may use Keycloak or similar identity management.

The identity design must support:

- Cloud deployment.
- Local deployment.
- Hybrid deployment.
- Admin users.
- Staff users.
- PIN workflows.
- Device identity.
- Service identities.
- Sync identities.
- Support identities.

## Authentication types

| Type | Purpose |
|---|---|
| Admin login | Back office/admin access |
| Staff login | Named user access |
| Staff PIN | Fast POS terminal access |
| Device registration token | Identifies terminal/device |
| Station PIN | KDS/prep station access where needed |
| Service token | Workers/sync/background services |
| Support access | Audited support/admin access |

## Staff PINs

Staff PINs are for fast terminal workflows.

Rules:

- PINs must be salted/hashed.
- PINs must not be stored in plaintext.
- PIN login must be scoped to tenant/location/terminal.
- PIN attempts should be rate-limited.
- Staff actions must still be attributed to a user.
- Manager override PINs must be audited.
- Staff PIN login must not replace admin authentication.

---

# Authorization

Daxa POS must support role-based and permission-based authorization.

## Example roles

- Owner
- Admin
- Venue manager
- Supervisor
- Staff/cashier
- Bar staff
- Kitchen staff
- Accountant
- Support user
- Franchise manager
- Service/worker

## Example permissions

```text
CanAccessBackOffice
CanManageProducts
CanManageMenus
CanManageTaxSettings
CanManagePaymentProviders
CanManageDevices
CanManagePrinters
CanManageUsers
CanViewReports
CanOpenCashDrawer
CanApplyDiscount
CanOverridePrice
CanVoidItem
CanVoidOrder
CanRefund
CanReprintReceipt
CanAdjustStock
CanViewAuditLog
CanRunEndOfDay
```

## High-risk permissions

These must always be audited:

- Refund.
- Void.
- Price override.
- Discount.
- Cash drawer open.
- Tax setting change.
- Payment provider change.
- Device disable.
- User role change.
- Stock adjustment.
- Receipt reprint.
- Sync override.
- Manual payment correction.

---

# Device Security

Device identity and user identity must be separate.

## Device registration

A device registration should capture:

```text
DeviceId
TenantId
OrganisationId
LocationId
TerminalId
DeviceType
DeviceName
RegistrationTokenHash
Status
RegisteredAt
LastSeenAt
AppVersion
AssignedPrinters
AssignedPaymentTerminals
AssignedCustomerDisplay
```

## Device rules

- Device token should be stored locally on the client/device.
- Device token must be revocable.
- Server-side device config determines device role.
- Lost/stolen devices must be disableable.
- Device registration must be audited.
- Device re-assignment must be audited.
- Device tokens should be rotated where appropriate.
- Device activity should be logged.

## Device types

- Daxa Terminal
- Daxa Display
- Daxa KDS
- Daxa Back Office
- Daxa Kiosk
- Daxa Local Server
- Worker/service

---

# Network Security

## Venue network

Recommended:

- POS LAN separated from guest Wi-Fi.
- Local server wired via Ethernet.
- Receipt/kitchen printers on POS LAN only.
- Payment terminals on approved network path.
- Admin access restricted.
- Router/firewall controlled.
- UPS for local server and network switch where possible.
- No printer exposure to guest/public networks.

## Local server

Local server should:

- Use wired Ethernet.
- Use firewall rules.
- Use HTTPS where feasible.
- Run minimum required services.
- Store secrets securely.
- Restrict database access to local services.
- Have backups configured.
- Have health monitoring.
- Have update/rollback procedure.

## Cloud access

Cloud connections should:

- Prefer outbound-only from local venue where possible.
- Use TLS.
- Authenticate sync clients.
- Use idempotency and replay protection.
- Avoid exposing local server directly to the internet unless explicitly configured and secured.

---

# Cloud, Local, and Hybrid Security

## Daxa Cloud

Security concerns:

- Multi-tenant isolation.
- Cloud database access.
- Payment provider credentials.
- Admin portal access.
- Reporting access.
- Support access.
- Audit log protection.
- API rate limiting.
- Regional data hosting later.

## Daxa Local

Security concerns:

- Local server physical/network access.
- Local database backups.
- Device registration.
- Local admin credentials.
- Printer/cash drawer access.
- Offline operation.
- Local secret storage.
- Local update process.

## Daxa Hybrid

Security concerns:

- Sync authentication.
- Sync replay protection.
- Sync conflict handling.
- Cloud-to-local configuration trust.
- Local-to-cloud event trust.
- Backup/export security.
- Support diagnostics access.

---

# Payment Security

Daxa POS should not store card numbers, CVV, magnetic stripe data, or PINs.

Payment providers handle card processing.

Daxa stores payment references and transaction metadata required for order history, refund, audit, and reconciliation.

## Store

- Provider name.
- Provider payment ID.
- Provider checkout ID.
- Payment terminal ID.
- Merchant/account/location reference.
- Amount.
- Currency.
- Status.
- Timestamp.
- Card brand/masked display if provider returns it.
- Receipt URL if provider returns it.
- Refund references.
- Settlement/reconciliation references where available.

## Do not store

- Full card number.
- CVV.
- PIN.
- Track data.
- Raw card-present payloads.
- Sensitive provider secrets in plaintext.

## Provider credentials

Provider credentials must be:

- Stored securely.
- Encrypted at rest where possible.
- Restricted to required services.
- Rotatable.
- Audited when changed.
- Never logged.
- Never committed to source control.

---

# Gift Card and Store Credit Security

Gift cards and store credit may be implemented later, but the security model must be planned early.

## Gift card rules

- Use random high-entropy tokens.
- Do not use sequential codes.
- Do not store balance in QR code.
- QR code identifies token/account only.
- Balance is server-side.
- Redemptions must be audited.
- Adjustments must be audited.
- Lost token disable/reissue must be supported.
- Public balance checks must be rate-limited.
- Gift card balance cannot go negative unless explicitly allowed by configured rule.

## Store credit rules

- Store credit must be ledgered.
- Store credit adjustments require permission.
- Store credit redemptions must be auditable.
- Store credit balances must be reconstructable from ledger.

---

# Audit Logging

Audit logging is mandatory.

## Audit events

```text
Login/logout
Failed login
PIN login
Failed PIN attempt
Device registered
Device disabled
Device reassigned
Open drawer
Void item
Void order
Refund
Manual payment correction
Price override
Discount applied
Tax setting changed
Payment provider changed
Receipt reprinted
Order reopened
Staff role changed
Menu item changed
Product tax category changed
Stock adjusted
Cash counted
Terminal paired/unpaired
Sync conflict resolved
Support access started
Support access ended
```

## Audit fields

```text
AuditEventId
TenantId
OrganisationId
LocationId
TerminalId
DeviceId
UserId
EventType
EntityType
EntityId
BeforeValue
AfterValue
Reason
IpAddress
UserAgent
CreatedAt
CorrelationId
```

Audit logs must be append-only. Do not silently edit audit history.

---

# Data Protection

## Customer data

Customer data may include:

- Name.
- Email.
- Phone.
- Purchase history.
- Loyalty balance.
- Gift card link.
- Store credit balance.
- Digital receipt preferences.

Rules:

- Collect only required data.
- Restrict access by role.
- Avoid displaying unnecessary customer data at POS.
- Audit high-risk access where appropriate.
- Support deletion/anonymisation policy later.
- Do not expose customer data across tenants.
- Cross-location customer profile must be explicitly designed.

---

# Secrets and Configuration

Never commit secrets.

Sensitive configuration includes:

- Database passwords.
- JWT/signing keys.
- Keycloak/admin credentials.
- Payment provider API keys.
- Webhook secrets.
- Sync credentials.
- Cloud backup credentials.
- SMTP/SMS provider credentials.
- Object storage keys.

Secrets should be stored in:

- Cloud secret manager for cloud deployments.
- Local secure secret store or protected environment for local deployments.
- Docker secrets where appropriate.
- Encrypted configuration where appropriate.

---

# iPad, Android, Linux, and Windows Lockdown

## Windows

For Daxa Terminal:

- Borderless full-screen can provide a terminal feel.
- Windows Assigned Access / kiosk mode should be used for stronger lockdown.
- Local admin access should be restricted.
- Auto-start can be configured for POS terminals.
- Device should be dedicated where possible.

## iPad/iPhone

Use:

- Guided Access for simple deployments.
- MDM for commercial deployments.

Restrict:

- Settings access.
- App installation.
- Safari/general browsing where possible.
- Notifications.
- Device changes.
- Leaving the PWA.

## Android

Use:

- Android kiosk/lock task mode for managed devices.
- MDM for commercial deployments.

## Linux kiosk

Use:

- Dedicated kiosk user.
- Auto-login where appropriate.
- Chromium/Firefox kiosk mode.
- Restricted desktop/session.
- No general desktop access for public devices.

---

# Required Security Tests

Claude Code must add or maintain tests for:

- Tenant isolation.
- Location isolation.
- Role permissions.
- Staff PIN login.
- Failed PIN rate limiting where implemented.
- Device token validation.
- Disabled device denial.
- Payment provider secret not logged.
- Refund permission enforcement.
- Void permission enforcement.
- Price override permission enforcement.
- Cash drawer audit.
- Tax setting change audit.
- Payment provider change audit.
- Sync authentication.
- Duplicate sync/idempotency.
- Cross-location report access denial.

---

# Production Readiness Security Checklist

Before production use, verify:

- Tenant isolation tests pass.
- Location isolation tests pass.
- Payment credentials are protected.
- Secret management is documented.
- Admin roles are reviewed.
- Staff PIN rules are documented.
- Device disable flow works.
- Audit logging is enabled.
- Backup/restore security is documented.
- Cloud/local sync authentication is implemented.
- TLS is configured.
- Database access is restricted.
- Logs do not contain secrets.
- Payment provider integrations meet provider requirements.
- Support access process is documented and auditable.
