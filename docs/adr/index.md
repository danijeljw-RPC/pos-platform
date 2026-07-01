# ADR Index — Daxa POS

Architecture Decision Records for Daxa POS.

---

## Accepted

| ADR | Title | Area |
|-----|-------|------|
| [ADR-0001](accepted/ADR-0001-single-codebase.md) | Single Codebase for All Deployment Modes | Architecture |
| [ADR-0002](accepted/ADR-0002-cloud-local-hybrid-deployment.md) | Cloud, Local, and Hybrid Deployment Modes | Architecture / Deployment |
| [ADR-0003](accepted/ADR-0003-multi-location-by-default.md) | Multi-Location by Default | Architecture / Tenancy |
| [ADR-0004](accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md) | Windows MAUI and PWA Device Strategy | Devices / Platform |
| [ADR-0005](accepted/ADR-0005-payment-provider-adapter-architecture.md) | Payment Provider Adapter Architecture | Payments |
| [ADR-0006](accepted/ADR-0006-tax-line-based-tax-engine.md) | Tax-Line Based Tax Engine | Tax |
| [ADR-0007](accepted/ADR-0007-local-hybrid-sync-principles.md) | Local/Hybrid Sync Principles | Sync / Offline |
| [ADR-0008](accepted/ADR-0008-device-identity-vs-user-identity.md) | Device Identity vs User Identity | Identity / Devices |
| [ADR-0010](accepted/ADR-0010-financial-records-ledger-and-audit.md) | Financial Records, Ledger, and Audit | Payments / Audit |
| [ADR-0011](accepted/ADR-0011-receipt-tax-marker-strategy.md) | Receipt Tax Marker Strategy | Tax / Receipts |
| [ADR-0012](accepted/ADR-0012-docker-local-deployment-strategy.md) | Docker and Docker Compose Local Deployment Strategy | Deployment / Infrastructure |
| [ADR-0013](accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) | Cloud Identity and Local POS Authentication Strategy | Identity / Security |

---

## Proposed

| ADR | Title | Area |
|-----|-------|------|
| [ADR-0014](proposed/ADR-0014-inter-module-communication.md) | Inter-Module Communication Pattern | Architecture |

Requires human review and acceptance before PLAN-0002 module scaffolding relies on it.

---

## Superseded

| ADR | Title | Superseded By |
|-----|-------|---------------|
| [ADR-0009](superseded/ADR-0009-keycloak-or-identity-provider-strategy.md) | Keycloak or Identity Provider Strategy | [ADR-0013](accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) |

---

## Rejected

None.

---

## ADR Template

See [ADR Template](templates/ADR-template.md) for the standard format.
