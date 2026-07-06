# Testing: Security Tests

Security tests verify tenant isolation, RBAC, and access controls.

---

## Required Test Categories

### Tenant Isolation

- Tenant A cannot access Tenant B's orders.
- Tenant A cannot access Tenant B's products.
- Tenant A cannot access Tenant B's users.
- Cross-tenant query returns empty result, not an error.

### Location Isolation

- User at Location A cannot see Location B's orders.
- User at Location A cannot see Location B's cash reconciliation.
- Multi-location user with permissions at both can see both.

### RBAC

- Staff role cannot access refund endpoints.
- Staff role cannot access void endpoints.
- Staff role cannot access tax configuration.
- VenueManager role can access refund endpoints.
- OrganisationOwner can access reporting.
- Unauthenticated request returns 401.
- Authenticated but unauthorised request returns 403.

### Financial Record Immutability

- Completed order cannot be silently edited.
- Direct database attempt to update order total is blocked by application (not raw DB test).
- Reversal/void records are created instead of editing original.

### Payment Security

- Payment provider credentials are not returned in API responses.
- Payment provider credentials are not in API logs.

### Audit Log Completeness

- Login events are recorded.
- Failed login attempts are recorded.
- Refund events are recorded with reason, user, and linked order.
- Void events are recorded.
- Receipt reprint events are recorded.
- Tax configuration change events are recorded.

---

## Test Project

```text
tests/DaxaPos.IntegrationTests/   (security and isolation tests)
tests/DaxaPos.Api.Tests/          (API-level security tests)
```

---

## Implementation Status (PLAN-0003 Milestone G, 2026-07-03)

The identity/tenancy/device slice of this document is implemented and consolidated in `tests/DaxaPos.Api.Tests/` and `tests/DaxaPos.UnitTests/` (no `DaxaPos.IntegrationTests` project exists yet — API-level tests run against a real Postgres container, matching the no-mocks pattern):

- **`RbacTests.cs`** — the consolidated authorization matrix, driven by one inventory of every protected endpoint: unauthenticated → 401, garbage/revoked token → 401, authenticated-without-the-permission → 403, device token → 403 on every permission-gated endpoint, a real staff-PIN session → 403 on every `rejectStaffPin` endpoint, and a valid session for tenant A against tenant B's rows → 404/empty (never 500, never data). New protected endpoints must be added to that inventory to inherit full matrix coverage.
- **`HybridOfflineLoginTests.cs`** — both Daxa WebAPI-native auth chains (admin username/password; device registration → staff PIN) end-to-end against local Postgres only, per ADR-0013's offline guarantee. Enforced environmentally too: CI defines no Keycloak service at all, and local runs keep the `keycloak` compose service stopped.
- **`IgnoreQueryFiltersUsageTests.cs`** (unit) — source-scan guard asserting the `IgnoreQueryFilters()` tenant-filter bypass appears only in the five documented bootstrap/authentication files (ADR-0015 §Risks).
- Per-feature security behaviour (lockout, PIN hashing, token revocation, cross-tenant/cross-organisation 404s, audit rows) is covered milestone-by-milestone in `TenantIsolationTests`, `LocalUserLoginTests`, `DeviceRegistration*Tests`, `StaffMemberEndpointsTests`, and `StaffPinLoginTests`.

Manual verification of the same surface: [Local smoke test](local-smoke-test.md).

---

## Related Documents

- [ADR-0009 — Keycloak or Identity Provider Strategy](../adr/superseded/ADR-0009-keycloak-or-identity-provider-strategy.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Security](../architecture/security.md)
- [Architecture: Tenancy](../architecture/tenancy.md)
