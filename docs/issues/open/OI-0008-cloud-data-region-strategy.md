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
