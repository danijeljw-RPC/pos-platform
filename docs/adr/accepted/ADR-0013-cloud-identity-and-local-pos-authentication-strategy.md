# ADR-0013 — Cloud Identity and Local POS Authentication Strategy

## Status

Accepted

## Supersedes

- [ADR-0009 — Keycloak or Identity Provider Strategy](ADR-0009-keycloak-or-identity-provider-strategy.md)

## Context

ADR-0009 considered whether Daxa POS should use Keycloak, or another identity provider, as the identity provider strategy across cloud, local, and hybrid deployments.

That framing is too broad for the actual POS operating model.

Daxa POS has multiple authentication use cases with different usability and security requirements:

- Local POS staff need fast login and fast user switching at terminals.
- Local POS staff login must continue to work during internet outages.
- POS staff commonly use numeric staff IDs and numeric PINs, not full keyboard-based username/password login.
- Managers and administrators need stronger login for sensitive actions.
- Cloud/admin users need secure account login, password reset, MFA, SSO, and support access controls.
- Employee self-service features such as roster access, payroll/payslip access, and personal-data updates should use normal username/password authentication.
- Devices have their own identity and registration process, separate from user identity.
- The WebAPI must apply consistent permissions regardless of how the user authenticated.

Using Keycloak for every authentication flow would add unnecessary complexity to local POS staff login. However, rejecting Keycloak entirely would remove useful cloud/admin identity features such as MFA, SSO, enterprise identity integration, and mature password/account management.

The product therefore needs a mixed authentication strategy rather than a single identity-provider-only strategy.

## Decision

Daxa POS will use a **mixed authentication strategy**.

Cloud-facing and administration-facing identity will use **Keycloak or an equivalent identity provider** where secure username/password login, MFA, SSO, account recovery, support access, and tenant-level account management are required.

Local POS runtime authentication will be handled by the **Daxa WebAPI** using:

- trusted registered device identity;
- staff ID;
- numeric PIN;
- location context;
- short-lived POS staff sessions.

Local manager and administrator authentication may be handled by the Daxa WebAPI using username/password authentication for MVP and smaller on-prem deployments.

Employee self-service features such as roster access, payroll or payslip access, personal details, and back-office access must use username/password authentication, not staff ID/PIN login.

The Daxa WebAPI remains responsible for application-level authorization across all authentication methods.

## Authentication Model

| Use case | Primary authentication method | Identity owner | Notes |
| --- | --- | --- | --- |
| Cloud admin login | Username/password, MFA later, SSO later | Keycloak or equivalent identity provider | Used for cloud portal, tenant admin, support access, and enterprise login. |
| Cloud employee self-service | Username/password | Keycloak or equivalent identity provider | Used where the user has keyboard access and is accessing personal/admin data. |
| Local POS staff login | Staff ID + PIN on trusted device | Daxa WebAPI | Used for fast POS operation and staff switching. |
| Local manager POS approval | Manager PIN or username/password depending on action risk | Daxa WebAPI | Low-risk overrides may use manager PIN. Sensitive actions require username/password. |
| Local admin portal login | Username/password | Daxa WebAPI for MVP; optional local identity provider later | Keeps local deployments simple. |
| Device registration | Device registration PIN/token | Daxa WebAPI | Covered by ADR-0008. Device identity is separate from user identity. |
| Hybrid deployment | Local runtime auth with cloud sync of users/permissions where applicable | Daxa WebAPI plus cloud identity | Local POS must continue during internet outage. |

## Authorization Model

Authentication may vary by deployment mode and use case.

Authorization must remain consistent.

The Daxa WebAPI owns application-level authorization and permission checks, including:

- client access;
- location access;
- device access;
- staff role;
- manager/admin role;
- POS permissions;
- catalogue permissions;
- tax configuration permissions;
- refund/void permissions;
- reporting permissions;
- employee self-service permissions;
- support access permissions.

All authenticated contexts must be normalised into a common internal authorization context.

Example conceptual model:

```text
AuthContext
- ClientId
- LocationId
- UserId
- StaffMemberId
- DeviceId
- AuthMethod
- Roles
- Permissions
```

Example authentication methods:

```text
CloudIdentityProvider
LocalUsernamePassword
LocalStaffPin
DeviceToken
SupportAccess
```

