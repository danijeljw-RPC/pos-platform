# ADR-0015 — Tenant Isolation Mechanism and POS Session Token Format

## Status

Accepted

## Date

2026-07-01

## Context

PLAN-0003 (Identity, Tenancy, Locations, Devices) needs to implement two foundational, hard-to-reverse mechanisms before any business module can rely on them:

1. **Multi-tenant EF Core query filters.** PLAN-0002 deliberately descoped this ("requires the identity/tenancy context built in PLAN-0003") and only created the tenant-scoped tables (`Organisation`, `Location`, `Device`, `Terminal`) with FK constraints, no `TenantId` denormalization and no global query filters.
2. **A session token mechanism for POS staff and local manager/admin sessions**, per ADR-0013's mixed authentication model. ADR-0013 deliberately does not specify a token format — it only specifies that POS staff sessions are "short-lived," issued by the Daxa WebAPI, and are *not* an OIDC/Keycloak token.

Both mechanisms will be touched by every future module that adds a tenant-scoped table or an authenticated endpoint, so getting them wrong is expensive to unwind later — the same rationale ADR-0014 used for the inter-module communication pattern.

A related, smaller question is where domain-event handlers that need database access (e.g. writing an `AuditEvent` row when a login occurs) should live, given ADR-0014's accepted project reference graph is `Api → {Application, Infrastructure, Persistence}`, `Infrastructure → {Application, Domain}`, `Persistence → Domain`, `Application → Domain` — i.e. `Persistence` cannot currently implement an `Application`-defined `IDomainEventHandler<T>`.

## Decision

### 1. Tenant isolation: denormalized `TenantId` + fail-closed global query filters

Every tenant-owned table carries its own `TenantId` column (denormalized, not derived via navigation/join), indexed, with a required FK to `Tenants`. This applies to the existing `Location`, `Device`, `Terminal` tables (new column, additive migration) and to every new tenant-owned entity introduced by PLAN-0003 (`User`, `StaffMember`, `AuthSession`, `DeviceCredential`, `DeviceRegistrationPin`, `AuditEvent`, `UserRole`, `StaffMemberRole`). `Role` and `Permission` are system-wide catalogues, not tenant-owned, and are not filtered.

A new `ICurrentTenantProvider` interface (`Guid? TenantId { get; }`) is defined in `DaxaPos.Domain` (not `Application`), specifically so `DaxaDbContext` — which only references `Domain` — can take it as a constructor dependency and use it inside `OnModelCreating` global query filters, without `Persistence` needing a new reference to `Application`.

**The filter fails closed if tenant context is missing or ambiguous.** "Missing" means `ICurrentTenantProvider.TenantId` is `null` (no authenticated context yet). "Ambiguous" means any situation where more than one candidate tenant could apply, or where a client-supplied value disagrees with the authenticated context (see the Context Provenance rule below) — both cases must also produce zero rows, never a best-guess match. Concretely, the filter is `e => _currentTenantProvider.TenantId != null && e.TenantId == _currentTenantProvider.TenantId`, not "unfiltered." Operations that must run before any tenant is known (e.g. looking up a `DeviceRegistrationPin` during device enrolment) use an explicit, narrowly-scoped, audited method that calls `IgnoreQueryFilters()` — never a global "null/ambiguous tenant means unfiltered" escape hatch.

### Context provenance: tenant/location/terminal/device context is never taken from client-supplied free text

`AuthContext.TenantId`/`OrganisationId`/`LocationId`/`TerminalId`/`DeviceId` are populated exclusively from server-side state established by a registered device credential or a validated session token (see Decision §2) — never parsed or trusted directly from a request header, query string, or body field the caller controls. Where an endpoint's route or body also carries an identifier (e.g. `POST /api/v1/organisations/{organisationId}/locations`), that value identifies *which resource the caller is trying to act on*, not *which tenant's data is in scope* — it must be cross-checked against the caller's `AuthContext` (e.g. `organisationId == AuthContext.OrganisationId`) and the request rejected if they disagree. A client can never widen or redirect its own tenant/location/terminal scope by changing a URL segment or JSON field; scope only ever narrows to what the authenticated device/session context already established.

