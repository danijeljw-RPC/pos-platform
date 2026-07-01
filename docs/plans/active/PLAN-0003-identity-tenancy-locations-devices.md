# PLAN-0003 — Identity, Tenancy, Locations, and Devices

## Status

Approved 2026-07-01 — Milestone A in progress. Human sign-off received on the plan and on [ADR-0015](../../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md) (now **Accepted**, moved from `proposed/` to `accepted/`), plus explicit decisions on Region/Country descoping, PIN/session policy defaults, dev-only bootstrap seeding, and single-home-location `StaffMember` for MVP — see "Human Decisions Needed" for the recorded answers. Milestone B is now unblocked.

- [x] Milestone A — Auth primitives (`AuthMethod`, `ICurrentTenantProvider`, `AuthContext`, hashers, token service, `DaxaPos.UnitTests` project) — complete, see PLAN-0003-worker-notes.md for the Milestone A report.
- [ ] Milestone B — Tenant isolation (schema + fail-closed query filters)
- [ ] Milestone C — RBAC schema, seed data, local username/password login, audit plumbing
- [ ] Milestone D — Organisation / Location / Terminal endpoints
- [ ] Milestone E — Device registration and credentials
- [ ] Milestone F — StaffMember and staff PIN login
- [ ] Milestone G — Local/offline verification and RBAC consolidation
- [ ] Milestone H — Documentation and plan closeout

## Goal

Implement the identity, tenancy, multi-location, and device registration foundation using the mixed authentication model accepted in ADR-0013:

- Keycloak (or an equivalent identity provider) remains reserved for cloud, back-office, admin, support, and external identity scenarios — not touched by this plan (see Non-goals).
- A Daxa WebAPI-owned staff ID + PIN flow for local POS terminal sessions — **not** an OIDC/Keycloak login.
- A Daxa WebAPI-owned username/password flow for local manager/admin login (MVP).
- One normalised authorization context (`AuthContext`) applied consistently regardless of which authentication method produced it.

## Scope

