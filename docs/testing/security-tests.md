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

```
tests/DaxaPos.IntegrationTests/   (security and isolation tests)
tests/DaxaPos.Api.Tests/          (API-level security tests)
```

---

## Related Documents

- [ADR-0009 — Keycloak or Identity Provider Strategy](../adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/proposed/ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Security](../architecture/security.md)
- [Architecture: Tenancy](../architecture/tenancy.md)
