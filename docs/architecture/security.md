# Security Architecture — Daxa POS

## Identity

Daxa POS uses Keycloak (or an equivalent, per ADR-0009) for identity and access management.

- Staff PIN login: short-lived token for fast counter access.
- Admin login: OIDC (email + password or SSO).
- Device registration: separate device token, not tied to a user session.
- Support access: separate Daxa-staff admin realm, all access audited.

## Authentication

- All API calls require a valid JWT bearer token.
- Tokens are issued by Keycloak.
- Token validation is performed in ASP.NET Core middleware.
- Token claims include: `tenant_id`, `organisation_id`, `location_ids`, `roles`, `user_id`.

## Authorisation

- Role-based access control (RBAC) enforced in API middleware.
- Permissions are checked per endpoint.
- Roles include: `SystemAdmin`, `OrganisationOwner`, `VenueManager`, `Staff`, `SupportAccess`.
- Financial operations (refunds, voids, discounts) require elevated roles.

## Tenant Isolation

- EF Core global query filters enforce tenant boundary on every database query.
- No cross-tenant data access is permitted via API.
- Tenant ID is extracted from the JWT, not from a URL parameter.

## Payment Security

- Payment provider credentials are stored encrypted.
- Provider credentials are never returned in API responses.
- Provider credentials are never logged.
- Integrated payment amounts are sent from the POS to the terminal — staff do not type amounts manually.

## Audit

All security-significant events are written to the audit log:

- Login (success and failure).
- PIN login.
- Device registration.
- Support access.
- Refunds, voids, discounts, manual overrides.
- Tax configuration changes.
- User and role changes.
- Receipt reprints.

## Open Questions

- See [OI-0002 — Identity Provider](../issues/open/OI-0002-identity-provider-local-cloud-hybrid.md)
- See [OI-0010 — Local Keycloak vs Cloud Keycloak](../issues/open/OI-0010-local-keycloak-vs-cloud-keycloak.md)

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](../adr/proposed/ADR-0008-device-identity-vs-user-identity.md)
- [ADR-0009 — Keycloak or Identity Provider Strategy](../adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/proposed/ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Tenancy](tenancy.md)
- [Module: Audit](../modules/audit.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