The application should check permissions against the normalised authorization context, not against the original authentication mechanism.

## Staff ID/PIN Login Rules

Staff ID/PIN login is intended for local POS operational use only.

It may be used for:

- creating orders;
- editing open orders;
- sending orders to kitchen/bar/coffee stations;
- taking payments;
- clock-on/clock-off where enabled;
- opening assigned POS functions;
- low-risk manager-approved actions where configured.

It must not be used for:

- editing tax configuration;
- editing payment provider settings;
- editing user permissions;
- accessing payroll or payslips;
- accessing employee personal details;
- changing security settings;
- exporting financial reports;
- performing support/admin access;
- accessing cloud administration features.

Staff ID/PIN login must require a trusted registered device context. A staff ID/PIN alone is not enough to authenticate against the broader system.

## Manager and Administrator Login Rules

Manager approval can support different authentication strength depending on the risk of the action.

| Action type | Acceptable authentication |
| --- | --- |
| Void unsent item | Manager PIN may be sufficient. |
| Apply discount | Manager PIN may be sufficient. |
| Open cash drawer override | Manager PIN may be sufficient, with audit logging. |
| Refund paid order | Username/password preferred; manager PIN may be configurable for low-risk venues. |
| Edit catalogue | Username/password required. |
| Edit tax configuration | Username/password required. |
| Manage users/roles/permissions | Username/password required. |
| View payroll/payslips/personal staff data | Username/password required. |
| Export financial reports | Username/password required. |
| Change payment provider settings | Username/password required. |

All manager approvals must be audit logged against:

- the staff member performing the original action;
- the approving manager/admin;
- the device;
- the location;
- the order/payment/report/configuration object affected;
- the date/time of approval.

## Cloud Identity Provider Usage

Keycloak, or an equivalent identity provider, is valuable for cloud and administration use cases.

The cloud identity provider should support:

- username/password login;
- secure password storage;
- password reset;
- MFA later;
- SSO later;
- tenant/client separation;
- support/admin access controls;
- account lockout policy;
- token issuing;
- audit events;
- integration with enterprise identity providers later.

The cloud identity provider is not required for normal local POS staff ID/PIN login.

## Local Deployment Behaviour

Local/on-prem deployments must be able to operate without internet access.

For MVP, local deployments may use Daxa WebAPI-managed authentication for:

- local POS staff ID/PIN login;
- local manager/admin username/password login;
- local device registration;
- local permission checks.

This avoids requiring every local deployment to run Keycloak.

Larger or enterprise local deployments may later support local Keycloak or another local identity provider where required.

## Hybrid Deployment Behaviour

Hybrid deployments should use local runtime authentication for POS operation.

Cloud identity may remain the source of truth for centrally managed users, but the local system must hold enough synced user, staff, role, and permission data to continue trading during internet outages.

Hybrid sync should support:

- syncing cloud-created users/staff to local systems;
- syncing role and permission assignments;
- syncing client/location access;
- disabling users locally after cloud disable sync;
- local emergency disable where required;
- audit logging of identity and permission changes;
- conflict handling consistent with ADR-0007 and OI-0006.

## Device Identity Relationship

Device identity is separate from user identity, as defined in ADR-0008.

A trusted device provides the operating context for local POS authentication.

A staff ID/PIN login is only valid within a trusted device and location context.

A device registration PIN/token is not a staff login credential and must not grant user permissions by itself.

## Questions and Answers

### Should Daxa POS use Keycloak everywhere?

No.

Keycloak is useful for cloud/admin identity, but it should not be used for every local POS staff login.

Normal POS staff login should be fast, numeric, local, and offline-capable. That is better handled by the Daxa WebAPI using trusted device identity plus staff ID/PIN.

### Should cloud/admin users use Keycloak?

Yes.

Cloud/admin users should use Keycloak or an equivalent identity provider because this area benefits from mature security features such as password reset, MFA, SSO, enterprise identity integration, token issuing, support access, and account auditing.

### Should local POS staff use username/password?

No, not for normal POS operation.

Local POS staff should use staff ID and PIN on a trusted registered device.

This supports fast login, fast staff switching, and offline local trading.

### Should managers use staff ID/PIN or username/password?