- Daxa WebAPI-native staff ID + PIN authentication service for POS terminal staff sessions.
- Daxa WebAPI-native username/password authentication for local manager/admin login (MVP).
- Short-lived session model (`AuthSession`) shared by both flows: tied to terminal/location/device where applicable, the authenticating identity, and a role/permission snapshot taken at session start, with full audit context.
- Multi-tenant EF Core global query filters (tenant isolation), fed by a shared tenant-context mechanism.
- Organisation / Location / Terminal CRUD APIs, scoped by tenant and authorization.
- Role-based and permission-based access control (`RBAC`), applied uniformly via the shared `AuthContext` — a minimal role/permission catalogue sized to this plan's own endpoints, not the full eventual permission set.
- Device registration flow (ADR-0008): device identity registered and trusted independently of any user identity, including registration PINs, credential issuance, rotation, and revocation.
- Audit context foundation: `AuditEvent` entity + in-process domain events for login/session/device lifecycle actions, per ADR-0014.
- Local mode: staff PIN login and manager/admin login must work with no internet connectivity (verified with the Keycloak container stopped, matching PLAN-0002's own verification pattern).
- Hybrid mode (bounded): staff PIN login only ever reads the local database — never Keycloak, never a cloud-only endpoint — so it is unaffected by an internet outage. The actual local↔cloud data-replication mechanism (outbox, conflict rules per OI-0006) is explicitly **not** built here; see Non-goals and the "Hybrid Mode Scope Clarification" domain assumption below.
- Cloud mode: the Daxa WebAPI remains the authority that issues and validates POS staff sessions, even when the venue is cloud-hosted.

## Non-goals

- Business domain modules (orders, products, payments, tax).
- PWA or MAUI UI — this plan is API-only.
- Full inventory or reporting.
- Local Keycloak deployment — explicitly rejected for MVP by ADR-0013.
- Using Keycloak/OIDC for POS terminal staff PIN login — explicitly rejected by ADR-0013.
- **Wiring Keycloak JWT bearer authentication at all.** The Keycloak container already exists (PLAN-0002) but nothing in this plan's endpoint list needs `AuthMethod.CloudIdentityProvider` — every endpoint this plan builds is reachable via the WebAPI-native username/password path, per ADR-0013's explicit allowance for that in MVP. `AuthMethod.CloudIdentityProvider` is defined in the enum so the model is ready, but no middleware validates it yet. See ADR-0015 §Follow-Up Work.
- Full admin UI/back-office for RBAC (role/permission management is API-only, minimal-catalogue).
- A new `DaxaPos.Modules.*` project. Identity/tenancy code is added to the existing `Domain`/`Application`/`Infrastructure`/`Persistence`/`Api` projects — see "Architecture Assumptions" for why a module project isn't justified yet.
- `Region`/`Country` as first-class entities. ADR-0003 describes them as "optional grouping... not for data isolation"; this plan only adds `TenantId` to existing tables for isolation. See "Human Decisions Needed" if this should change.
- The actual local↔cloud sync/outbox mechanism (PLAN-0007's job) and the sync conflict review queue (OI-0006).
- Clock-on/clock-off timesheets, shift management, cash float/drawer reconciliation — `security.md`/`docs/modules/13-...md` mention these as examples of what a staff PIN session *may eventually* authorize, not a feature this plan builds.
- Multi-location staff assignment (a `StaffMember` belonging to more than one `Location`). MVP models one home location per staff member — see "Human Decisions Needed."

## Context Read

- `CLAUDE.md`
- `docs/README.md`, `docs/adr/index.md`, `docs/issues/index.md`
- `docs/plans/active/PLAN-0002-platform-skeleton.md` (current skeleton state, what's already committed)
- `docs/adr/accepted/ADR-0003-multi-location-by-default.md`
- `docs/adr/accepted/ADR-0008-device-identity-vs-user-identity.md`
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`
- `docs/adr/accepted/ADR-0014-inter-module-communication.md`
- `docs/adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md` (proposed in the prior planning pass, accepted 2026-07-01 with explicit additions — see its Acceptance Addendum)
- `docs/architecture/tenancy.md`, `docs/architecture/security.md`, `docs/architecture/multi-location.md`
- `docs/modules/devices.md`, `docs/modules/audit.md`
- `docs/testing/testing-strategy.md`, `docs/testing/security-tests.md`
- `docs/issues/closed/OI-0002-identity-provider-local-cloud-hybrid.md`, `OI-0006-hybrid-sync-conflict-rules.md`, `OI-0007-tax-configuration-editing-permissions.md`, `OI-0010-local-keycloak-vs-cloud-keycloak.md`
- Current source: `src/DaxaPos.Domain/Entities/*` (`Tenant`, `Organisation`, `Location`, `Device`, `DeviceType`, `Terminal`), `src/DaxaPos.Persistence/*` (`DaxaDbContext`, configurations), `src/DaxaPos.Application/Events/*`, `src/DaxaPos.Infrastructure/*`, `src/DaxaPos.Api/Program.cs`, `tests/DaxaPos.Api.Tests/*`, all `*.csproj` files (to confirm the current project reference graph and package versions).

## Files Likely To Change

```
docs/adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md   (accepted 2026-07-01)
docs/adr/index.md                                                            (ADR-0015 listed under Accepted)

src/DaxaPos.Domain/
  Entities/User.cs, StaffMember.cs, Role.cs, Permission.cs, RolePermission.cs,
           UserRole.cs, StaffMemberRole.cs, AuthSession.cs, DeviceCredential.cs,
           DeviceRegistrationPin.cs, AuditEvent.cs                            (new)
  Entities/Location.cs, Device.cs, Terminal.cs                               (add TenantId)
  Enums/AuthMethod.cs                                                        (new)
  Tenancy/ICurrentTenantProvider.cs                                          (new)
  Events/*DomainEvent.cs (10 new event records — see Implementation Steps)

src/DaxaPos.Application/
  Identity/AuthContext.cs, IAuthContextAccessor.cs, IPinHasher.cs,
           IDeviceCredentialHasher.cs, ISessionTokenService.cs, Permissions.cs (new)

src/DaxaPos.Infrastructure/
  Security/Pbkdf2PinHasher.cs, HmacDeviceCredentialHasher.cs,
           RandomSessionTokenService.cs                                     (new)
  Identity/HttpContextAuthContextAccessor.cs, CurrentTenantProvider.cs      (new)
  DependencyInjection.cs                                                   (register new services)
  DaxaPos.Infrastructure.csproj                                            (add Microsoft.AspNetCore.Http.Abstractions)

src/DaxaPos.Persistence/
  Configurations/*.cs for every new entity + updates to Location/Device/Terminal configs
  DaxaDbContext.cs                                                         (ctor takes ICurrentTenantProvider; global query filters; new DbSets)
  Migrations/ (5 new migrations — see Implementation Steps)

src/DaxaPos.Api/
  Authentication/DeviceTokenAuthenticationHandler.cs, SessionAuthenticationHandler.cs (new)
  Authorization/RequirePermissionAttribute.cs or endpoint filter                       (new)
  Audit/DomainEventAuditHandlers.cs                                                    (new)
  Endpoints/Identity/*.cs (organisations, locations, terminals, devices,
             device-registration, staff-members, users, auth, sessions)               (new)
  Program.cs                                                                          (wire everything up)

tests/DaxaPos.Api.Tests/
  TenantIsolationTests.cs, StaffPinLoginTests.cs, LocalUserLoginTests.cs,
  DeviceRegistrationTests.cs, RbacTests.cs, HybridOfflineLoginTests.cs                 (new)

tests/DaxaPos.UnitTests/                                                              (new project)
  DaxaPos.UnitTests.csproj
  Pbkdf2PinHasherTests.cs, HmacDeviceCredentialHasherTests.cs,
  AuthContextPermissionTests.cs

DaxaPos.sln                                                                           (add DaxaPos.UnitTests)

docs/architecture/tenancy.md, security.md, multi-location.md
docs/modules/devices.md, audit.md
docs/plans/active/PLAN-0003-worker-notes.md                                           (new)
```

## Architecture Assumptions

- **No new `DaxaPos.Modules.*` project.** The current skeleton has no `Modules.*` projects at all — ADR-0014's module-boundary rules govern communication *between* `Modules.*` projects, which don't exist yet. Introducing a `Modules.Identity` project now, before any second module exists to define a boundary against, would be speculative structure with no present payoff. Identity/tenancy code is added directly to `Domain`/`Application`/`Infrastructure`/`Persistence`/`Api`, following the layering already established by PLAN-0002.
- **The existing project reference graph is unchanged.** `Api → {Application, Infrastructure, Persistence}`, `Infrastructure → {Application, Domain}`, `Persistence → Domain`, `Application → Domain` — exactly as PLAN-0002/ADR-0014 left it. This is possible because: (a) `ICurrentTenantProvider` lives in `Domain` (so `Persistence`'s `DaxaDbContext` can consume it without a new reference), and (b) domain-event handlers needing DB access are hosted in `Api` for now, per ADR-0015 §4.
- **Authentication handlers and endpoint orchestration live in `DaxaPos.Api`**, not in an `Application`-layer "service" class, because `Application` cannot reference `Persistence` (no `DaxaDbContext` access) under the current graph, and inventing a repository-interface-per-entity abstraction purely to keep orchestration logic in `Application` is not justified for a plan this size (YAGNI/no premature abstraction). `Api` already plays the composition-root role for the skeleton; this plan continues that pattern for identity endpoints too.
- **Tenant isolation uses denormalized `TenantId` + fail-closed EF Core global query filters**, per ADR-0015 §1.
- **Session tokens are opaque, server-hashed, DB-validated — not JWT**, per ADR-0015 §2.
- **PIN hashing:** PBKDF2-SHA256 via `Rfc2898DeriveBytes.Pbkdf2` (built into .NET 9, no new NuGet dependency), 210,000 iterations (OWASP 2023 baseline for PBKDF2-SHA256), 16-byte random salt per credential, stored as `{iterations}.{base64(salt)}.{base64(hash)}`. Combined with account lockout (see Domain Assumptions) since numeric PINs have low entropy and a slow hash alone is not sufficient.
- **Device credential hashing:** device credentials are high-entropy (256-bit random secrets, not human-chosen), so a fast salted hash (HMAC-SHA256 with a per-record random salt) is used rather than PBKDF2 — appropriate given these may be checked on every request from a terminal and don't need brute-force-resistant slow hashing the way a 4-6 digit PIN does.
- **Roles and Permissions are a system-wide catalogue, not tenant-owned** — seeded via EF Core `HasData` in their configurations (deterministic GUIDs), not created via API. Only the *assignment* tables (`UserRole`, `StaffMemberRole`) are tenant-scoped.
- **Sensitive identity/tenancy endpoints reject `AuthMethod.LocalStaffPin` outright**, in addition to the normal permission check — this is defense-in-depth matching ADR-0013's explicit rule that PIN login must never be used for "editing user permissions," even if a role/permission were ever misconfigured to include the relevant permission code for a PIN-authenticated session. Endpoints so restricted: all of `organisations.*`, `locations.*` (write), `terminals.*` (write), `staff-members.*`, `users.*`, `sessions.*` (revoke-other).
- **Bootstrapping:** the very first `Tenant`/`Organisation`/`User` (a `SystemAdmin`) is created outside the public API, since there is no way to call `POST /api/v1/organisations` without already being authenticated as someone with `organisations.manage` — a chicken-and-egg seed is unavoidable for the first tenant. Corrected mechanism (per Human Decisions Needed #4): `Tenant`/`Organisation` (non-secret) are seeded via EF Core `HasData` in their configurations, but the bootstrap `User`'s password is **not** — `HasData` values are baked into migration source at design time, which would mean committing a password hash to source control. Instead, a small idempotent startup routine in `DaxaPos.Api` (runs once per app start, before `app.Run()`: if no `SystemAdmin` user exists for the seeded bootstrap tenant, read `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`DAXA_BOOTSTRAP_ADMIN_PASSWORD` from configuration/environment variables — with documented local-dev-only defaults in `deploy/.env.example`, never a literal in source — hash the password via `IPinHasher`, and insert the `User` row) creates the bootstrap admin. This is clearly commented as dev/local-only and documented in `docs/deployment/local.md`; it must not run — or must require the env vars to be explicitly set with no fallback default — in any deployment mode flagged as production. This is flagged in "Human Decisions Needed" since production onboarding/provisioning is out of scope for this plan.

## Domain Assumptions

- `Tenant → Organisation → Location → Terminal` hierarchy (matching what PLAN-0002 actually built) plus a `Device`, separate from `Terminal`, per ADR-0008. `Region`/`Country` are out of scope (see Non-goals).
- A POS staff session requires all of: a trusted registered device context (ADR-0008, `AuthMethod.DeviceToken` established first), a location context, a valid staff code/PIN pair, and a role/permission snapshot taken at session start. **A device token alone carries empty `Roles`/`Permissions` in `AuthContext` — it must never grant user permissions by itself.**
- POS staff sessions are short-lived and scoped to operational actions only, per ADR-0013's Staff ID/PIN Login Rules. This plan does not build any of those operational endpoints (orders, payments — later plans), but the `AuthContext`/RBAC mechanism it does build must already refuse a `LocalStaffPin` session on the identity/tenancy endpoints this plan *does* build, since those fall squarely in ADR-0013's "must not be used for" list.
- **Hybrid Mode Scope Clarification:** "hybrid mode using synced local staff/permission data" is satisfied at this stage by construction, not by building a sync mechanism: the staff-PIN and local-username/password login code paths only ever read `DaxaDbContext` (the local Postgres database) and never call Keycloak or any cloud-only service. In the current single-database MVP architecture there is one database per deployment (ADR-0002/ADR-0007), so "local data" and "the data" are the same thing — there is no separate local-vs-cloud replica to sync between yet. The actual local↔cloud replication protocol (outbox tables, conflict detection per OI-0006, the admin review queue) is PLAN-0007's responsibility; this plan's job is only to make sure nothing in the auth path *assumes* live connectivity, which it verifies directly (Milestone G: Keycloak container stopped, staff-PIN and username/password login still work).
- Local mode: staff PIN login and local username/password login must succeed with zero internet connectivity — verified directly, not just asserted.
- Cloud mode: even when the venue is cloud-hosted, the Daxa WebAPI — not Keycloak — issues and validates POS staff sessions. Nothing in this plan special-cases "cloud mode" differently from "local mode" at the code level; it's the same WebAPI code running in different infrastructure, per ADR-0001/ADR-0002.
- A `StaffMember` has exactly one home `Location` in this plan (see "Human Decisions Needed" re: multi-location staff). A `User` (manager/admin/back-office) is scoped to an `Organisation`, not a single `Location`.
- `StaffMember.StaffCode` is unique per `Organisation` (a short, human-enterable code — e.g. 4 digits — distinct from the `StaffMember.Id` GUID primary key, per the task's explicit instruction not to conflate the two).
- Manager PIN vs. username/password action-risk distinctions (ADR-0013's table) are a rule this plan's RBAC mechanism must be *capable* of enforcing (via the `AuthMethod != LocalStaffPin` check described above) but the actual risk-tiered actions themselves (refunds, discounts, void) belong to later plans (PLAN-0005 etc.) — this plan only proves the mechanism using its own endpoints as the example.

## Risks

- **Two authentication code paths (local username/password and staff PIN) must both normalise into the same `AuthContext` and be checked by the same authorization layer** without permission logic diverging between them. Mitigated by building both through the same `AuthSession` table and the same `RequirePermission` check; `RbacTests.cs` explicitly tests both paths against the same set of endpoints.
- **Staff PIN storage/lockout must be implemented from scratch** — there is no Keycloak password-policy machinery to borrow for this path. Mitigated by the PBKDF2 + lockout design above and dedicated tests (correct PIN, wrong PIN × N triggering lockout, lockout expiry).
- **Fail-closed query filters must actually be applied to every new tenant-owned entity** — a forgotten `TenantId`/filter on a future table is a silent isolation gap (though fails safe, not open — see ADR-0015 §Risks). Mitigated by making "add a `TenantId` column + filter line" the first, explicit step whenever a new tenant-owned entity is introduced, and a checklist item in `docs/architecture/tenancy.md`.
- **This plan is large** (roughly 8 milestones, ~20 commits). Per CLAUDE.md's rule against more than three meaningful changes without a plan refresh, this plan's own "Status" section must be updated with ✅/⬜ checkboxes after each milestone at minimum (see PLAN-0002's own doc for the pattern to follow) — flagged explicitly so the next worker doesn't skip it.
- ~~ADR-0015 is not yet accepted.~~ Resolved: accepted 2026-07-01 with explicit additions (see its Acceptance Addendum) — Milestone B is unblocked.
- **Bootstrap seeding of the first `SystemAdmin` user** is a known-weak point for a real deployment (see corrected env-var-sourced mechanism in Architecture Assumptions) — acceptable for this plan's MVP/local-dev scope, called out explicitly rather than silently shipped, since tenant/organisation onboarding for real customers is a distinct, larger topic this plan does not solve. The startup seeding routine must be written so it cannot silently run against a production database with a guessable default credential — this is a specific implementation risk to test for in Milestone C (an explicit test asserting the routine refuses to run without the env vars set, rather than falling back to a guessable default, is required).

## Implementation / Documentation Steps

Organised as milestones in dependency order. Each milestone ends in one or more commits (see Commit Sequence) and should be followed by a plan status update.

### Milestone A — Auth primitives (no DB, no ADR-0015 dependency)

1. Add `AuthMethod` enum to `DaxaPos.Domain` (`CloudIdentityProvider`, `LocalUsernamePassword`, `LocalStaffPin`, `DeviceToken`, `SupportAccess` — matching ADR-0013 exactly).
2. Add `ICurrentTenantProvider` (`Guid? TenantId { get; }`) to `DaxaPos.Domain.Tenancy`.
3. Add to `DaxaPos.Application.Identity`: `AuthContext` (record: `TenantId`, `OrganisationId?`, `LocationId?`, `UserId?`, `StaffMemberId?`, `DeviceId?`, `AuthMethod`, `Roles: IReadOnlyCollection<string>`, `Permissions: IReadOnlyCollection<string>`), `IAuthContextAccessor` (`AuthContext? Current { get; }`), `IPinHasher` (`Hash(string pin)`, `Verify(string pin, string hash)`), `IDeviceCredentialHasher` (same shape), `ISessionTokenService` (`GenerateToken()`, `Hash(string token)`), and `Permissions` (const string catalogue — see table below).
4. Implement in `DaxaPos.Infrastructure.Security`: `Pbkdf2PinHasher`, `HmacDeviceCredentialHasher`, `RandomSessionTokenService`.
5. Implement in `DaxaPos.Infrastructure.Identity`: `HttpContextAuthContextAccessor` (reads `HttpContext.Items["AuthContext"]` via `IHttpContextAccessor`) and `CurrentTenantProvider` (implements `Domain.Tenancy.ICurrentTenantProvider`, returns `HttpContextAuthContextAccessor`'s current `TenantId`). Add `Microsoft.AspNetCore.Http.Abstractions` package reference to `DaxaPos.Infrastructure.csproj`.
6. Register all of the above in `AddDaxaInfrastructure`.
7. Add `tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj` (referencing `Domain`, `Application`, `Infrastructure`); add to `DaxaPos.sln`. Write `Pbkdf2PinHasherTests` (hash/verify round-trip, wrong-PIN rejection, distinct salts for identical PINs) and `HmacDeviceCredentialHasherTests`.

### Milestone B — Tenant isolation (unblocked — ADR-0015 accepted 2026-07-01)

8. Add `TenantId` (Guid, required) to `Location`, `Device`, `Terminal` entities and their EF configurations (indexed FK to `Tenant`).
9. Update `DaxaDbContext` constructor to accept `ICurrentTenantProvider`; add fail-closed global query filters for `Location`, `Device`, `Terminal` in `OnModelCreating`.
10. Create migration `AddTenantIsolationColumns`. No backfill logic needed (no production data exists yet in this skeleton). Apply and verify against a real Postgres container (`dotnet ef database update` + `psql \d` check), matching PLAN-0002's verification style.
11. Add `TenantIsolationTests.cs` to `DaxaPos.Api.Tests`: seed two tenants' `Organisation`/`Location` rows directly via `DaxaDbContext` with a test double `ICurrentTenantProvider`, assert cross-tenant queries return zero rows and same-tenant queries return the expected rows; assert a `null` `ICurrentTenantProvider.TenantId` also returns zero rows (fail-closed).

### Milestone C — RBAC schema, seed data, local username/password login, audit plumbing

12. Add entities: `Role` (`Id`, `Name`, `Description`, `IsSystemDefined`), `Permission` (`Id`, `Code`, `Description`), `RolePermission` (join), `User` (`Id`, `TenantId`, `OrganisationId`, `Email`, `DisplayName`, `PasswordHash?`, `ExternalIdentityProvider?`, `ExternalSubjectId?`, `IsActive`, `FailedLoginCount`, `LockedOutUntilUtc?`, `CreatedAtUtc`), `UserRole` (`UserId`, `RoleId`, `TenantId`, `OrganisationId?`, `LocationId?`), `AuthSession` (`Id`, `TenantId`, `OrganisationId?`, `LocationId?`, `TerminalId?`, `DeviceId?`, `UserId?`, `StaffMemberId?`, `AuthMethod`, `RoleSnapshot` (jsonb `string[]`), `PermissionSnapshot` (jsonb `string[]`), `SessionTokenHash`, `IssuedAtUtc`, `ExpiresAtUtc` (absolute 12h cap), `LastActivityAtUtc` (updated per authenticated request; session rejected once idle beyond 8h, per Human Decisions Needed #3), `RevokedAtUtc?`, `RevokedReason?`), `AuditEvent` (`Id`, `TenantId`, `OrganisationId?`, `LocationId?`, `TerminalId?`, `DeviceId?`, `UserId?`, `StaffMemberId?`, `EventType`, `EntityType?`, `EntityId?`, `BeforeValue` (jsonb, nullable), `AfterValue` (jsonb, nullable), `Reason?`, `IpAddress?`, `OccurredAtUtc`).
13. Seed `Role` (`SystemAdmin`, `OrganisationOwner`, `VenueManager`, `Staff`, `SupportAccess`) and `Permission` (see table below) and `RolePermission` mappings via `HasData` in their configurations. Seed one bootstrap `Tenant`/`Organisation` via `HasData` (non-secret, deterministic GUIDs). Add a dev/local-only startup routine in `DaxaPos.Api` (registered before `app.Run()`) that creates the bootstrap `SystemAdmin` `User` from `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`DAXA_BOOTSTRAP_ADMIN_PASSWORD` env vars if no such user exists yet — refusing to run (not falling back to a guessable default) if the env vars are unset. Document the local-dev default values in `deploy/.env.example`, never as a literal in source.
14. Create migration `AddIdentityAndRbacCore`. Apply and verify.
15. Add domain events: `LocalUserLoginSucceededDomainEvent`, `LocalUserLoginFailedDomainEvent` (both: `TenantId`, `OrganisationId?`, `UserId?`, `AttemptedEmail`, `OccurredAtUtc`, plus `FailureReason` on the failed variant).
16. Implement `POST /api/v1/auth/local/login` in `DaxaPos.Api` (validates email/password against `User.PasswordHash` via `IPinHasher`... actually a dedicated password hasher — reuse `IPinHasher`'s PBKDF2 mechanism since the algorithm is the same, just applied to a password instead of a PIN; do not introduce a second hashing scheme for this plan), issues an `AuthSession` (`AuthMethod = LocalUsernamePassword`), returns the opaque token. Raise `LocalUserLoginSucceededDomainEvent`/`Failed` accordingly.
17. Implement `SessionAuthenticationHandler` (custom `AuthenticationHandler<AuthenticationSchemeOptions>`): reads `Authorization: Bearer <token>`, hashes it, looks up `AuthSession` where `SessionTokenHash` matches, not revoked, not past `ExpiresAtUtc` (12h absolute cap), and not idle beyond 8h since `LastActivityAtUtc`; on success, updates `LastActivityAtUtc` to now and builds `AuthContext` from the session's snapshot; stashes it in `HttpContext.Items["AuthContext"]`. A request failing only the idle-timeout check is rejected the same way as an expired/revoked session (401), not silently extended.
18. Implement `RequirePermission(string permissionCode, bool rejectStaffPin = false)` as a minimal API endpoint filter, checking `AuthContext.Permissions.Contains(permissionCode)` and, if `rejectStaffPin`, `AuthContext.AuthMethod != AuthMethod.LocalStaffPin`.
19. Implement `Api/Audit/DomainEventAuditHandlers.cs`: `IDomainEventHandler<LocalUserLoginSucceededDomainEvent>` and `Failed` variant, each writing one `AuditEvent` row via injected `DaxaDbContext`. Register both in `Program.cs`.
20. Implement `POST /api/v1/auth/logout` (revokes the calling session's `AuthSession`, raises `AuthSessionRevokedDomainEvent`, audited).
21. Add `LocalUserLoginTests.cs` to `DaxaPos.Api.Tests`: correct password, wrong password, inactive user, lockout after N failures, successful login followed by an authenticated call to a protected endpoint, logout then reuse of the revoked token is rejected. Add an `AuditEvent` assertion (login success/failure produces exactly one row with the expected fields).

### Milestone D — Organisation / Location / Terminal endpoints

22. Implement `POST /api/v1/organisations`, `GET /api/v1/organisations/{id}`, `POST /api/v1/organisations/{organisationId}/locations`, `GET /api/v1/organisations/{organisationId}/locations`, `GET /api/v1/locations/{id}`, `POST /api/v1/locations/{locationId}/terminals`, `GET /api/v1/locations/{locationId}/terminals`, `GET /api/v1/terminals/{id}` — each gated by the matching permission (`organisations.manage`, `locations.manage`, `terminals.manage`) with `rejectStaffPin: true`, scoped by the caller's `AuthContext.OrganisationId`/`TenantId`.
23. Add tests covering: a non-owner organisation cannot create a location under another organisation's id; a `Staff`-role session (once staff-PIN login exists in Milestone F) is rejected outright — write this test now against the username/password path and revisit/extend once Milestone F lands.

### Milestone E — Device registration and credentials

24. Add entities: `DeviceCredential` (`Id`, `TenantId`, `DeviceId`, `CredentialHash`, `Salt`, `IssuedAtUtc`, `RotatedAtUtc?`, `RevokedAtUtc?`, `Status` enum `Active/Retired/Revoked`), `DeviceRegistrationPin` (`Id`, `TenantId`, `OrganisationId`, `LocationId`, `PinCode` (stored hashed, same hasher as device credentials — high-entropy is not required here since it's rate-limited and short-lived, but hashing at rest is still correct practice), `ExpiresAtUtc`, `MaxUses`, `UsedCount`, `CreatedByUserId`, `CreatedAtUtc`, `RevokedAtUtc?`).
25. Create migration `AddDeviceCredentialsAndRegistrationPins`. Apply and verify.
26. Add domain events: `DeviceRegisteredDomainEvent`, `DeviceRegistrationFailedDomainEvent`, `DeviceCredentialRotatedDomainEvent`, `DeviceRevokedDomainEvent`; corresponding `Api/Audit` handlers.
27. Implement `POST /api/v1/locations/{locationId}/device-registration-pins` (`devices.register` permission, `rejectStaffPin: true`) — generates and returns a 6-digit PIN (per ADR-0008) plus expiry.
28. Implement `POST /api/v1/device-registration` (pre-auth, PIN-gated per ADR-0008 — not an authenticated endpoint in the `AuthContext` sense, but rate-limited and fully audited): validates PIN against `DeviceRegistrationPin` (via the one documented `IgnoreQueryFilters()` bootstrap path from ADR-0015 §1, since no tenant context exists yet), creates `Device` + `DeviceCredential`, returns the device id and credential secret **once**.
29. Implement `DeviceTokenAuthenticationHandler`: reads a device bearer token (distinct scheme name from the session scheme, e.g. `Device <secret>`), hashes it, looks up `DeviceCredential`, builds a partial `AuthContext` (`DeviceId`, `LocationId`, `OrganisationId`, `TenantId` populated; `UserId`/`StaffMemberId` null; `Roles`/`Permissions` empty) with `AuthMethod = DeviceToken`.
30. Implement `POST /api/v1/devices/{id}/rotate-credential` and `POST /api/v1/devices/{id}/revoke` (`devices.manage`, `rejectStaffPin: true`) and `GET /api/v1/locations/{locationId}/devices`.
31. Add `DeviceRegistrationTests.cs`: valid PIN succeeds once, reused/expired PIN fails, rate-limit on repeated invalid PINs, a `DeviceToken`-only `AuthContext` is rejected by every permission-gated endpoint (proving a device token alone grants nothing), rotation invalidates the old credential, revocation blocks further use — each audited.

### Milestone F — StaffMember and staff PIN login

32. Add entities: `StaffMember` (`Id`, `TenantId`, `OrganisationId`, `LocationId`, `StaffCode`, `DisplayName`, `PinHash`, `FailedPinAttempts`, `LockedOutUntilUtc?`, `IsActive`, `LinkedUserId?`, `CreatedAtUtc`), `StaffMemberRole` (`StaffMemberId`, `RoleId`, `TenantId`, `LocationId?`).
33. Create migration `AddStaffMembers`. Apply and verify.
34. Add domain events: `StaffPinLoginSucceededDomainEvent`, `StaffPinLoginFailedDomainEvent`, `StaffMemberDisabledDomainEvent`; corresponding audit handlers.
35. Implement `POST /api/v1/organisations/{organisationId}/staff-members`, `POST /api/v1/staff-members/{id}/reset-pin`, `POST /api/v1/staff-members/{id}/roles`, `GET /api/v1/staff-members/{id}` (`staff.manage`, `rejectStaffPin: true`).
36. Implement `POST /api/v1/auth/staff-pin/login`: requires a valid `DeviceToken` `AuthContext` (Milestone E) on the request; body `{ locationId, staffCode, pin }`; validates `locationId` matches the device's registered location, validates `staffCode`+`pin` (PBKDF2 verify, lockout on repeated failure), issues an `AuthSession` (`AuthMethod = LocalStaffPin`) with the staff member's role/permission snapshot. Rejects if the resulting permission set would include any of the sensitive-endpoint permissions this plan gates with `rejectStaffPin: true` — logged as a configuration error (a role should never be assigned both to staff-PIN-eligible staff and a sensitive permission, but the check exists as a second line of defense).
37. Implement `POST /api/v1/staff-members/{id}/disable` (`staff.manage`, `rejectStaffPin: true`) — sets `IsActive = false`, revokes all of that staff member's active `AuthSession` rows, raises `StaffMemberDisabledDomainEvent` — this is the "emergency disable" path from ADR-0013's hybrid deployment behaviour section.
38. Add `StaffPinLoginTests.cs`: correct PIN via a trusted device succeeds; wrong PIN × N locks out; login attempt from a device registered to a *different* location is rejected; login without any device token is rejected; a `LocalStaffPin` session cannot call any of the Milestone C/D/E `rejectStaffPin: true` endpoints even if (mis)configured with a matching permission; disabling a staff member immediately revokes their active session (next authenticated call with the old token fails).

### Milestone G — Local/offline verification and RBAC consolidation

39. Add `HybridOfflineLoginTests.cs`: with the `keycloak` container stopped (docker compose), run the full local-username/password login test and the full staff-PIN login test end-to-end — proving neither path has a hidden Keycloak/cloud dependency, matching PLAN-0002's own "API starts and reports healthy with Keycloak stopped" verification pattern but for the auth paths specifically.
40. Add `RbacTests.cs` consolidating cross-cutting RBAC assertions across both auth methods: unauthenticated request → 401; authenticated-but-unauthorized (wrong permission) → 403; `Staff`-role PIN session on every `rejectStaffPin: true` endpoint → 403; cross-tenant access via a valid session for the wrong tenant → 404/empty, not 500.

### Milestone H — Documentation and plan closeout

41. Update `docs/architecture/tenancy.md` (tenant isolation mechanism, denormalized `TenantId` note), `docs/architecture/security.md` (confirm it still matches the implemented `AuthContext`/session model — it already documents the ADR-0013 model correctly at the conceptual level), `docs/architecture/multi-location.md` (note `Region`/`Country` deferral), `docs/modules/devices.md` (registration PIN + credential lifecycle implemented).
42. ~~Move `docs/adr/proposed/ADR-0015-...md` to `docs/adr/accepted/` once approved~~ — done 2026-07-01, per explicit human acceptance of ADR-0015 with the required additions; `docs/adr/index.md` updated accordingly.
43. Update this plan's "Status" section with final ✅/⬜ per milestone; write `docs/plans/active/PLAN-0003-worker-notes.md` summarising what's implemented vs. deferred for the next worker (PLAN-0004).

## Initial Permission Catalogue

| Permission code | Purpose | Seeded to roles |
|---|---|---|
| `organisations.manage` | Create/update organisations | `SystemAdmin` |
| `locations.manage` | Create/update locations within an organisation | `SystemAdmin`, `OrganisationOwner` |
| `terminals.manage` | Create/update terminals within a location | `SystemAdmin`, `OrganisationOwner`, `VenueManager` |
| `devices.manage` | Rotate/revoke device credentials, list devices | `SystemAdmin`, `OrganisationOwner`, `VenueManager`, `SupportAccess` |
| `devices.register` | Generate/rotate device registration PINs | `SystemAdmin`, `OrganisationOwner`, `VenueManager` |
| `staff.manage` | Create staff members, reset PIN, assign roles, disable staff | `SystemAdmin`, `OrganisationOwner`, `VenueManager` |
| `users.manage` | Create local manager/admin users, assign roles | `SystemAdmin`, `OrganisationOwner` |
| `sessions.manage` | Force-revoke another identity's active session | `SystemAdmin`, `OrganisationOwner`, `SupportAccess` |

`Staff` role receives none of the above — matching ADR-0013's explicit list of actions staff-PIN login must never authorize. This catalogue is intentionally minimal; later plans add their own permissions (`tax.manage` per OI-0007, `refunds.approve`, `reports.export`, etc.) as those modules are built — this plan does not pre-build the full eventual catalogue.

## Tests To Run Later

- `dotnet build DaxaPos.sln` (0 warnings, 0 errors) after each milestone.
- `dotnet test DaxaPos.sln` against a real Postgres container (matching PLAN-0002/CI's "no mocks" pattern) — all of `TenantIsolationTests`, `LocalUserLoginTests`, `DeviceRegistrationTests`, `StaffPinLoginTests`, `RbacTests`, `HybridOfflineLoginTests`, plus the `DaxaPos.UnitTests` hasher/permission tests.
- Manual/CI: `docker compose up -d db` only (Keycloak stopped) → run `HybridOfflineLoginTests` → confirm pass, proving no hidden Keycloak dependency.
- EF Core migrations apply cleanly in sequence on a fresh database (`dotnet ef database update` from empty).
- `.github/workflows/ci.yml` continues to pass with the added `DaxaPos.UnitTests` project (no new external service dependency needed for it — pure logic tests).

## Documentation To Update

- `docs/architecture/tenancy.md`
- `docs/architecture/security.md`
- `docs/architecture/multi-location.md`
- `docs/modules/devices.md`
- `docs/modules/audit.md` (confirm implemented `AuditEvent` fields match what's documented)
- ~~`docs/adr/index.md` (ADR-0015 listing, then move from Proposed to Accepted)~~ — done

## ADRs Required

- **ADR-0015 (Accepted 2026-07-01)** — tenant isolation mechanism and session token format, with explicit additions on Keycloak/OIDC separation, raw-credential-never-stored, fail-closed-if-missing-or-ambiguous, and context provenance (see its Acceptance Addendum).
- ADR-0003, ADR-0008, ADR-0013, ADR-0014 (all already accepted, unchanged, no amendment needed — see Architecture Assumptions on why ADR-0014's reference graph doesn't need to change).

## Open Issues Required

- None new. OI-0002, OI-0006, OI-0007, OI-0010 remain resolved/closed and are referenced, not reopened.

## Human Decisions Needed — Recorded Answers (2026-07-01)

All five were decided by explicit human approval; recorded here so later milestones don't need to re-derive them.

1. **ADR-0015 — Accepted**, with six explicit statements added before acceptance (POS staff login is not Keycloak/OIDC; POS staff sessions are operational sessions separate from admin/cloud identity; session tokens must never be stored raw; PINs must never be stored raw; tenant context fails closed if missing *or ambiguous*; tenant/location/terminal context must come from a registered terminal/session context, never arbitrary user-supplied free text — see ADR-0015's Acceptance Addendum and new §"Context provenance" subsection). Domain-event handlers with DB access may stay in `Api` for now, as proposed.
2. **Region/Country descoping — Approved.** `TenantId` is the only isolation key for PLAN-0003; no `Region`/`Country` entities or fields. Add later only when multi-region/global deployment actually needs it.
3. **PIN/session policy — Approved with corrected session-expiry model.** StaffCode + PIN (never the database primary key) for staff login; 4-digit minimum PIN; PBKDF2 hash, never plaintext; lockout after 5 failed attempts for 15 minutes. **Session expiry is two-part, not a single fixed value as originally drafted:** an absolute maximum of 12 hours from issuance (`AuthSession.ExpiresAtUtc`) *and* an 8-hour idle timeout tracked separately (`AuthSession.LastActivityAtUtc`, updated on each authenticated request; a session is rejected once idle time exceeds 8 hours, even if the 12-hour absolute cap hasn't been reached). This applies to `AuthSession` generally (both `LocalStaffPin` and `LocalUsernamePassword`) for MVP; per-tenant/per-location configurability is deferred, hardcoded defaults are acceptable for this plan. The Milestone C `AuthSession` field list is corrected to include `LastActivityAtUtc` accordingly. The role/permission snapshot captured at session creation is explicitly for audit purposes — later permission changes must not rewrite what a staff member was authorized to do at the time of a past action (this was already this plan's Domain Assumption; now explicitly confirmed).
4. **Bootstrap seeding — Approved**, dev/local-only, must not become an accidental production default. The seeded `SystemAdmin` password will be sourced from an environment variable with a documented local-dev default (not a hardcoded literal in migration source), and the bootstrap seed will be clearly commented as dev/local-only in code and in `docs/deployment/local.md`. No production admin credentials are ever seeded by this plan.
5. **Single-location `StaffMember` — Approved for MVP**, with an explicit forward-compatibility requirement: the schema must not preclude adding multi-location staff assignment later without a full model rewrite. Addressed by modelling `StaffMember.LocationId` as the *home* location (used for staff-PIN login's location-match check) while keeping `StaffMemberRole.LocationId` as its own nullable scope column (already planned in Milestone F) — multi-location access can later be added as additional `StaffMemberRole`-style assignment rows (a new `StaffMemberLocation` table) without changing `StaffMember` itself or any existing session/audit code, since sessions already key off `AuthSession.LocationId` (the location the staff member is *currently* operating at), not off `StaffMember.LocationId` directly.

## Commit Sequence

```
docs: propose ADR-0015 (tenant isolation and session token mechanism)
feat(identity): add AuthContext, tenant provider, and credential hashing ports
test(identity): add PBKDF2 and device-credential hasher unit tests
feat(tenancy): add TenantId columns and fail-closed EF Core global query filters
test(tenancy): add cross-tenant query filter isolation tests
feat(identity): add RBAC schema (roles, permissions, users) with seed data
feat(identity): add local username/password login and session authentication
feat(audit): add AuditEvent and domain-event audit handlers for login activity
test(identity): add local username/password login and audit tests
feat(tenancy): add organisation, location, and terminal endpoints
test(tenancy): add organisation/location/terminal authorization tests
feat(devices): add device registration PINs, device credentials, and rotation/revocation
test(devices): add device registration and device-token isolation tests
feat(identity): add staff members and staff ID/PIN login
feat(identity): add staff member emergency-disable path
test(identity): add staff PIN login, lockout, and emergency-disable tests
test(identity): add hybrid/offline login verification with Keycloak stopped
test(identity): add consolidated RBAC tests across both auth methods
docs: update tenancy, security, multi-location, and device docs for PLAN-0003
docs: close out PLAN-0003 status and write worker notes
```

## Handoff Notes

This plan depends on PLAN-0002 (Platform Skeleton, complete and committed as `0e41cd7`), which scaffolds the `IDomainEvent`/`IDomainEventDispatcher` abstraction that every domain event in this plan builds on, per ADR-0014.

**What changed in this rewrite (2026-07-01, second pass):** the previous version of this plan was architecture-level — correct on the ADR-0013 model, but not concrete enough to start coding from (no entity field lists, no migration names, no endpoint list, no test file names). This pass adds all of that, plus resolves three design questions the previous draft left implicit: (1) where domain-event handlers with DB access live, given the current project reference graph (resolved: `DaxaPos.Api`, no graph change — see ADR-0015 §3); (2) what the session token actually is (resolved: opaque, hashed, DB-validated — see ADR-0015 §2); (3) what "hybrid mode using synced data" means when PLAN-0007's sync mechanism doesn't exist yet (resolved: bounded to "the auth path has no hidden cloud dependency," verified directly in Milestone G, not simulated). Keycloak JWT wiring, which the previous draft's step 2 planned to implement, is now explicitly deferred (see Non-goals) since nothing in this plan's own endpoint list needs it — the previous draft would have built unused Keycloak middleware.

After completing this plan, the identity, tenancy, and device-registration foundation is in place with real RBAC enforcement. The next worker can implement the product catalogue (PLAN-0004) with proper tenant/location/authorization context available, including a real permission-catalogue pattern to extend (`tax.manage` per OI-0007 is the first one PLAN-0004 should add).