### 2. POS/local session tokens: opaque, server-hashed, DB-validated — not JWT

**POS staff login is not Keycloak/OIDC.** `AuthMethod.LocalStaffPin` (and `AuthMethod.LocalUsernamePassword` for local manager/admin) are Daxa WebAPI-native flows, per ADR-0013. Neither ever produces, consumes, or is validated against a Keycloak/OIDC token, in any deployment mode, including cloud-hosted venues.

**A POS staff session is an operational session, not an identity/admin session.** It is a distinct concept from `AuthMethod.CloudIdentityProvider` (cloud/admin/back-office/support/external identity, owned by Keycloak or equivalent) — the two are never interchangeable, never share a token format, and a POS staff session's permissions are scoped to operational POS use per ADR-0013's Staff ID/PIN Login Rules, never to identity/admin management.

`AuthSession` (backing both `AuthMethod.LocalStaffPin` and `AuthMethod.LocalUsernamePassword`) uses a random opaque bearer token (256-bit), returned to the caller once at login. **The raw token is never persisted** — the server stores only a SHA-256 hash of the token in `AuthSession.SessionTokenHash` and validates a presented token by re-hashing it and looking up the match via a database lookup on every request. The same never-store-raw rule applies to staff PINs and local passwords (see §3 below): only their hashes are ever persisted.

This is deliberately **not** a self-contained signed JWT, for three reasons:
- Instant revocation (including the ADR-0013 "emergency disable" requirement) needs the server to be able to invalidate a session the moment `RevokedAtUtc` is set, without waiting for token expiry.
- Keeping the token format visually and mechanically distinct from Keycloak's JWTs reinforces ADR-0013's boundary: a POS staff session is a Daxa WebAPI construct, never mistakable for (or interchangeable with) an OIDC token.
- The validating lookup is a local Postgres read, which is available in Local and Hybrid deployments with no internet — it does not reintroduce a cloud dependency into the local staff-PIN path.

Cloud/admin/back-office authentication via Keycloak (`AuthMethod.CloudIdentityProvider`) is unaffected by this decision and continues to use standard JWT bearer validation once that integration is wired up (see Follow-Up Work — it is out of scope for PLAN-0003).

### 3. Credential storage: PINs, passwords, session tokens, and device secrets are never stored raw

