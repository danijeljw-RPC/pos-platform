# Security Architecture — Daxa POS

## Identity and Authentication

Daxa POS uses a mixed authentication strategy, as defined in [ADR-0013](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md). ADR-0009 (single Keycloak strategy) is superseded.

| Use case | Authentication method | Owner |
|---|---|---|
| Cloud admin / back office | Keycloak or equivalent identity provider | Cloud identity provider |
| Cloud employee self-service | Username/password via Keycloak or equivalent | Cloud identity provider |
| Local POS staff login | Staff ID + PIN on trusted registered device | Daxa WebAPI |
| Local manager/admin login | Username/password through Daxa WebAPI (MVP) | Daxa WebAPI |
| Device registration | Device registration PIN/token | Daxa WebAPI |

Local POS staff authentication must continue to work during internet outages. Local Keycloak is not required for MVP.

See [ADR-0013](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) for the full authentication model.

---

## Staff PIN Login Rules

Staff ID/PIN login is for local POS operational use only.

Permitted actions:

- Creating orders.
- Editing open orders.
- Sending orders to preparation stations.
- Taking payments.
- Clock-on/clock-off where enabled.
- Low-risk manager-approved operational actions.

Not permitted:

- Editing tax configuration.
- Editing payment provider settings.
- Editing users, roles, or permissions.
- Accessing payroll or employee personal data.
- Exporting financial data or reports.
- Accessing cloud administration features.

---

## Authorization

Authentication method may vary by deployment mode. Authorization must remain consistent.

The Daxa WebAPI owns application-level authorization.

All successful authentication methods are normalised into a common authorization context:

```text
AuthContext
- ClientId
- OrganisationId
- LocationId
- UserId
- StaffMemberId
- DeviceId
- AuthMethod (CloudIdentityProvider / LocalUsernamePassword / LocalStaffPin / DeviceToken / SupportAccess)
- Roles
- Permissions
```

Roles include: `SystemAdmin`, `OrganisationOwner`, `VenueManager`, `Staff`, `SupportAccess`.

Financial operations (refunds, voids, discounts) require elevated roles.

---

## Tenant Isolation

- EF Core global query filters enforce tenant boundary on every database query.
- No cross-tenant data access is permitted via API.
- Tenant ID is extracted from the JWT or authenticated session, not from a URL parameter.

---

## Payment Security

- Payment provider credentials are stored encrypted.
- Provider credentials are never returned in API responses.
- Provider credentials are never logged.
- Integrated payment amounts are sent from the POS to the terminal — staff do not type amounts manually.
- Daxa POS does not store raw card data.

---

## Audit

All security-significant events are written to the audit log:

- Login (success and failure) — all authentication methods.
- Device registration and deregistration.
- Support access.
- Refunds, voids, discounts, and manual overrides.
- Tax configuration changes.
- User, role, and permission changes.
- Receipt reprints.
- Manager approval actions.

---

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)
- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Tenancy](tenancy.md)
- [Module: Audit](../modules/audit.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
