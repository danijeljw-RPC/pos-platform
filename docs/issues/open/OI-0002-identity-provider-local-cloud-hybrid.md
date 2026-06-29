# OI-0002 — Identity Provider for Local, Cloud, and Hybrid

## Status

Open

## Area

Identity / Security

## Summary

Which identity provider should Daxa POS use, and how should it be deployed across cloud, local, and hybrid modes?

## Context

Daxa POS needs a robust IAM solution supporting multi-tenant isolation, RBAC, OIDC, staff PIN login, and admin login. ADR-0009 proposes Keycloak as the preferred option.

The challenge is: Keycloak is heavy for on-premises (Daxa Local) deployments on small hardware. Alternative lightweight options exist but may not support all required features.

## Impact

- Determines the identity runtime across all deployment modes.
- Affects local server hardware requirements (Keycloak is memory-intensive).
- Affects staff PIN login implementation.
- Affects multi-tenant isolation model.

## Options

1. **Keycloak (self-hosted in Docker)** — Full-featured, supports multi-tenancy via realms, OIDC, SAML, RBAC. Heavy for small hardware.
2. **Auth0 / Okta** — Managed cloud identity. No on-premises option without special enterprise plans.
3. **ASP.NET Core Identity** — Lightweight, custom. No enterprise features out of the box. Requires significant custom work.
4. **Microsoft Entra ID / Azure AD B2C** — Enterprise-grade. Cloud-only. May not fit local deployments.
5. **Duende IdentityServer** — Open-source, self-hosted, OIDC. Lighter than Keycloak. Commercial licence for production.

## Recommendation

**Keycloak** for cloud and hybrid deployments, with evaluation of a lighter alternative (Duende IdentityServer or custom ASP.NET Core Identity) for Daxa Local deployments on minimum-spec hardware.

## Decision Needed

- Which identity provider to use.
- Whether one provider serves all deployment modes or separate providers per mode.
- How staff PIN login is implemented within the chosen provider.

## Related ADRs

- [ADR-0009 — Keycloak or Identity Provider Strategy](../../adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md)

## Related Documents

- [OI-0010 — Local Keycloak vs Cloud Keycloak](OI-0010-local-keycloak-vs-cloud-keycloak.md)
- [Architecture: Security](../../architecture/security.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