Every credential this plan handles — staff PINs, local manager/admin passwords, POS session tokens, and device registration credentials — is persisted only as a salted hash, never as plaintext, and never in a reversible encoding (e.g. base64 alone does not count as hashing). Staff PINs and local passwords use PBKDF2-SHA256 (slow, brute-force-resistant, appropriate for low-entropy human-chosen credentials); session tokens and device secrets use a fast salted/keyed hash (appropriate for high-entropy server-generated values — see the plan's Architecture Assumptions for the exact algorithm choices, which are an implementation detail this ADR does not need to fix). This ADR fixes the invariant — never raw, always hashed at rest — not the specific algorithm, so the hashing implementation can be swapped later (e.g. to Argon2id) without another ADR, as long as the invariant holds.

### 4. Domain-event handlers with DB access: stay in `DaxaPos.Api` for now, no reference-graph change

PLAN-0003 needs at least one domain-event handler with database access (writing `AuditEvent` rows in reaction to login/registration/session events, per ADR-0014's Handler I/O Rule — audit-log writes are explicitly listed there as fine to do directly in-process). Rather than adding `Persistence → Application` to satisfy this, these handlers are implemented in `DaxaPos.Api` (which already references both), registered directly in `Program.cs`. ADR-0014's accepted reference graph is left unchanged.

This is revisited the first time a *second*, unrelated DB-touching domain-event consumer needs to exist (expected around PLAN-0005 or PLAN-0007, per ADR-0014's own Follow-Up Work note) — if handler code starts accumulating in `Api` in a way that no longer feels like "composition root," that is the trigger to reconsider `Persistence → Application`, not a decision to make speculatively now.

## Consequences

### Positive

- One well-understood tenant-isolation pattern that every future module can copy without re-deriving it (add a `TenantId` column, add one filter line).
- Fail-closed filter behaviour means a missing/forgotten tenant context produces an empty result, not a cross-tenant data leak.
- Session revocation (including emergency-disable) is immediate and doesn't depend on short token lifetimes to bound the blast radius of a compromised credential.
- No change to ADR-0014's accepted reference graph; PLAN-0003 ships without asking for a second amendment to an already-accepted ADR.

### Negative

- Every authenticated request pays one extra indexed DB lookup (session validation) — acceptable given the local server target already assumes a local Postgres round trip is cheap, but worth remembering if a future high-throughput cloud path needs to reconsider it.
- Denormalized `TenantId` duplicates information already derivable via `Organisation`/`Location` — normal, accepted tradeoff for multi-tenant EF Core filters, but means `TenantId` must be set correctly at insert time everywhere (enforced via required column + tests, not by triggers).
- Audit-event handlers living in `Api` is a known, temporary layering compromise, not a long-term architectural statement.

### Risks

- If a future write path forgets to populate the denormalized `TenantId` on a new tenant-owned entity, the fail-closed filter will simply hide that row from everyone (including its rightful tenant) rather than leaking it — a safe failure mode, but one that needs a test per new entity to catch during development rather than in production.
- The `IgnoreQueryFilters()` escape hatch for bootstrap operations (device registration PIN lookup) is a place where a future contributor could be tempted to reach for it more broadly; the plan's own tests should assert it is used in exactly the one narrow, documented location.

## Alternatives Considered

- **Navigation-property-based query filters** (e.g. `l => l.Organisation.TenantId == ...`) — rejected. Requires exposing navigation properties EF currently doesn't have on these entities, generates a join per filtered query, and is harder to reason about for entities several hops from `Tenant` (e.g. a future `OrderLine` under `Order` under `Terminal` under `Location`).
- **Self-contained signed JWT for POS/local sessions** — rejected. Complicates instant revocation (would require a denylist check anyway, negating the "no DB lookup" benefit while adding signing-key management), and blurs ADR-0013's explicit line between Keycloac/OIDC tokens and Daxa WebAPI-native sessions.
- **Add `Persistence → Application` now** to give domain-event handlers a conventional home — deferred, not rejected outright. Reasonable, but not justified by a single consumer; revisit per the Follow-Up Work note below rather than deciding speculatively.

## Follow-Up Work

- Wiring actual Keycloak JWT bearer authentication for `AuthMethod.CloudIdentityProvider` is out of scope for PLAN-0003 (no cloud/admin-portal consumer exists yet in this plan's non-goals-constrained scope). The plan should be picked up by whichever later plan first builds a cloud/admin-portal endpoint that needs it.
- Revisit the `Persistence → Application` question (see Decision §4) the first time a second DB-touching domain-event handler is needed outside `Api`.
- PLAN-0007 (Sync) should confirm the denormalized `TenantId` pattern extends cleanly to synced/replicated data, and that sync conflict records (OI-0006) carry `TenantId` consistently with everything else in this ADR.

## Related Documents

- [ADR-0003 — Multi-Location by Default](../accepted/ADR-0003-multi-location-by-default.md)
- [ADR-0008 — Device Identity vs User Identity](../accepted/ADR-0008-device-identity-vs-user-identity.md)
- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](../accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md)
- [ADR-0014 — Inter-Module Communication Pattern](../accepted/ADR-0014-inter-module-communication.md)
- [PLAN-0002 — Platform Skeleton](../../plans/active/PLAN-0002-platform-skeleton.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)

---

## Acceptance Addendum

ADR-0015 is accepted (2026-07-01), with the following explicit points confirmed as part of acceptance, per human review:

- POS staff login is not Keycloak/OIDC (Decision §2).
- POS staff sessions are operational sessions, separate from admin/cloud identity (Decision §2).
- Session tokens must never be stored raw (Decision §2/§3).
- PINs must never be stored raw (Decision §3).
- Tenant context must fail closed if missing or ambiguous (Decision §1).
- Tenant/location/terminal context must come from a registered terminal/session context, not arbitrary user-supplied free text (Decision §1, Context Provenance).

## Status Update

Status: **Accepted**
