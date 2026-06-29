# PLAN-0003 — Identity, Tenancy, Locations, and Devices

## Status

Draft

## Goal

Implement the identity, tenancy, multi-location, and device registration foundation. This includes Keycloak integration, multi-tenant API middleware, role-based access control, location hierarchy, device registration, and staff PIN login.

## Scope

- Keycloak integration (OIDC).
- Multi-tenant middleware and query filters.
- Organisation / Location / Terminal hierarchy (API and database).
- Role-based and permission-based access control.
- Device registration flow.
- Staff PIN login (short-lived token or Keycloak extension).
- Support/admin access with audit.

## Non-goals

- Business domain modules (orders, products, payments).
- PWA or MAUI UI.
- Full inventory or reporting.

## Context Read

- `docs/plans/active/PLAN-0002-platform-skeleton.md`
- `docs/adr/proposed/ADR-0003-multi-location-by-default.md`
- `docs/adr/proposed/ADR-0008-device-identity-vs-user-identity.md`
- `docs/adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md`
- `docs/architecture/tenancy.md`
- `docs/architecture/security.md`
- `docs/issues/open/OI-0002-identity-provider-local-cloud-hybrid.md`
- `docs/issues/open/OI-0010-local-keycloak-vs-cloud-keycloak.md`

## Files Likely To Change

```
src/DaxaPos.Domain/          (User, Role, Permission, Device, Location, Organisation)
src/DaxaPos.Application/     (identity use cases)
src/DaxaPos.Infrastructure/  (Keycloak client, JWT middleware)
src/DaxaPos.Persistence/     (tenant filter, location queries)
src/DaxaPos.Api/             (auth endpoints, device registration)
```

## Architecture Assumptions

- Keycloak (or decided alternative) is running via Docker Compose.
- One Keycloak realm per tenant OR shared realm with tenant claims.
- JWT bearer tokens used for all API calls.
- PIN login uses a custom short-lived session token.

## Domain Assumptions

- `Tenant → Organisation → Region → Country → Location → Terminal` hierarchy.
- Device identity and user identity are separate (ADR-0008).
- All financially significant API calls require authenticated user context.

## Risks

- Keycloak realm-per-tenant vs. shared realm decision affects implementation significantly.
- Staff PIN login in Keycloak may require a custom extension.
- Multi-tenant EF Core query filters must be applied to every query.

## Implementation / Documentation Steps

1. Resolve OI-0002 and OI-0010 (or create proposed ADR for Keycloak strategy).
2. Configure Keycloak realm and client for Daxa POS.
3. Implement JWT bearer auth middleware in ASP.NET Core.
4. Implement multi-tenant context extraction from JWT claims.
5. Apply EF Core global query filters for tenant isolation.
6. Implement Organisation, Location, Region, Country APIs.
7. Implement Terminal/Device registration API.
8. Implement staff user management API.
9. Implement PIN login flow.
10. Implement RBAC middleware.
11. Write tenant isolation integration tests.
12. Update `docs/architecture/tenancy.md` and `docs/architecture/security.md`.

## Tests To Run Later

- Tenant isolation tests (cannot access another tenant's data).
- Location isolation tests.
- Device registration tests.
- PIN login tests.
- RBAC tests (correct roles required for each endpoint).
- Support access audit tests.

## Documentation To Update

- `docs/architecture/tenancy.md`
- `docs/architecture/security.md`
- `docs/architecture/multi-location.md`
- `docs/modules/devices.md`

## ADRs Required

- ADR-0003, ADR-0008, ADR-0009 (all already proposed).

## Open Issues Required

- OI-0002, OI-0010 (to be resolved during this plan).

## Commit Sequence

```
feat(identity): add Keycloak integration and JWT auth
feat(tenancy): add multi-tenant middleware and EF Core query filters
feat(location): add organisation/location/terminal hierarchy
feat(devices): add device registration API
feat(identity): add staff PIN login
test(identity): add tenant isolation and RBAC tests
docs: update tenancy, security, and device docs
```

## Handoff Notes

This plan depends on PLAN-0002 (Platform Skeleton). After completing this plan, the identity and tenancy foundation is in place. The next worker can implement the product catalogue (PLAN-0004) with proper tenant/location context available.
