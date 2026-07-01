# OI-0008 — Cloud Data Region Strategy

## Status

Open

## Area

Architecture / Deployment

## Summary

Which cloud regions should Daxa Cloud use for AU/NZ launch, and what is the data residency and sovereignty strategy?

## Context

Daxa POS processes financial transactions including customer payment data. AU data privacy requirements (Privacy Act 1988) and NZ requirements (Privacy Act 2020) require careful consideration of where data is stored and processed.

Some venues may require that data does not leave Australia or New Zealand. Enterprise customers may have specific data residency requirements.

## Impact

- Determines which cloud provider and regions to use for AU/NZ launch.
- Affects pricing (cloud hosting costs vary by region).
- Affects latency for Daxa Local and Daxa Hybrid sync.
- Affects compliance documentation.
- Affects data backup and DR strategy.

## Options

1. **AWS ap-southeast-2 (Sydney)** — AU data residency. Major provider, good AU presence.
2. **Azure Australia East / Southeast (Sydney/Melbourne)** — AU data residency. Azure has strong enterprise compliance documentation.
3. **GCP australia-southeast1 (Sydney)** — AU data residency. Growing presence.
4. **Multi-region (AU + NZ)** — Separate NZ data in AWS ap-southeast-2 or a NZ-adjacent region. Complex but better data sovereignty for NZ customers.
5. **Single AU region for AU/NZ launch** — Simpler. NZ data stays in AU region initially. Revisit for NZ compliance requirements.

## Recommendation

**AWS ap-southeast-2 (Sydney)** for AU/NZ launch as a pragmatic starting point. Evaluate NZ-specific data residency requirements before NZ commercial launch.

## Decision Needed

- Cloud provider for initial deployment.
- Primary and secondary regions.
- Data residency commitment to customers.
- Backup/DR region strategy.

## Related ADRs

- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](../../adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0012 — Docker Local Deployment Strategy](../../adr/proposed/ADR-0012-docker-local-deployment-strategy.md)

## Related Documents

- [Deployment: Cloud](../../deployment/cloud.md)
- [PLAN-0008 — Testing, Security, Deployment](../../plans/active/PLAN-0008-testing-security-deployment-planning.md)

---

## Decision Addendum

OI-0008 is resolved.

Daxa Cloud will use a configurable, region-aware data hosting strategy.

The platform must not hard-code an AU/NZ-only data region model. Daxa POS is designed to support cloud, local, and hybrid deployments across multiple countries and regions.

## Decision

Daxa Cloud will support region-based deployment groups.

Each tenant/client must be assigned to a primary data region when the customer is provisioned.

The primary data region determines where the customer's cloud-hosted operational data is stored and processed.

Example region groups:

```text
APAC
Australia
New Zealand
North America
Europe
United Kingdom
Singapore
Other future regions
```

The exact cloud provider and exact physical cloud region can be selected per deployment phase, customer requirement, and commercial readiness.

## Initial Launch Direction

For the initial commercial launch, Daxa may use a single primary cloud region suitable for the first target customers.

For an Australia-first launch, an Australia-hosted primary region is acceptable.

However, the system design must not assume that all tenants are permanently hosted in Australia or New Zealand.

The data region must be a tenant/client provisioning setting, not a hard-coded application constant.

## Region-Agnostic Rule

The platform must not contain logic such as:

```text
if country == "AU" then use region X
if country == "NZ" then use region Y
```

Instead, customer/tenant provisioning should define:

```text
TenantId
PrimaryDataRegion
BackupRegion
AllowedStorageRegions
DataResidencyPolicy
DeploymentMode
```

This lets Daxa support different countries, customers, and enterprise requirements without rewriting core platform logic.

## Local and Hybrid Considerations

Daxa Local remains locally hosted on the customer's onsite server.

Daxa Hybrid uses local processing for venue operations and syncs selected data to the assigned cloud region.

For hybrid deployments:

- The local venue remains operational if internet drops.
- Cloud sync targets the tenant's configured cloud region.
- Cloud reporting, backup, monitoring, and central management occur in the tenant's configured region.
- Multi-location customers may require locations in different countries or regions later.

## Data Residency Commitment

Daxa should not make a global legal data-residency commitment in code.

Instead, Daxa should provide configurable hosting commitments by customer/tenant contract and deployment region.

Customer-facing documentation should state the customer's selected hosting region and any backup/DR region used.

The platform should be able to support region-specific contractual commitments such as:

```text
Primary data stored in Region A
Backups stored in Region A or approved Region B
Support access audited
Cross-region replication disabled unless contracted
```

## Backup and Disaster Recovery

The backup and DR strategy must be tied to the tenant's configured data region.

For MVP, a simple region-local backup approach is acceptable.

Future production deployments should support:

- Primary region.
- Backup region.
- Point-in-time restore.
- Encrypted backups.
- Tenant-level restore boundaries.
- Clear data export and deletion process.
- Audited support access.

Cross-region backups or replication must be configurable and documented.

## Payment and Sensitive Data

Daxa POS should avoid storing raw card data.

Integrated payment providers should return tokens, transaction IDs, status, and approved/declined/cancelled outcomes rather than raw payment-card details.

Payment provider data residency may depend on the selected payment provider and their own infrastructure.

Daxa should document what is stored in Daxa Cloud versus what remains with the payment provider.

## Multi-Location Rule

Multi-location is supported by default.

A tenant may have one location or many locations.

For MVP, a tenant should normally have a single primary cloud data region.

Future enterprise support may allow region-specific tenant partitioning if a single organisation operates locations across multiple legal/data-residency regions.

## Consequences

This decision keeps Daxa POS cloud deployment flexible and internationally viable.

It allows an Australia-first deployment without making Australia or New Zealand assumptions permanent in the architecture.

It also supports future expansion into North America, Europe, Singapore, and other regions by provisioning customers into the appropriate data region.

## Status Update

This open issue is resolved by adopting a configurable, tenant-assigned cloud data region strategy.

Status: **Closed**
