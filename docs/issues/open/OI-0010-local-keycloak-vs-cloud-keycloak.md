# OI-0010 — Local Keycloak vs Cloud Keycloak

## Status

Open

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