Both can exist, but they must be used differently.

Manager PIN may be acceptable for low-risk POS overrides, such as discounts or voiding an unsent item.

Username/password should be required for sensitive actions such as editing tax configuration, editing catalogue data, changing user permissions, viewing payroll, exporting financial reports, or changing payment provider settings.

### Should employee self-service use staff ID/PIN?

No.

Employee self-service features such as rosters, payslips, personal information, and back-office access should use username/password authentication.

These are not fast POS terminal actions and may expose personal or employment-related information.

### Should local/on-prem deployments run Keycloak?

Not by default for MVP.

Local deployments should be simple to install and operate. Requiring Keycloak locally adds operational complexity and hardware requirements.

The MVP local deployment can use Daxa WebAPI-managed username/password authentication for managers/admins and staff ID/PIN authentication for POS users.

Local Keycloak can remain a future option for larger or enterprise deployments.

### Should hybrid deployments depend on cloud identity at runtime?

No.

Hybrid deployments must continue operating during internet outages.

The local system must retain enough user, staff, role, permission, and device data to authenticate locally and continue trading.

Cloud identity can remain the central source for account management, but local POS runtime authentication must not require live cloud access.

### Who owns authorization?

The Daxa WebAPI owns authorization.

The login mechanism proves who or what is acting. The WebAPI decides what that actor can do within a client, location, device, order, payment, report, or configuration context.

### Should authentication and authorization be separated?

Yes.

Authentication may be performed by Keycloak, local username/password, staff ID/PIN, device token, or support access.

Authorization must be applied consistently by the Daxa WebAPI using roles, permissions, client access, location access, device context, and staff/user context.

### How does this replace ADR-0009?

ADR-0009 framed the decision as whether to use Keycloak or another identity provider.

ADR-0013 replaces that with a more accurate mixed strategy:

- Keycloak or equivalent identity provider for cloud/admin authentication.
- Daxa WebAPI staff ID/PIN authentication for local POS runtime.
- Daxa WebAPI username/password authentication for local manager/admin MVP use.
- Consistent WebAPI-owned authorization across all modes.

## Consequences

### Positive

- Better fit for hospitality POS workflows.
- Fast local POS staff login.
- Local trading can continue during internet outages.
- Cloud/admin security remains strong.
- Keycloak is used where it provides value, not where it creates friction.
- The WebAPI has one consistent authorization model.
- Device identity and user identity remain cleanly separated.
- Future enterprise identity features remain possible.

### Negative

- More than one authentication method must be implemented.
- The WebAPI must normalise different authentication methods into one authorization context.
- Local username/password authentication adds some security responsibility to Daxa POS.
- Staff PIN login must be carefully limited to operational POS use only.
- Sync between cloud identity and local runtime identity must be carefully designed for hybrid deployments.

## Alternatives Considered

### Use Keycloak for all authentication

Rejected.

This is too heavy and awkward for local POS staff ID/PIN login. It also makes offline/local operation more complex than necessary.

### Use only custom Daxa WebAPI authentication everywhere

Rejected.

This would simplify local operation but would lose mature cloud identity features such as MFA, SSO, account recovery, enterprise identity integration, and mature token/account management.

### Use cloud-only Keycloak and require internet for login

Rejected.

Local and hybrid POS deployments must continue operating during internet outages.

### Use local Keycloak for every on-prem deployment

Rejected for MVP.

This may be suitable for larger enterprise customers later, but it adds unnecessary install/runtime complexity to smaller local deployments.

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](ADR-0008-device-identity-vs-user-identity.md)
- [ADR-0009 — Keycloak or Identity Provider Strategy](ADR-0009-keycloak-or-identity-provider-strategy.md)
- [ADR-0007 — Local/Hybrid Sync Principles](ADR-0007-local-hybrid-sync-principles.md)
- [OI-0002 — Identity Provider for Local, Cloud, Hybrid](../../issues/open/OI-0002-identity-provider-local-cloud-hybrid.md)
- [OI-0010 — Local Keycloak vs Cloud Keycloak](../../issues/open/OI-0010-local-keycloak-vs-cloud-keycloak.md)
- [OI-0006 — Hybrid Sync Conflict Rules](../../issues/open/OI-0006-hybrid-sync-conflict-rules.md)
