# OI-0003 — Local Server Reference Hardware

## Status

Open

## Area

Devices / Hardware / Deployment

## Summary

What is the minimum and recommended hardware specification for a Daxa Local server (on-premises deployment)?

## Context

Daxa Local requires a physical server running inside the venue's network. This server runs the Daxa API, PostgreSQL, Keycloak (or alternative identity provider), the sync service, and background workers — all via Docker Compose.

Without a defined hardware specification, customer support, deployment documentation, and hardware testing cannot be completed.

## Impact

- Affects Docker Compose resource configuration (memory limits, CPU limits).
- Affects Keycloak deployment decision (if Keycloak is too heavy for minimum spec, an alternative may be required).
- Affects deployment documentation for Daxa Local.
- Affects customer hardware purchasing guidance.

## Options

1. **Mini PC (Intel N100 or equivalent)** — 8GB RAM, 128GB SSD. Common in venue environments. Low power, fanless, reliable.
2. **Small form-factor PC (Intel i5/i7, 16GB RAM)** — More headroom for Keycloak and concurrent connections. Higher cost.
3. **Raspberry Pi 5** — Low cost, limited RAM (8GB max). PostgreSQL and Keycloak may be constrained.
4. **Venue-owned existing hardware** — Uncontrolled spec. Risk of under-powered hardware.
5. **Daxa-supplied appliance** — Controlled spec, easier support, higher upfront cost.

## Recommendation

Define a **minimum spec** of 8GB RAM / 4-core Intel/AMD x86-64 / 256GB SSD NVMe, and a **recommended spec** of 16GB RAM / 4-core / 512GB SSD. Raspberry Pi is not recommended for production Daxa Local due to ARM compatibility uncertainty with some Docker images and memory constraints.

## Decision Needed

- Minimum hardware spec.
- Recommended hardware spec.
- Whether Daxa supplies a reference device or lets venues supply their own.

## Related ADRs

- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](../../adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md)
- [ADR-0012 — Docker Local Deployment Strategy](../../adr/proposed/ADR-0012-docker-local-deployment-strategy.md)

## Related Documents

- [Deployment: Local](../../deployment/local.md)
- [Deployment: Docker](../../deployment/docker.md)
