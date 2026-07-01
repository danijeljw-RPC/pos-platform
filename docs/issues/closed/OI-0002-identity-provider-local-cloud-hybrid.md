# OI-0002 — Identity Provider for Local, Cloud, and Hybrid

## Status

Closed

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

- [ADR-0009 — Keycloak or Identity Provider Strategy](../../adr/superseded/ADR-0009-keycloak-or-identity-provider-strategy.md)

## Related Documents

- [OI-0010 — Local Keycloak vs Cloud Keycloak](OI-0010-local-keycloak-vs-cloud-keycloak.md)
- [Architecture: Security](../../architecture/security.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)

---

## Decision Addendum

OI-0002 is resolved.

Daxa POS will use the mixed authentication model defined by ADR-0013 — Cloud Identity and Local POS Authentication Strategy.

The original question asked which identity provider should be used across local, cloud, and hybrid deployment modes. That framing has been replaced by a more practical split between:

- Cloud/admin identity.
- Local POS runtime authentication.
- Local manager/admin authentication.
- Device identity.
- Application-level authorization.

Daxa POS does not require every local POS staff login to be handled by Keycloak or another full identity provider.

## Decision

Daxa POS will use different authentication mechanisms for different operating contexts, while keeping one consistent authorization and permission model inside the Daxa WebAPI.

| Context | Authentication method | Owner |
|---|---|---|
| Cloud admin / back office | Keycloak or equivalent identity provider | Cloud identity provider |
| Cloud employee self-service | Username/password via Keycloak or equivalent | Cloud identity provider |
| Local POS staff login | Staff ID + PIN on a trusted registered device | Daxa WebAPI |
| Local manager/admin login | Username/password through Daxa WebAPI for MVP | Daxa WebAPI |
| Device registration | Device registration PIN/token and server-issued device identity | Daxa WebAPI |
| Application permissions | Consistent permission checks across client, location, device, role, and staff context | Daxa WebAPI |

Keycloak remains useful for cloud-oriented identity where secure account login, password reset, MFA, SSO, account recovery, and tenant-level account management are required.

Keycloak is not required for normal local POS staff ID/PIN login.

## Staff PIN Login

Local POS staff authentication should be easy and fast.

A normal staff member using the POS terminal, KDS, or counter workflow should authenticate using:

```text
Staff ID: numeric employee/user ID
PIN: numeric PIN
```

This login is only valid on trusted registered devices and only for operational POS actions.

It must not be treated as a full account login for sensitive back-office functions.

Staff ID/PIN authentication may allow actions such as:

- Creating orders.
- Editing open orders.
- Sending orders to preparation stations.
- Taking payments.
- Clocking on/off, if implemented.
- Opening assigned cash drawers, if permitted.
- Performing low-risk operational actions granted by role.

Staff ID/PIN authentication must not be sufficient for:

- Editing tax configuration.
- Editing global or location-level payment provider settings.
- Editing users, roles, or permissions.
- Viewing payroll or sensitive employee data.
- Exporting financial data.
- Changing sync or deployment settings.
- Accessing cloud administration features.

## Manager and Admin Login

Managers, owners, administrators, accountants, and support users require stronger authentication for sensitive functions.

For MVP and local deployments, local username/password authentication through the Daxa WebAPI is acceptable for manager/admin use.

For cloud deployments, manager/admin login should use Keycloak or an equivalent identity provider.

For employee self-service features such as roster access, personal details, payslips, or other keyboard-based staff account functions, username/password authentication must be used instead of POS staff ID/PIN login.

## Authorization Rule

Authentication can vary by context, but authorization must remain consistent.

The Daxa WebAPI owns application-level authorization.

The permission system must check:

- Tenant/client.
- Organisation.
- Region/country, where configured.
- Location.
- Device.
- Staff/user identity.
- Role.
- Permission.
- Authentication method.

The application should normalise all successful authentication methods into a common authorization context before checking permissions.

Example internal model:

```csharp
public sealed record AuthContext(
    Guid ClientId,
    Guid? OrganisationId,
    Guid? LocationId,
    Guid? UserId,
    Guid? StaffMemberId,
    Guid? DeviceId,
    AuthMethod AuthMethod,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Permissions);

public enum AuthMethod
{
    CloudIdentityProvider,
    LocalUsernamePassword,
    LocalStaffPin,
    DeviceToken,
    SupportAccess
}
```

## Relationship to ADR-0013

ADR-0013 replaces ADR-0009 and provides the accepted identity strategy.

OI-0002 is closed by adopting ADR-0013.

The answer is not “Keycloak everywhere” and not “custom WebAPI authentication everywhere”.

The accepted model is:

```text
Cloud/admin identity = Keycloak or equivalent identity provider
Local POS runtime identity = Daxa WebAPI staff ID/PIN on trusted devices
Local manager/admin identity = Daxa WebAPI username/password for MVP
Authorization = Daxa WebAPI everywhere
```

## Consequences

This keeps local POS operation simple and resilient while preserving a proper cloud identity path for administrative and enterprise features.

It avoids forcing every waiter, bartender, cashier, or KDS user into a full OIDC identity-provider login.

It also avoids writing a full enterprise identity provider inside Daxa for cloud use.

The system remains flexible enough to support local Keycloak or another local identity provider later for larger enterprise deployments, but it is not required for MVP.

## Status Update

This open issue is resolved by ADR-0013.

Status: **Closed**
