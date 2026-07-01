# OI-0003 — Local Server Reference Hardware

## Status

Closed

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

---

## Answer / Decision Input (2026-06-30)

The Daxa Local server should use a published hardware capability baseline rather than require one specific vendor device.

### Minimum Hardware

- 8GB RAM.
- 4-core Intel/AMD x86-64 CPU.
- 256GB NVMe SSD.
- Wired Ethernet strongly preferred.
- Capable of running Linux and Docker Compose reliably.
- Suitable for always-on venue operation.

### Recommended Hardware

- 16GB RAM.
- 4-core Intel/AMD x86-64 CPU.
- 512GB NVMe SSD.
- Wired Ethernet.
- Business/industrial mini PC or small form-factor PC.
- Suitable thermal profile for always-on operation inside a venue.

### Production Position

Raspberry Pi / ARM hardware is not recommended for production Daxa Local deployments at this stage. It may be useful for experimentation, but it adds compatibility and support risk for Docker images, Keycloak/identity services, PostgreSQL performance, and long-running venue operation.

Venue-owned existing hardware may be allowed only if it satisfies the published minimum specification. Unsupported or under-powered hardware should be rejected for production support.

### Reference Device Strategy

Daxa should initially publish minimum and recommended specifications rather than lock the product to one exact model.

Daxa may later publish a tested reference device list or offer a Daxa-supplied appliance once real-world deployment testing has identified reliable, available hardware models.

The preferred first production path is:

1. Define the hardware capability baseline.
2. Test on at least one Intel/AMD x86-64 mini PC.
3. Document the tested model as a known-good reference.
4. Allow customer-supplied hardware only where it meets or exceeds the minimum specification.
5. Consider a Daxa-supplied appliance after deployment and support requirements are clearer.

### Decision Summary

- Minimum spec: 8GB RAM / 4-core x86-64 / 256GB NVMe SSD.
- Recommended spec: 16GB RAM / 4-core x86-64 / 512GB NVMe SSD.
- Production architecture should assume Linux + Docker Compose on x86-64 hardware.
- Raspberry Pi / ARM is not a production target unless separately validated later.
- Daxa does not need to mandate a single hardware model at this stage.
