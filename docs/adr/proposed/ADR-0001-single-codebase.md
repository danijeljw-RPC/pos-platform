# ADR-0001 — Single Codebase for All Deployment Modes

## Status

Proposed

## Context

Daxa POS must support cloud, local (on-premises), and hybrid deployments. Early in the product's design, a decision was needed on whether to maintain separate codebases for each deployment mode or to use one shared codebase controlled by configuration and infrastructure.

Separate codebases would allow simpler per-mode specialisation but would immediately create divergence, increased maintenance burden, inconsistent feature delivery, and fragmentation of the product identity.

## Decision

Daxa POS will use a **single codebase** for all deployment modes.

- Daxa Cloud, Daxa Local, and Daxa Hybrid are deployment modes, not separate products.
- Deployment mode differences are handled through configuration, environment variables, infrastructure setup, and feature flags.
- All tenants, regardless of deployment mode, share the same domain model, API contracts, and data schema.
- Differences in behaviour between modes (e.g. local server vs. cloud API endpoint) are abstracted through configuration and service registrations, not separate source trees.

## Consequences

**Positive:**
- All deployment modes receive the same features when they are ready.
- Bug fixes and improvements apply across all modes simultaneously.
- Single test suite covers all modes.
- Product identity remains unified.
- Reduces long-term maintenance cost.

**Negative:**
- Configuration and environment handling becomes more complex.
- Engineers must be careful not to introduce mode-specific hard-coding.
- Some deployment-mode-specific behaviour must be carefully tested per mode.

## Alternatives Considered

1. **Separate codebases per deployment mode** — Rejected. Creates divergence, duplication, and inconsistency.
2. **Monorepo with separate apps per mode** — Partially considered. Some code sharing is possible but still encourages divergence of core domain logic.

## Open Questions

- How will deployment mode be communicated to the application at runtime? (Environment variable? Configuration file? Registration mechanism?)
- Will a single Docker image be used for all modes, or will separate images be produced from the same source?

## Related Documents

- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](ADR-0002-cloud-local-hybrid-deployment.md)
- [PLAN-0002 — Platform Skeleton](../plans/active/PLAN-0002-platform-skeleton.md)
- [OI-0003 — Local Server Reference Hardware](../issues/open/OI-0003-local-server-reference-hardware.md)
