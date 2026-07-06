# PLAN-0008 — Testing, Security, and Deployment

## Status

Draft

## Goal

Establish comprehensive test coverage, security hardening, and production-ready deployment configuration for Daxa POS. This plan covers the full test suite strategy, security review, and cloud + local deployment documentation and tooling.

## Scope

- Unit, integration, and API test suites.
- Security review: tenant isolation, RBAC, payment credential storage, audit log completeness.
- Cloud deployment configuration (TBD — depends on chosen cloud platform).
- Local server deployment guide.
- Hybrid deployment guide.
- CI/CD pipeline.
- Secrets management.

## Non-goals

- Full penetration testing (engages a specialist firm).
- Specific cloud provider selection (separate ADR when needed).
- Performance/load testing (Phase 3+).

## Context Read

- `docs/adr/accepted/ADR-0012-docker-local-deployment-strategy.md`
- `docs/adr/accepted/ADR-0010-financial-records-ledger-and-audit.md`
- `docs/testing/strategy.md`
- `docs/security/security-overview.md`
- `docs/deployment/cloud.md`
- `docs/deployment/local.md`
- `docs/deployment/hybrid.md`
- `docs/issues/closed/OI-0008-cloud-data-region-strategy.md`

## Files Likely To Change

```text
tests/DaxaPos.UnitTests/
tests/DaxaPos.IntegrationTests/
tests/DaxaPos.Api.Tests/
tests/DaxaPos.Tax.Tests/
tests/DaxaPos.Receipt.Tests/
tests/DaxaPos.Sync.Tests/
tests/DaxaPos.PaymentProvider.Tests/
.github/workflows/   (CI pipeline)
docker-compose.yml
docker-compose.prod.yml  (if applicable)
```

## Architecture Assumptions

- Integration tests run against real PostgreSQL and Keycloak (Docker Compose in CI).
- No database mocks in integration tests.
- Test isolation via separate database per test run (or per-test transactions).
- Payment provider tests use sandbox/test credentials.

## Domain Assumptions

- Security tests must verify: tenant isolation, location isolation, RBAC, audit log completeness.
- Financial record immutability tests are mandatory.
- Tax calculation tests must cover AU/NZ mixed baskets.

## Risks

- CI pipeline Keycloak startup time may slow test runs.
- Payment provider sandbox environments may be rate-limited.
- Cloud deployment strategy not yet decided (OI-0008).

## Implementation / Documentation Steps

1. Set up test project structure.
2. Write tenant isolation integration tests.
3. Write location isolation integration tests.
4. Write RBAC integration tests.
5. Write AU/NZ GST mixed-basket tax tests.
6. Write payment idempotency tests.
7. Write refund audit trail tests.
8. Write receipt rendering tests (with tax markers).
9. Write sync idempotency tests.
10. Set up GitHub Actions CI pipeline.
11. Write security review checklist.
12. Document local server deployment (Docker Compose).
13. Document hybrid deployment configuration.
14. Resolve OI-0008 (cloud data region strategy).
15. Update all testing and deployment docs.

## Tests To Run Later

All existing test suites must pass. New tests added in this plan.

## Documentation To Update

- `docs/testing/strategy.md`
- `docs/testing/tax-tests.md`
- `docs/testing/payment-tests.md`
- `docs/testing/sync-tests.md`
- `docs/testing/receipt-tests.md`
- `docs/testing/security-tests.md`
- `docs/deployment/cloud.md`
- `docs/deployment/local.md`
- `docs/deployment/hybrid.md`
- `docs/deployment/docker.md`

## ADRs Required

- ADR-0012 (already proposed).
- Cloud deployment ADR (when cloud platform is decided).

## Open Issues Required

- OI-0008 (cloud data region strategy) — must be resolved before cloud deployment.

## Commit Sequence

```text
test: add tenant isolation and RBAC integration tests
test: add tax, payment, and receipt tests
test: add sync and financial record audit tests
ci: add GitHub Actions pipeline
docs: update testing and deployment docs
infra: add production Docker Compose configuration
```

## Handoff Notes

This plan runs in parallel with PLAN-0006 and PLAN-0007 where possible. Security review and deployment configuration should be completed before any production rollout. Cloud deployment ADR must be created once the cloud provider is decided.
