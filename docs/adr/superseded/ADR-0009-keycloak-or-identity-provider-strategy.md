# ADR-0009 — Keycloak or Identity Provider Strategy

## Status

Superseded

## Context

Daxa POS needs a robust identity and access management solution that supports:

- Multi-tenant isolation.
- Role-based and permission-based access control.
- Staff PIN login for fast counter POS use.
- Admin web login (email/password or SSO).
- Support access with auditable entry.
- Cloud, local, and hybrid deployment modes.

Building a custom identity provider from scratch is a significant security risk. Using a well-supported identity platform reduces risk and speeds development.

Keycloak is a leading open-source identity provider that supports multi-tenancy (via realms), OIDC, SAML, RBAC, and self-hosting — making it a candidate for both cloud and local deployments.

## Decision

Daxa POS will use **Keycloak** (or an equivalent identity platform) for identity and access management.

- Cloud deployment uses a Daxa-managed Keycloak instance.
- Local deployment uses a locally-hosted Keycloak instance or a lightweight alternative that can run on-premises.
- Hybrid deployment may share a Keycloak instance between local and cloud or use a mirrored/federated configuration.
- Staff PIN login is handled via a short-lived token exchange or a custom Keycloak extension, not raw password storage in the application database.

This decision is **proposed** and requires further evaluation before acceptance (see Open Questions).

## Consequences

**Positive:**
- Battle-tested identity platform with extensive community support.
- Multi-tenant (realm per tenant, or shared realm with tenant isolation).
- Supports OIDC for web/PWA and native MAUI app authentication.
- Admin console for user management.
- Auditable login events.

**Negative:**
- Keycloak is a heavy runtime for small local deployments.
- PIN login for POS staff requires customisation.
- Local Keycloak on-premises adds operational complexity.
- Version upgrades require care.

## Alternatives Considered

1. **Custom identity service** — Rejected for security risk and time cost.
2. **Auth0 / Okta** — Viable for cloud-only. Less suitable for on-premises (Hybrid/Local modes).
3. **ASP.NET Core Identity (standalone)** — Viable for simple cases but lacks multi-tenant and enterprise IAM features.
4. **Microsoft Entra ID / Azure AD B2C** — Viable for cloud and enterprise customers but may not run locally.

## Open Questions

- See [OI-0002 — Identity Provider for Local, Cloud, Hybrid](../../issues/closed/OI-0002-identity-provider-local-cloud-hybrid.md)
- See [OI-0010 — Local Keycloak vs Cloud Keycloak](../../issues/closed/OI-0010-local-keycloak-vs-cloud-keycloak.md)
- Can staff PIN login be implemented cleanly within a Keycloak flow?
- What is the minimum spec for a local Keycloak deployment?

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](ADR-0008-device-identity-vs-user-identity.md)
- [Architecture: Security](../../architecture/security.md)
- [Architecture: Tenancy](../../architecture/tenancy.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)

---

## Superseded By

ADR-0009 is superseded by ADR-0013 — Cloud Identity and Local POS Authentication Strategy.

ADR-0009 asked whether Daxa POS should use Keycloak or another identity provider as a broad system-wide identity strategy.

That framing was replaced because Daxa POS requires a mixed authentication model:

- Cloud/admin identity uses Keycloak or an equivalent identity provider.
- Local POS staff authentication uses trusted device identity plus staff ID/PIN.
- Local manager/admin authentication may use username/password through the Daxa WebAPI for MVP simplicity.
- The Daxa WebAPI owns application-level authorization across all authentication methods.

Status: **Superseded**
