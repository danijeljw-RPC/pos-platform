# OI-0010 — Local Keycloak vs Cloud Keycloak

## Status

Closed

## Area

Identity / Deployment

## Summary

Should Daxa Local (on-premises) deployments run their own local Keycloak instance, or should they authenticate against the Daxa Cloud Keycloak instance?

## Context

ADR-0009 proposes Keycloak as the identity provider. However, for Daxa Local deployments, there is a question of whether Keycloak runs locally (enabling offline authentication) or relies on the Daxa Cloud Keycloak (requiring internet access for authentication).

This is a significant architectural question because:
- If Keycloak is local, staff can log in even when internet is down.
- If Keycloak is cloud-only, internet loss could prevent staff from starting the POS (unacceptable for local deployments).
- Running Keycloak locally on small hardware adds memory and resource pressure.

## Impact

- Determines whether a local Keycloak instance is required for Daxa Local deployments.
- Affects minimum hardware specification (OI-0003).
- Affects offline resilience.
- Affects sync design (user accounts synced from cloud to local?).

## Options

1. **Cloud Keycloak only** — Simpler. Breaks auth on internet outage. Unacceptable for Daxa Local.
2. **Local Keycloak only** — Works offline. More resource-intensive. No central user management.
3. **Local Keycloak + cloud Keycloak federated** — Local Keycloak syncs users from cloud. Enables offline auth AND central management. Complex.
4. **Local lightweight identity (not Keycloak) for offline auth, Keycloak for admin** — Different providers for different roles. Complicates auth design.
5. **Cached session tokens (short-lived)** — Staff sessions are cached locally. Works for short outages. Eventually requires re-auth against cloud Keycloak. Pragmatic for hybrid.

## Recommendation

For **Daxa Local**: local Keycloak instance (within Docker Compose stack) with periodic user sync from cloud.
For **Daxa Hybrid**: hybrid Keycloak federation or cached session approach.
For **Daxa Cloud**: cloud Keycloak only.

This requires hardware to support local Keycloak — see OI-0003.

## Decision Needed

- Whether Daxa Local requires a local Keycloak instance.
- Whether user accounts are synced from cloud to local.
- Session cache strategy for short internet outages.

## Related ADRs

- [ADR-0009 — Keycloak or Identity Provider Strategy](../../adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md)
- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](../../adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md)

## Related Documents

- [OI-0002 — Identity Provider for Local, Cloud, Hybrid](OI-0002-identity-provider-local-cloud-hybrid.md)
- [OI-0003 — Local Server Reference Hardware](OI-0003-local-server-reference-hardware.md)
- [Architecture: Security](../../architecture/security.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)

---

## Decision Addendum

OI-0010 is resolved.

Daxa Local will not run local Keycloak by default for MVP.

Daxa Cloud may use Keycloak or an equivalent identity provider for cloud/admin login, but local POS runtime authentication will be handled by the Daxa WebAPI using trusted device identity plus staff ID/PIN.

This decision follows ADR-0013 — Cloud Identity and Local POS Authentication Strategy.

## Decision

The accepted model is:

```text
Daxa Cloud
    Cloud identity provider for admin/back-office login

Daxa Local
    Daxa WebAPI local staff ID/PIN login for POS runtime
    Daxa WebAPI username/password login for local manager/admin MVP use

Daxa Hybrid
    Local POS runtime remains local and offline-capable
    Cloud/admin identity remains available for cloud management
    Staff/users/permissions sync according to Daxa Sync rules
```

Local Keycloak is not required for normal local POS use.

Cloud Keycloak is not required for normal local staff PIN login.

## Why Cloud Keycloak Only Is Rejected

Cloud-only authentication is not acceptable for local POS runtime.

A local venue must still be able to trade if the internet connection fails.

If every staff login required a cloud identity provider, an internet outage could prevent staff from opening the POS or continuing normal operations.

That is not acceptable for Daxa Local or Daxa Hybrid.

## Why Local Keycloak Is Not Default

Local Keycloak provides strong identity-provider features, but it is heavy for small on-prem deployments and unnecessary for normal POS staff PIN login.

Normal staff POS login should be quick and simple:

```text
Staff ID + PIN + trusted registered device
```

That model is better handled directly by the Daxa WebAPI.

Local Keycloak may be supported later as an enterprise deployment option, but it is not required for MVP or standard local venues.

## Local POS Staff Authentication

Local POS staff authentication uses:

- Trusted registered device.
- Staff ID.
- Numeric PIN.
- Local staff/permission records.
- Short-lived POS staff session.

This authentication method is limited to operational POS workflows.

It must not be used for sensitive admin features such as tax configuration, user management, payroll-style employee data, cloud administration, or financial exports.

## Local Manager/Admin Authentication

For MVP, local manager/admin login may be handled by the Daxa WebAPI using username/password authentication.

This is suitable for:

- Local Back Office.
- Manager approvals.
- Local configuration.
- Local reporting.
- Location-level administrative functions.

Cloud-based administration and future enterprise deployments may use Keycloak or another identity provider.

## Hybrid Sync

Hybrid deployments may sync staff, users, roles, permissions, and location access from cloud to local.

The local server must retain enough identity and permission data to continue operating during internet outages.

When connectivity returns, local operational events and audit logs sync back to cloud according to the accepted sync principles and conflict handling rules.

## Session Cache Strategy

For local POS staff sessions, cached cloud identity tokens are not the primary offline-authentication mechanism.

The primary mechanism is local authentication against local staff/PIN data on a trusted registered device.

For cloud/admin users, cached browser sessions may exist, but sensitive cloud/admin actions should require valid authentication according to the active deployment mode and security policy.

## Optional Future Local Identity Provider

Daxa may later support local Keycloak or another local identity provider for enterprise customers that require:

- Local SSO.
- Local MFA.
- Local identity federation.
- Enterprise directory integration.
- More complex offline identity management.

That should be an advanced deployment option, not the default MVP path.

## Relationship to OI-0002 and ADR-0013

OI-0010 is closed by the same identity strategy used to close OI-0002.

ADR-0013 supersedes ADR-0009 and provides the accepted model:

```text
Cloud/admin identity = Keycloak or equivalent
Local POS runtime identity = Daxa WebAPI staff ID/PIN
Local manager/admin identity = Daxa WebAPI username/password for MVP
Authorization = Daxa WebAPI everywhere
```

## Consequences

This keeps Daxa Local lighter, simpler, and more resilient.

It avoids requiring local Keycloak on every small venue server.

It also avoids breaking local POS operation when internet access is unavailable.

The architecture remains open to local Keycloak later where enterprise customers need it.

## Status Update

This open issue is resolved by rejecting both cloud-only Keycloak for local POS and mandatory local Keycloak for MVP. Daxa Local uses Daxa WebAPI local authentication for POS runtime and local admin use, while cloud/admin identity can use Keycloak or an equivalent provider.

Status: **Closed**
