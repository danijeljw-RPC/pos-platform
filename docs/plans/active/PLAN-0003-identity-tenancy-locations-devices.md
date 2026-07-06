# PLAN-0003 — Identity, Tenancy, Locations, and Devices

## Status

Approved 2026-07-01 — Milestone C complete. Human sign-off received on the plan and on [ADR-0015](../../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md) (now **Accepted**, moved from `proposed/` to `accepted/`), plus explicit decisions on Region/Country descoping, PIN/session policy defaults, dev-only bootstrap seeding, and single-home-location `StaffMember` for MVP — see "Human Decisions Needed" for the recorded answers.

Milestone D implemented and complete 2026-07-02. Human approved the planning pass with clarifications (`IsActive` excluded from the tenant query filter; list hides inactive by default; single `GET` may return inactive rows; deactivate/reactivate must find inactive rows) — see "Human Decisions Needed" entry dated 2026-07-02 and `PLAN-0003-worker-notes.md`'s Milestone D Report for the full implementation record. `dotnet build`/`dotnet test` clean (92/92), migrations verified from a fresh database. Committed as `c592b49`.

Milestone E approved 2026-07-02 with amendments (flat routes; `Device {credentialId}.{secret}` token format; verify-scan PIN validation; PIN revoke endpoint added; six audit events; no `Device.IsActive`; unknown-PIN attempts unaudited by design) — see "Human Decisions Needed" entry dated 2026-07-02 (Milestone E) and the worker notes' Milestone E Planning Pass.

Milestone E implemented and complete 2026-07-02. `dotnet build`/`dotnet test` clean (140/140), migration `AddDeviceCredentialsAndRegistrationPins` verified incrementally and from a fresh dropped database (all five migrations apply cleanly in sequence), Keycloak stopped throughout. See the worker notes' Milestone E Report. Committed as `06bebfe` (2026-07-02).

Milestone F approved 2026-07-02 with thirteen explicit amendments (flat routes + list endpoint; alphanumeric `StaffCode` 2–20 uppercase-normalised; PIN digits 4–10; trusted-device-context rules; staff sessions on a shorter 8h/30min expiry in `StaffSessionExpiryPolicy`; server-generated PIN reset; sensitive-permission login guard with a recorded follow-up on permission metadata; unknown-staff-code attempts audited; full test list) — see "Human Decisions Needed" entry dated 2026-07-02 (Milestone F).

Milestone F implemented and complete 2026-07-02. `dotnet build`/`dotnet test` clean (215/215), migration `AddStaffMembers` verified incrementally and from a fresh dropped database (all six migrations apply cleanly in sequence), Keycloak stopped throughout. See the worker notes' Milestone F Report. Committed as `585cd39` (2026-07-02), after human review and approval.

Milestone G approved 2026-07-03 as test-and-documentation-only (offline verification tests, consolidated RBAC matrix, `IgnoreQueryFilters()` guard test, smoke-test adoption, open issues OI-0011–OI-0015 for the Milestone C–F deferred risks; explicit decision that OI-0011 user-management endpoints is issue-only, not implemented) — see "Human Decisions Needed" entry dated 2026-07-03 (Milestone G). The preceding docs housekeeping/planning pass was committed first as `60dedbe`.

Milestone G implemented and complete 2026-07-03. `dotnet build`/`dotnet test` clean (371/371 — 156 new tests), no migrations added (fresh-database verification re-run: all six existing migrations apply cleanly), no `src/` file changed, Keycloak stopped throughout. See the worker notes' Milestone G Report. Committed as `fb8a8b8` (`test(identity): add hybrid offline and RBAC verification`) and `cca09b5` (`docs: record PLAN-0003 deferred risks as open issues`).

Milestone H (documentation-only closeout) approved and complete 2026-07-03. No source code, migrations, or tests changed. Consolidated `docs/architecture/security.md`/`tenancy.md`/`multi-location.md` and `docs/modules/devices.md`/`audit.md` with final implementation-status rollups; corrected stale Milestone G commit-status wording (above); created `docs/issues/open/OI-0016-define-completed-plan-archival-convention.md` (explicit human decision: PLAN-0003 stays in `docs/plans/active/`, not moved to `completed/`); added a `docs/CHANGELOG.md` entry for PLAN-0003; wrote the PLAN-0003 → PLAN-0004 handoff section in the worker notes. See the worker notes' Milestone H Report. **PLAN-0003 is complete, in place, under `docs/plans/active/`.**

- [x] Milestone A — Auth primitives (`AuthMethod`, `ICurrentTenantProvider`, `AuthContext`, hashers, token service, `DaxaPos.UnitTests` project) — complete, committed as `23f89c8`. See PLAN-0003-worker-notes.md for the report.
- [x] Milestone B — Tenant isolation (`TenantId` on `Location`/`Device`/`Terminal`, fail-closed global query filters on `Organisation`/`Location`/`Device`/`Terminal`, migration `AddTenantIsolationColumns`, cross-tenant isolation tests) — complete, committed as `ba7e237`. See PLAN-0003-worker-notes.md for the Milestone B report.
- [x] Milestone C — RBAC schema (`Role`/`Permission`/`RolePermission`/`User`/`UserRole`), local username/password login (`/auth/local/login`, `/auth/logout`, `/auth/me`), `AuthSession`, `AuditEvent` + audit plumbing, dev-only bootstrap admin seeding, migration `AddIdentityAndRbacCore` — complete, see PLAN-0003-worker-notes.md for the Milestone C report.
- [x] Milestone D — Organisation / Location / Terminal endpoints — complete, committed as `c592b49` (2026-07-02). See PLAN-0003-worker-notes.md for the Milestone D report.
- [x] Milestone E — Device registration and credentials — complete, committed as `06bebfe` (2026-07-02). See PLAN-0003-worker-notes.md for the Milestone E report.
- [x] Milestone F — StaffMember and staff PIN login — complete, committed as `585cd39` (2026-07-02). See PLAN-0003-worker-notes.md for the Milestone F report.
- [x] Milestone G — Local/offline verification and RBAC consolidation — complete, committed as `fb8a8b8`/`cca09b5` (2026-07-03). See PLAN-0003-worker-notes.md for the Milestone G report.
- [x] Milestone H — Documentation and plan closeout — complete (2026-07-03), documentation-only. See PLAN-0003-worker-notes.md for the Milestone H report.

**PLAN-0003 is complete as of Milestone H (2026-07-03), in place under `docs/plans/active/`** (not moved to `docs/plans/completed/` — see `docs/issues/open/OI-0016-define-completed-plan-archival-convention.md` for the deferred archival-convention question).

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

```text
docs/adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md   (accepted 2026-07-01)
docs/adr/index.md                                                            (ADR-0015 listed under Accepted)

src/DaxaPos.Domain/
  Entities/User.cs, StaffMember.cs, Role.cs, Permission.cs, RolePermission.cs,
           UserRole.cs, StaffMemberRole.cs, AuthSession.cs, DeviceCredential.cs,
           DeviceRegistrationPin.cs, AuditEvent.cs                            (new)
  Entities/Location.cs, Device.cs, Terminal.cs                               (add TenantId)
  Entities/Organisation.cs, Location.cs, Terminal.cs                         (Milestone D: add IsActive)
  Enums/AuthMethod.cs                                                        (new)
  Tenancy/ICurrentTenantProvider.cs                                          (new)
  Events/*DomainEvent.cs (10 new event records — see Implementation Steps)
  Events/OrganisationLifecycleDomainEvent.cs, LocationLifecycleDomainEvent.cs,
         TerminalLifecycleDomainEvent.cs                                     (Milestone D, new)

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
  Configurations/OrganisationConfiguration.cs, LocationConfiguration.cs,
                 TerminalConfiguration.cs                                   (Milestone D: IsActive column)
  DaxaDbContext.cs                                                         (ctor takes ICurrentTenantProvider; global query filters; new DbSets)
  Migrations/ (5 new migrations — see Implementation Steps)
  Migrations/AddIsActiveToOrganisationLocationTerminal                     (Milestone D, new)

src/DaxaPos.Api/
  Authentication/DeviceTokenAuthenticationHandler.cs, SessionAuthenticationHandler.cs (new)
  Authorization/RequirePermissionAttribute.cs or endpoint filter                       (new)
  Authorization/RequirePermissionFilter.cs                                            (Milestone D: add rejectStaffPin)
  Audit/DomainEventAuditHandlers.cs                                                    (new)
  Audit/DomainEventAuditHandlers.cs                                                    (Milestone D: add 3 lifecycle handlers)
  Endpoints/Identity/*.cs (organisations, locations, terminals, devices,
             device-registration, staff-members, users, auth, sessions)               (new)
  Endpoints/Identity/OrganisationEndpoints.cs, LocationEndpoints.cs,
             TerminalEndpoints.cs                                                     (Milestone D, new)
  Program.cs                                                                          (wire everything up)

tests/DaxaPos.Api.Tests/
  TenantIsolationTests.cs, StaffPinLoginTests.cs, LocalUserLoginTests.cs,
  DeviceRegistrationTests.cs, RbacTests.cs, HybridOfflineLoginTests.cs                 (new)
  OrganisationEndpointsTests.cs, LocationEndpointsTests.cs,
  TerminalEndpointsTests.cs                                                           (Milestone D, new)
  Support/RbacTestSeeder.cs                                                           (Milestone D, new)
  RequirePermissionFilterTests.cs                                                     (Milestone D: add rejectStaffPin cases)

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

### Milestone B — Tenant isolation (complete 2026-07-01)

1. ✅ Add `TenantId` (Guid, required) to `Location`, `Device`, `Terminal` entities and their EF configurations (indexed FK to `Tenant`).
2. ✅ Update `DaxaDbContext` constructor to accept `ICurrentTenantProvider`; add fail-closed global query filters for `Location`, `Device`, `Terminal` in `OnModelCreating` — **and also `Organisation`**, which already had `TenantId` from PLAN-0002 but no filter yet (an addition beyond this step's original wording, made to satisfy "ensure TenantId is applied consistently to tenant-owned entities"; `Tenant` itself remains unfiltered as the isolation root).
3. ✅ Create migration `AddTenantIsolationColumns`. No backfill logic needed — verified the `db` container's tables were actually empty (in fact the InitialCreate migration had never been applied to that particular fresh Docker volume; both migrations were applied together via `dotnet ef database update`). Verified against a real Postgres container (`psql \d locations/devices/terminals` confirming columns, indexes, and FKs), matching PLAN-0002's verification style.
4. ✅ Add `TenantIsolationTests.cs` to `DaxaPos.Api.Tests` (plus a `Support/FakeCurrentTenantProvider.cs` test double): 6 tests covering same-tenant visibility, cross-tenant invisibility (`Location` and `Organisation`), missing-tenant-context invisibility (`Location` and `Organisation`), and a list query that only returns the caller's tenant's rows when two tenants both have data. All written test-first (RED via compile failure, since `TenantId`/the new constructor didn't exist yet) then implemented to GREEN.

### Milestone C — RBAC schema, seed data, local username/password login, audit plumbing (complete 2026-07-02)

1. ✅ Added entities: `Role`, `Permission`, `RolePermission` (system-wide, not tenant-owned, no filter), `User`, `UserRole`, `AuthSession`, `AuditEvent` (all four tenant-owned, `TenantId` + fail-closed filter, matching the Milestone B pattern) — exactly as planned, plus two pure-logic classes not originally itemised here: `LoginLockoutPolicy` (5 attempts / 15 min) and `SessionExpiryPolicy` (12h absolute / 8h idle), extracted for independent unit testing rather than left as inline magic numbers in the endpoint handler.
2. ✅ Seeded `Role`/`Permission`/`RolePermission` via `HasData` exactly as planned (deterministic GUIDs in `Persistence/Seed/RbacSeedIds.cs`). **Deviation:** rather than splitting bootstrap seeding between `HasData` (Tenant/Organisation) and a startup routine (User only), all of Tenant + Organisation + User + UserRole are created together in one idempotent startup routine (`BootstrapAdminSeeder.SeedAsync`, called before `app.Run()`) — avoids duplicating a magic GUID between migration-baked `HasData` and runtime code. Requires both `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`DAXA_BOOTSTRAP_ADMIN_PASSWORD`; if either is unset, seeding is skipped (logged) and the app still starts — it does not refuse to start, since that would make the app unable to run at all once bootstrap env vars are deliberately unset after a real admin exists. Idempotent: skips (does not overwrite) if a user with that email already exists. Documented in `deploy/.env.example` and `docs/deployment/local.md`.
3. ✅ Created migration `AddIdentityAndRbacCore`. Applied and verified against a freshly dropped/recreated database (`dotnet ef database drop --force` + `dotnet ef database update`), plus a `psql` check confirming seeded role/permission/role_permission row counts match the catalogue table below exactly.
4. ✅ Added domain events: `LocalUserLoginSucceededDomainEvent` (`TenantId`, `OrganisationId`, `UserId`, `AuthSessionId`, `OccurredAtUtc`), `LocalUserLoginFailedDomainEvent` (`TenantId`, `OrganisationId`, `UserId`, `FailureReason`, `OccurredAtUtc`), `AuthSessionRevokedDomainEvent`. **Deviation from this step's original field sketch:** `LocalUserLoginFailedDomainEvent` has no `AttemptedEmail` field and is **only raised when a real `User` was matched** (wrong password, inactive, or locked out) — an unknown email returns 401 with no domain event/audit row at all, since there is no tenant to attach an audit row to and `AuditEvent.TenantId` is non-nullable by design. Verified by `UnknownEmail_DoesNotWriteAnyAuditEventRow`.
5. ✅ Implemented `POST /api/v1/auth/local/login` — reuses `IPinHasher`'s PBKDF2 mechanism for passwords (no second hashing scheme), issues an `AuthSession` (`AuthMethod = LocalUsernamePassword`), returns the opaque token + role/permission snapshot. Raises the domain events above.
6. ✅ Implemented `SessionAuthenticationHandler` exactly as planned, including the two-part expiry check via `SessionExpiryPolicy` and updating `LastActivityAtUtc` on every successful validation.
7. **Partially implemented, scope adjusted:** `RequirePermissionFilter` checks `AuthContext.Permissions.Contains(permissionCode)` and returns 401 (no `AuthContext`) / 403 (missing permission) / calls `next` (authorized). **The `rejectStaffPin` parameter described in the original plan is not yet implemented** — there is no `StaffMember`/`LocalStaffPin` session to reject yet (Milestone F), so building that check now would be untestable dead code. It will be added in Milestone F alongside staff PIN login, applied retroactively to every sensitive endpoint gated by `RequirePermission`, per this plan's Architecture Assumptions. Unit-tested directly (no HTTP round-trip) in `RequirePermissionFilterTests.cs`.
8. ✅ Implemented `Api/Audit/DomainEventAuditHandlers.cs`: handlers for all three domain events above, each writing one `AuditEvent` row via injected `DaxaDbContext`. Registered in `Program.cs`.
9. ✅ Implemented `POST /api/v1/auth/logout` (revokes the calling session's `AuthSession`, raises `AuthSessionRevokedDomainEvent`).
10. ✅ Added `LocalUserLoginTests.cs` (12 tests) to `DaxaPos.Api.Tests`, covering everything the step asked for plus a permanent `GET /api/v1/auth/me` endpoint (built specifically to serve as the "protected endpoint" this step needed, rather than a throwaway test-only route) and explicit `AuditEvent` assertions for both success and failure, plus confirmation that an unknown email writes no audit row at all.

### Milestone D — Organisation / Location / Terminal endpoints

**Revised 2026-07-02** (planning pass, no code written yet): expanded from the original 8 create/read-only endpoints to non-destructive full CRUD (create, read, rename, deactivate, reactivate — never hard delete) per the human's explicit Milestone D scope decision recorded below. Routes are flat (not nested under a parent segment); the parent id is a request-body field, cross-checked against `AuthContext`, never a path segment used for scoping.

1. **`rejectStaffPin` pulled forward from Milestone F.** Add a `rejectStaffPin` parameter to `RequirePermissionFilter`'s constructor and the `RequirePermission` extension method (default `false`, so the three existing Milestone C call sites — none of which set it — keep their exact current behaviour with zero code changes at those sites). When `true`, the filter returns 403 if `AuthContext.AuthMethod == AuthMethod.LocalStaffPin`, evaluated independently of the permission-code check (so a misconfigured role that happened to grant a sensitive permission to a staff-PIN session is still blocked — defense in depth, per ADR-0013's "must not be used for... editing user permissions" and the plan's own Architecture Assumptions). No `StaffMember`/staff-PIN login exists yet (Milestone F), so this is proven with a unit test that constructs an `AuthContext` with `AuthMethod.LocalStaffPin` directly (the same technique `RequirePermissionFilterTests.cs` already uses to construct contexts without a real HTTP login) — not an end-to-end staff-PIN HTTP test, which is impossible until Milestone F exists. TDD: extend `RequirePermissionFilterTests.cs` first (RED — the constructor doesn't accept the parameter yet), then implement.
2. **Add `IsActive` (`bool`, required, default `true`) to `Organisation`, `Location`, `Terminal`** plus their EF configurations (`HasDefaultValue(true)`). Unlike the Milestone B `TenantId` column (whose `Guid.Empty` migration default was an inert placeholder because the table was verified empty), `true` here is the actually-correct value for every pre-existing row — no backfill concern. Create migration `AddIsActiveToOrganisationLocationTerminal`. Apply against the dev database and verify with `psql \d organisations/locations/terminals`, matching the verification style of every prior migration in this plan.
3. **Add three lifecycle domain events**, one per entity rather than one per action (four actions × three entities = twelve near-identical event classes was rejected as unnecessary repetition — see "Design Decisions" below): `OrganisationLifecycleDomainEvent(Guid TenantId, Guid OrganisationId, Guid? UserId, string Action, string? BeforeValue, string? AfterValue, DateTimeOffset OccurredAtUtc)`, `LocationLifecycleDomainEvent(Guid TenantId, Guid OrganisationId, Guid LocationId, Guid? UserId, string Action, string? BeforeValue, string? AfterValue, DateTimeOffset OccurredAtUtc)`, `TerminalLifecycleDomainEvent(Guid TenantId, Guid OrganisationId, Guid LocationId, Guid TerminalId, Guid? UserId, string Action, string? BeforeValue, string? AfterValue, DateTimeOffset OccurredAtUtc)`. `Action` is one of `"Created"`, `"Updated"`, `"Deactivated"`, `"Reactivated"`. Add three corresponding `Api/Audit` handlers (`OrganisationLifecycleAuditHandler`, etc.) that write one `AuditEvent` row each, with `EventType` built as `$"{EntityType}{Action}"` (e.g. `"OrganisationCreated"`, `"LocationDeactivated"`) — consistent with the existing `"LocalUserLoginSucceeded"`-style naming. Register in `Program.cs` alongside the Milestone C handlers.
4. **Implement `src/DaxaPos.Api/Endpoints/Identity/OrganisationEndpoints.cs`** (`app.MapGroup("/api/v1/organisations")`), gated `organisations.manage` + `rejectStaffPin: true` on every route:
    - `POST /` — body `CreateOrganisationRequest(string Name, Guid? TenantId = null)`. 400 if `Name` is null/whitespace. **400 if `TenantId` is non-null** (explicit rejection, not silent ignoring — see Human Decisions Needed). Creates with `TenantId = AuthContext.TenantId`, `IsActive = true`. Raises `OrganisationLifecycleDomainEvent(Action: "Created")`. Returns 201 with `OrganisationResponse(Guid Id, Guid TenantId, string Name, bool IsActive, DateTimeOffset CreatedAtUtc)`.
    - `GET /` — lists all organisations visible under the tenant-filtered `Organisations` `DbSet` (no further `AuthContext.OrganisationId` restriction — see "Design Decisions" on why Organisation-level endpoints scope to tenant, not to the caller's own organisation).
    - `GET /{organisationId}` — 404 if not found under the tenant filter.
    - `PATCH /{organisationId}` — body `UpdateOrganisationRequest(string Name, Guid? TenantId = null)`; same 400 rules as create; 404 if not found; raises `OrganisationLifecycleDomainEvent(Action: "Updated", BeforeValue/AfterValue = old/new name)`.
    - `POST /{organisationId}/deactivate`, `POST /{organisationId}/reactivate` — no body; 404 if not found; flips `IsActive`; raises the matching lifecycle event. Idempotent (deactivating an already-inactive organisation is not an error — no complex lifecycle rules per the human's explicit instruction).
5. **Implement `src/DaxaPos.Api/Endpoints/Identity/LocationEndpoints.cs`** (`app.MapGroup("/api/v1/locations")`), gated `locations.manage` + `rejectStaffPin: true`:
    - `POST /` — body `CreateLocationRequest(string Name, Guid OrganisationId, Guid? TenantId = null)`. 400 rules as above. **404 (not 400/403) if `OrganisationId != AuthContext.OrganisationId`** — the literal application of ADR-0015's Context Provenance example, and consistent with the project's "fail closed, don't confirm existence" convention. Creates with `TenantId = AuthContext.TenantId`.
    - `GET /` — lists locations where `OrganisationId == AuthContext.OrganisationId` (tenant filter applies automatically to the underlying `DbSet`).
    - `GET /{locationId}`, `PATCH /{locationId}`, `POST /{locationId}/deactivate`, `POST /{locationId}/reactivate` — fetch via the tenant-filtered `DbSet` (404 if absent), then 404 if `location.OrganisationId != AuthContext.OrganisationId`. Same rename/toggle/audit shape as Organisation.
6. **Implement `src/DaxaPos.Api/Endpoints/Identity/TerminalEndpoints.cs`** (`app.MapGroup("/api/v1/terminals")`), gated `terminals.manage` + `rejectStaffPin: true`:
    - `POST /` — body `CreateTerminalRequest(string Name, Guid LocationId, Guid? TenantId = null)`. Fetch the referenced `Location` via the tenant-filtered `DbSet` (404 if absent), then 404 if `location.OrganisationId != AuthContext.OrganisationId`. Creates with `TenantId = AuthContext.TenantId`.
    - `GET /` — joins `Terminals` to `Locations` (no navigation property exists on `Terminal` today — see `TerminalConfiguration.cs` — so this is an explicit LINQ join, not `.Include()`) filtering on `Location.OrganisationId == AuthContext.OrganisationId`.
    - `GET /{terminalId}`, `PATCH /{terminalId}`, `POST /{terminalId}/deactivate`, `POST /{terminalId}/reactivate` — fetch `Terminal` (tenant filter, 404 if absent) → fetch its `Location` (tenant filter) → 404 if `location.OrganisationId != AuthContext.OrganisationId`. Same rename/toggle/audit shape.
7. Wire `MapOrganisationEndpoints()`, `MapLocationEndpoints()`, `MapTerminalEndpoints()` in `Program.cs`, alongside the existing `MapAuthEndpoints()`.
8. Add `tests/DaxaPos.Api.Tests/Support/RbacTestSeeder.cs` — a shared helper (avoids near-duplicating the same Tenant/Organisation/User/UserRole/login-token setup three times) that seeds a `Tenant` + `Organisation` + `User`, assigns a named seeded role (looked up by `Role.Name`, e.g. `"SystemAdmin"` or `"OrganisationOwner"`, from the Milestone C `HasData` catalogue — not `RbacSeedIds` directly, since that class is `internal` to `DaxaPos.Persistence` and not visible to the test project), logs in via `POST /api/v1/auth/local/login`, and returns the bearer token plus the seeded `TenantId`/`OrganisationId`. Add `OrganisationEndpointsTests.cs`, `LocationEndpointsTests.cs`, `TerminalEndpointsTests.cs` (per-entity, matching the one-file-per-feature convention already used by `LocalUserLoginTests.cs`) covering: happy-path create/list/get/update/deactivate/reactivate; 400 on client-supplied `TenantId` (create and update); 403 when the caller's role lacks the permission; 404 for a different tenant's row; 404 for a *different organisation in the same tenant*'s row (Location/Terminal only — this is the case that specifically exercises the `AuthContext.OrganisationId` cross-check independently of tenant filtering, so it needs two organisations seeded under one shared tenant); an `AuditEvent` row is written for create/update/deactivate/reactivate. Extend `RequirePermissionFilterTests.cs` with: `rejectStaffPin: true` + `AuthMethod.LocalStaffPin` (even with the matching permission) → 403; `rejectStaffPin: true` + `AuthMethod.LocalUsernamePassword` + matching permission → calls next (proves no regression to current admin sessions); `rejectStaffPin: false` (default) + `AuthMethod.LocalStaffPin` + matching permission → calls next (proves the flag is opt-in, matching the three untouched Milestone C call sites).

#### Design Decisions (Milestone D, recorded before implementation)

- **Never hard-delete.** Matches the codebase's own established convention (`AuthSession.RevokedAtUtc`, `User.IsActive`/`LockedOutUntilUtc`) of marking rows inactive/revoked rather than removing them — these three entities will accumulate references from audit logs, sessions, devices, and (later) orders/payments/reports/sync data, so an `IsActive` flag with dedicated deactivate/reactivate endpoints was chosen over hard `DELETE` (which would also fail unpredictably via `OnDelete: Restrict` FK violations the moment a row has any children). Explicit human decision, recorded 2026-07-02.
- **`IsActive` is deliberately excluded from the tenant-isolation global query filter** — tenant isolation and lifecycle visibility are separate concerns (explicit human clarification, 2026-07-02). List endpoints (`GET /api/v1/organisations`, etc.) filter to `IsActive == true` in their own query, in application code, not via a second global filter. Single-record `GET`, `PATCH`, `deactivate`, and `reactivate` never filter on `IsActive` — a manage-permission caller can look up, rename, or reactivate an inactive row directly by id (deactivate/reactivate could not otherwise find their own target once it's inactive).
- **Client-supplied `TenantId` is rejected with 400, not silently ignored.** ADR-0015's Context Provenance rule allows either "ignored or rejected"; the human chose explicit rejection so a caller who sends a stray/malicious `tenantId` field gets clear, auditable feedback rather than a silently-dropped field that might mask a client bug.
- **Organisation-level endpoints scope to the caller's tenant, not to `AuthContext.OrganisationId`.** `organisations.manage` is granted only to `SystemAdmin` in the Initial Permission Catalogue, and the schema allows a tenant to own more than one `Organisation` (each has its own `TenantId`, not derived from a 1:1 relationship) — e.g. a `SystemAdmin` managing a holding-company tenant with multiple trading entities. Restricting Organisation endpoints to the caller's own `AuthContext.OrganisationId` would make a `SystemAdmin` unable to see or manage a second organisation they had just created. Location/Terminal endpoints, by contrast, are also granted to `OrganisationOwner`/`VenueManager` — roles that are legitimately scoped to exactly one organisation — so those endpoints apply the stricter `AuthContext.OrganisationId` cross-check ADR-0015 §Context Provenance illustrates. This is the plan's own interpretation of an accepted ADR's example, not a new ADR-level decision, but it is flagged here explicitly in case the human disagrees and wants Organisation endpoints scoped the same way.
- **One domain event per entity (carrying an `Action` field), not one per action.** Twelve near-identical event classes (`OrganisationCreatedDomainEvent`, `OrganisationUpdatedDomainEvent`, ... × 3 entities) was judged excessive repetition versus three classes with an `Action` discriminator; this is a narrower, more targeted abstraction than a fully generic cross-entity lifecycle event (which no other milestone uses), so it stays consistent with this plan's existing per-domain, explicitly-named event style (`LocalUserLoginSucceededDomainEvent`, etc.) while avoiding the twelve-class blowup.
- **Routes are flat** (`POST /api/v1/locations`, not `POST /api/v1/organisations/{organisationId}/locations`), per explicit human instruction — a deviation from the original step 22's nested route sketch. The parent id (`OrganisationId` on `CreateLocationRequest`, `LocationId` on `CreateTerminalRequest`) is a body field, not a path segment, and is cross-checked against `AuthContext` exactly the same way regardless of where in the request it appears.
- **`PATCH` replaces the whole `Name` field**, not a partial merge-patch — `Name` is required (400 if blank) on `UpdateOrganisationRequest`/`UpdateLocationRequest`/`UpdateTerminalRequest`. No other fields are editable yet ("do not add complex lifecycle rules yet," per the human's explicit instruction) — re-parenting (moving a `Location` to a different `Organisation`, a `Terminal` to a different `Location`) is out of scope for this milestone.
- **Create returns 201 with a `Location` header**, a small correctness improvement over the Milestone C login endpoint's `200 OK` (which is an action, not a resource creation) — idiomatic REST for these actual resource-creation endpoints.

### Milestone E — Device registration and credentials

**Revised 2026-07-02** (planning pass, approved with amendments — see "Human Decisions Needed" entry dated 2026-07-02 (Milestone E) and the worker notes' Milestone E Planning Pass): flat routes replace the original nested sketch (consistent with Milestone D's convention); the device token format is corrected to `Device {credentialId}.{secret}` (the original "hash it and look it up" cannot work with a per-record salted hash); PIN validation is a verify-scan over live PINs; a PIN-revoke endpoint and two extra audit events are added; `DeviceCredential.Salt` is dropped (salt is embedded in the hash string).

1. Add entities: `DeviceCredential` (`Id`, `TenantId`, `DeviceId`, `CredentialHash` — salted HMAC via `IDeviceCredentialHasher`, salt embedded in the hash string, no separate `Salt` column — `Status` enum `DeviceCredentialStatus` `Active/Retired/Revoked`, `IssuedAtUtc`, `RotatedAtUtc?`, `RevokedAtUtc?`), `DeviceRegistrationPin` (`Id`, `TenantId`, `OrganisationId`, `LocationId`, `PinHash`, `ExpiresAtUtc`, `MaxUses`, `UsedCount`, `CreatedByUserId`, `CreatedAtUtc`, `RevokedAtUtc?`). Both tenant-owned with fail-closed filters. **No `Device` schema changes** — revocation state lives on credentials; a revoked/lost device re-registers as a new `Device` per ADR-0008. Policy values in `DeviceRegistrationPinPolicy` (Application, `LoginLockoutPolicy` pattern): 6-digit PIN, 15-minute expiry, `MaxUses` default 1, allowed range 1–10, registration rate limit 10 attempts/minute per remote IP.
2. Create migration `AddDeviceCredentialsAndRegistrationPins`. Apply and verify (incremental + fresh-database).
3. Add domain events (six, per the approved amendment): `DeviceRegistrationPinCreatedDomainEvent`, `DeviceRegistrationPinRevokedDomainEvent`, `DeviceRegisteredDomainEvent`, `DeviceRegistrationFailedDomainEvent` (raised only when the matching PIN row resolves unambiguously — an unknown PIN has no tenant for the non-nullable `AuditEvent.TenantId`, per the Milestone C unknown-email precedent), `DeviceCredentialRotatedDomainEvent`, `DeviceRevokedDomainEvent`; corresponding `Api/Audit` handlers.
4. Implement `POST /api/v1/device-registration-pins` (`devices.register`, `rejectStaffPin: true`) — body `(LocationId, MaxUses?, TenantId?)`; 400 on non-null `TenantId` or out-of-range `MaxUses`; 404 if the location doesn't resolve under the caller's tenant + organisation; generates and returns the raw 6-digit PIN **once** plus expiry. Also `POST /api/v1/device-registration-pins/{pinId}/revoke` (same gate) so an accidentally created PIN can be revoked before expiry.
5. Implement `POST /api/v1/device-registration` (anonymous, rate-limited via the built-in ASP.NET Core rate limiter, 429 on excess): verify-scans candidate PIN rows via the documented `IgnoreQueryFilters()` bootstrap path (no tenant context exists yet); only a single live (unexpired, unrevoked, uses-remaining) match registers — more than one live match fails closed per ADR-0015's ambiguity rule; creates `Device` + `Active` `DeviceCredential`; increments `UsedCount` only after successful registration; returns the device identity fields and the raw credential secret **once**. Tenant/organisation/location derive exclusively from the matched PIN row.
6. Implement `DeviceTokenAuthenticationHandler` (scheme `DeviceToken`, header `Authorization: Device {credentialId}.{secret}`): loads the credential by id (bootstrap `IgnoreQueryFilters()`), constant-time-verifies the secret against the stored salted hash, requires `Status == Active`, resolves `Device`/`Location` server-side, and builds a partial `AuthContext` (`TenantId`, `OrganisationId`, `LocationId`, `DeviceId` populated; `UserId`/`StaffMemberId` null; `Roles`/`Permissions` **empty**) with `AuthMethod = DeviceToken`. A default policy scheme forwards `Bearer …` → `Session` and `Device …` → `DeviceToken` so both work on `.RequireAuthorization()` endpoints.
7. Implement `POST /api/v1/devices/{deviceId}/rotate-credential` and `POST /api/v1/devices/{deviceId}/revoke` (`devices.manage`, `rejectStaffPin: true`) and `GET /api/v1/devices?locationId=` — each resolves `Device → Location → OrganisationId` and returns 404 for cross-tenant or cross-organisation access. Rotation retires the old credential immediately and returns the new secret once; revoke revokes all of the device's credentials (terminal — the device must re-register).
8. Add tests per the approved required-tests list: `DeviceRegistrationPinPolicyTests` (unit), plus `DeviceRegistrationPinEndpointsTests.cs`, `DeviceRegistrationTests.cs`, `DeviceEndpointsTests.cs` (HTTP) — PIN issuance/revocation, raw PIN/secret never persisted, `TenantId` rejection, permission/`rejectStaffPin` gating, MaxUses/expiry/revoked/exhausted/unknown/ambiguous PIN outcomes and their audit behaviour, rate-limit 429, device-token authentication with zero permissions (403 on every permission-gated endpoint), rotation/revocation lifecycles, and cross-tenant/cross-organisation 404s.

### Milestone F — StaffMember and staff PIN login

**Revised 2026-07-02** (planning pass, approved with amendments — see "Human Decisions Needed" entry dated 2026-07-02 (Milestone F) and the worker notes' Milestone F Planning Pass): flat routes replace the original nested sketch; a list endpoint is added; `StaffCode` is alphanumeric (2–20, uppercase-normalised), not numeric-only; staff PIN sessions get their own shorter expiry policy (8h absolute / 30min idle) instead of reusing `SessionExpiryPolicy`; PIN reset is server-generated (raw PIN returned once), never admin-supplied; unknown staff-code attempts are audited (tenant known from device context); staff creation/PIN reset/role assignment are audited via a lifecycle event.

1. Add entities: `StaffMember` (`Id`, `TenantId`, `OrganisationId`, `LocationId` — home location — `StaffCode`, `DisplayName`, `PinHash`, `FailedPinAttempts`, `LockedOutUntilUtc?`, `IsActive`, `LinkedUserId?`, `CreatedAtUtc`), `StaffMemberRole` (`StaffMemberId`, `RoleId`, `TenantId`, `LocationId?`). Both tenant-owned with fail-closed filters. Unique index on `(OrganisationId, StaffCode)`. `StaffCode` rules: human-enterable, uppercase letters and digits only, 2–20 chars, normalised to uppercase before storing/comparing (uniqueness is per organisation — the same code may exist under different organisations). PIN rules: digits only, 4–10, hashed via the existing `Pbkdf2PinHasher`, never stored raw. Policy values in `StaffCodePolicy`/`StaffPinPolicy`/`StaffSessionExpiryPolicy` (Application, `LoginLockoutPolicy` pattern); staff PIN sessions use **8h absolute / 30min idle** expiry (operational POS sessions are shorter-lived than back-office sessions — Decision 6).
2. Create migration `AddStaffMembers` — also adds the deferred `auth_sessions.staff_member_id` FK now that the table exists (per the comment left on `AuthSession` in Milestone C). Apply and verify (incremental + fresh-database).
3. Add domain events: `StaffMemberLifecycleDomainEvent` (`Action`: `"Created"` / `"PinReset"` / `"RoleAssigned"` — audit `EventType` is `$"StaffMember{Action}"`), `StaffPinLoginSucceededDomainEvent`, `StaffPinLoginFailedDomainEvent`, `StaffMemberDisabledDomainEvent`; corresponding audit handlers. Unknown staff-code login attempts **are** audited (`Reason = "UnknownStaffCode"`, `StaffMemberId = null`) — a deliberate departure from the unknown-email/unknown-PIN precedents, justified because the trusted device context supplies the tenant. The client always receives a generic failure — never which of staff code or PIN was wrong.
4. Implement flat-route staff management endpoints (`staff.manage`, `rejectStaffPin: true` on all): `POST /api/v1/staff-members` (body carries `LocationId`; organisation derives from the resolved location and is cross-checked against `AuthContext.OrganisationId`, 404 on mismatch; duplicate `StaffCode` in the organisation → 409; client-supplied `TenantId` → 400), `GET /api/v1/staff-members?locationId=`, `GET /api/v1/staff-members/{id}`, `POST /api/v1/staff-members/{id}/reset-pin` (**server-generates** a new PIN, returns the raw PIN once, stores only the hash, clears `FailedPinAttempts`/`LockedOutUntilUtc`, revokes the staff member's active sessions — Decision 10), `POST /api/v1/staff-members/{id}/roles` (assigns a catalogue role, optional `LocationId` scope).
5. Implement `POST /api/v1/auth/staff-pin/login`: requires a valid `DeviceToken` `AuthContext` (anonymous → 401; a `Bearer` admin/session token → 403); body `{ locationId, staffCode, pin }` (client-supplied `TenantId` → 400); `locationId` must match the device's `AuthContext.LocationId` **and** the staff member's home `LocationId`; validates `staffCode` (uppercase-normalised) + `pin` (PBKDF2 verify, `LoginLockoutPolicy` lockout on repeated failure); issues an `AuthSession` (`AuthMethod = LocalStaffPin`, tenant/org/location/device from the device context, `StaffMemberId` set, `UserId`/`TerminalId` null, role/permission snapshot captured once — Decision 7). Rejects (generic 401, audited configuration error) if the resulting permission set includes any code in `Permissions.AdminSensitive` — defense-in-depth beneath the endpoint-level `rejectStaffPin` net. **Follow-up recorded (Decision 8):** permission metadata/category should later define staff-PIN eligibility; the hard-coded current-catalogue list is temporary. **Deferred (Decision 5):** `TerminalId` stays null until device→terminal mapping exists; staff sessions attach `TerminalId` later.
6. Implement `POST /api/v1/staff-members/{id}/disable` (`staff.manage`, `rejectStaffPin: true`) — sets `IsActive = false`, revokes all of that staff member's active `AuthSession` rows, raises `StaffMemberDisabledDomainEvent` — this is the "emergency disable" path from ADR-0013's hybrid deployment behaviour section.
7. Add `StaffMemberEndpointsTests.cs` + `StaffPinLoginTests.cs` covering the full approved test list (Decision 13): create/get/list happy paths; client-supplied `TenantId` 400; bad `StaffCode`/PIN format 400; duplicate `StaffCode` 409 (and same code allowed across organisations); missing-permission 403; cross-tenant/cross-organisation 404; reset-PIN returns the raw PIN once, never stores it, clears lockout, revokes sessions; role assignment reflected at next login's snapshot; correct code+PIN on a trusted device succeeds; wrong PIN increments/locks out after 5; correct PIN while locked out rejected; unknown staff-code audited with device context; wrong-location device and body-`locationId` mismatch rejected; no device token → 401; `Bearer` admin session → 403; inactive staff rejected; disable revokes sessions immediately; a real `LocalStaffPin` session gets 403 on every `rejectStaffPin: true` endpoint (including a directly-seeded session carrying a misconfigured sensitive permission snapshot); sensitive-permission role blocks login; audit rows for create/PIN-reset/role-assignment/login-success/login-failure/disable; all with Keycloak stopped.

### Milestone G — Local/offline verification and RBAC consolidation

**Revised 2026-07-03** (planning pass, approved): confirmed as test-and-documentation-only — no entities, no migrations, no endpoints, no product-code behaviour changes. Two additions beyond the original steps 39–40: the `IgnoreQueryFilters()` source-scan guard test (implementing ADR-0015 §Risks' request that tests police the escape hatch) and the promotion of the Milestone C–F deferred risks to open issues OI-0011–OI-0015. `docs/testing/local-smoke-test.md` (human-authored manual walkthrough, committed with the planning pass as `60dedbe`) is adopted as the manual companion to these tests.

1. ✅ Add `HybridOfflineLoginTests.cs`: with the `keycloak` container stopped (docker compose), run the full local-username/password login chain and the full staff-PIN login chain end-to-end (registration PIN → device registration → device-token context → staff PIN login → `rejectStaffPin` 403 → logout → dead token) — proving neither path has a hidden Keycloak/cloud dependency, matching PLAN-0002's own "API starts and reports healthy with Keycloak stopped" verification pattern but for the auth paths specifically. CI enforces the same property structurally: `.github/workflows/ci.yml` defines only a Postgres service, no Keycloak container at all.
2. ✅ Add `RbacTests.cs` consolidating cross-cutting RBAC assertions across both auth methods, driven by one shared inventory of every protected endpoint (29 permission-gated + 3 auth-only): unauthenticated request → 401; garbage token → 401; revoked token → 401 immediately; authenticated-but-unauthorized (wrong permission) → 403; `DeviceToken` context → 403 on every permission-gated endpoint; a real `LocalStaffPin` session on every `rejectStaffPin: true` endpoint → 403; cross-tenant access via a valid `SystemAdmin` session for the wrong tenant → 404/empty, not 500 and never data.
3. ✅ (Added at approval.) Add `IgnoreQueryFiltersUsageTests.cs` (unit): source-scan guard asserting `.IgnoreQueryFilters(` appears only in the five documented bootstrap/authentication files, with exact two-way equality so both an unapproved new bypass and a silently removed documented one fail the build. Create open issues OI-0011 (user-management endpoints — issue only, per explicit decision), OI-0012 (inactive parent lifecycle vs device/staff auth), OI-0013 (registration-PIN `MaxUses` race), OI-0014 (tenant-less security-event auditing), OI-0015 (permission metadata for staff-PIN eligibility); update `docs/issues/index.md`; add implementation-status notes to `docs/testing/testing-strategy.md`/`security-tests.md`. (Milestone H's original steps 41–43 are renumbered 42–44 by this insertion.)

### Milestone H — Documentation and plan closeout

**Revised 2026-07-03** (approved as documentation-only, no source/migration/test changes; PLAN-0003 explicitly kept under `docs/plans/active/` rather than moved to `completed/` — see the new OI-0016 below): all three steps complete.

1. ✅ Added a short, additive final "Implementation status" rollup to `docs/architecture/tenancy.md`, `docs/architecture/security.md`, `docs/architecture/multi-location.md`, `docs/modules/devices.md`, and `docs/modules/audit.md` — each doc's existing per-milestone status notes were left untouched (they remain a useful record of when each piece landed); the rollup only adds a closing summary that the plan is complete and states what's still explicitly out of scope.
2. ~~Move `docs/adr/proposed/ADR-0015-...md` to `docs/adr/accepted/` once approved~~ — done 2026-07-01, per explicit human acceptance of ADR-0015 with the required additions; `docs/adr/index.md` updated accordingly. No ADR changes in Milestone H — ADR-0015 stays accepted, ADR-0016 stays proposed, neither reopened.
3. ✅ Updated this plan's "Status" section with final state per milestone (including correcting Milestone G's stale "not yet committed" wording to record its actual commit hashes `fb8a8b8`/`cca09b5`); updated `docs/plans/active/PLAN-0003-worker-notes.md` with a Milestone H report and a fresh PLAN-0003 → PLAN-0004 handoff section. Also, beyond the original step wording: created `docs/issues/open/OI-0016-define-completed-plan-archival-convention.md` (+ `docs/issues/index.md` entry) recording the plan-relocation question raised during this closeout; corrected stale wording in `docs/README.md` (open-issue count) and added a `docs/CHANGELOG.md` entry for PLAN-0003.

## Initial Permission Catalogue

| Permission code | Purpose | Seeded to roles |
| --- | --- | --- |
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

- OI-0002, OI-0006, OI-0007, OI-0010 remain resolved/closed and are referenced, not reopened.
- **Six new open issues were created by this plan, all still open at PLAN-0003 completion:** OI-0011 (user management endpoints), OI-0012 (inactive parent lifecycle vs device/staff authentication), OI-0013 (device registration PIN `MaxUses` concurrency race), OI-0014 (tenant-less security-event auditing), OI-0015 (permission metadata for staff-PIN eligibility) — all opened at Milestone G (2026-07-03) — and OI-0016 (define completed-plan archival convention), opened at Milestone H (2026-07-03). None of the six are closed by this plan; see `docs/issues/index.md` for the current status of each.

## Human Decisions Needed — Recorded Answers (2026-07-01)

All five were decided by explicit human approval; recorded here so later milestones don't need to re-derive them.

1. **ADR-0015 — Accepted**, with six explicit statements added before acceptance (POS staff login is not Keycloak/OIDC; POS staff sessions are operational sessions separate from admin/cloud identity; session tokens must never be stored raw; PINs must never be stored raw; tenant context fails closed if missing *or ambiguous*; tenant/location/terminal context must come from a registered terminal/session context, never arbitrary user-supplied free text — see ADR-0015's Acceptance Addendum and new §"Context provenance" subsection). Domain-event handlers with DB access may stay in `Api` for now, as proposed.
2. **Region/Country descoping — Approved.** `TenantId` is the only isolation key for PLAN-0003; no `Region`/`Country` entities or fields. Add later only when multi-region/global deployment actually needs it.
3. **PIN/session policy — Approved with corrected session-expiry model.** StaffCode + PIN (never the database primary key) for staff login; 4-digit minimum PIN; PBKDF2 hash, never plaintext; lockout after 5 failed attempts for 15 minutes. **Session expiry is two-part, not a single fixed value as originally drafted:** an absolute maximum of 12 hours from issuance (`AuthSession.ExpiresAtUtc`) *and* an 8-hour idle timeout tracked separately (`AuthSession.LastActivityAtUtc`, updated on each authenticated request; a session is rejected once idle time exceeds 8 hours, even if the 12-hour absolute cap hasn't been reached). This applies to `AuthSession` generally (both `LocalStaffPin` and `LocalUsernamePassword`) for MVP; per-tenant/per-location configurability is deferred, hardcoded defaults are acceptable for this plan. The Milestone C `AuthSession` field list is corrected to include `LastActivityAtUtc` accordingly. The role/permission snapshot captured at session creation is explicitly for audit purposes — later permission changes must not rewrite what a staff member was authorized to do at the time of a past action (this was already this plan's Domain Assumption; now explicitly confirmed).
4. **Bootstrap seeding — Approved**, dev/local-only, must not become an accidental production default. The seeded `SystemAdmin` password will be sourced from an environment variable with a documented local-dev default (not a hardcoded literal in migration source), and the bootstrap seed will be clearly commented as dev/local-only in code and in `docs/deployment/local.md`. No production admin credentials are ever seeded by this plan.
5. **Single-location `StaffMember` — Approved for MVP**, with an explicit forward-compatibility requirement: the schema must not preclude adding multi-location staff assignment later without a full model rewrite. Addressed by modelling `StaffMember.LocationId` as the *home* location (used for staff-PIN login's location-match check) while keeping `StaffMemberRole.LocationId` as its own nullable scope column (already planned in Milestone F) — multi-location access can later be added as additional `StaffMemberRole`-style assignment rows (a new `StaffMemberLocation` table) without changing `StaffMember` itself or any existing session/audit code, since sessions already key off `AuthSession.LocationId` (the location the staff member is *currently* operating at), not off `StaffMember.LocationId` directly.

## Human Decisions Needed — Recorded Answers (2026-07-02, Milestone D)

1. **Milestone D CRUD scope — Approved as non-destructive full CRUD.** The originally-approved plan (step 22) scoped Milestone D to create+read only; the human expanded this to create/read/rename/deactivate/reactivate, explicitly ruling out hard `DELETE` endpoints ("these entities will be referenced by audit logs, sessions, devices, orders, payments, reports, and future sync data"). `IsActive` (default `true`) is added to `Organisation`/`Location`/`Terminal` via a small migration. Routes are flat, not nested (see Milestone D's "Design Decisions"). A client-supplied `TenantId` in any request body is rejected with 400, not merely ignored. `rejectStaffPin` support in `RequirePermissionFilter` is pulled forward from Milestone F to Milestone D and applied to every endpoint this milestone adds, even though no staff-PIN session exists yet to exercise it end-to-end (proven via a unit test constructing the `AuthContext` directly, per Milestone D step 22's TDD note).

## Human Decisions Needed — Recorded Answers (2026-07-02, Milestone E)

1. **Milestone E approved with amendments.** (1) Flat routes (`/api/v1/device-registration-pins`, `/api/v1/device-registration`, `/api/v1/devices/{deviceId}/…`, `GET /api/v1/devices?locationId=`), plus a new `POST /api/v1/device-registration-pins/{pinId}/revoke` (`devices.register`, `rejectStaffPin: true`) so an accidentally created PIN can be revoked before expiry. (2) Device token format `Device {credentialId}.{secret}` — the id is the lookup key, the secret is verified against the stored salted hash; device credentials stay on the salted HMAC hasher, never deterministic SHA-256, and no raw secret/PIN is ever stored. (3) Verify-scan PIN validation approved for MVP: scan live (unexpired, unrevoked, uses-remaining) PIN rows, fail closed on more than one live match, 15-minute expiry, `MaxUses` default 1 / range 1–10, `UsedCount` incremented only after successful registration. (4) Unknown-PIN attempts return a generic failure and write **no** `AuditEvent` (no tenant for the non-nullable `TenantId`); matched-but-expired/revoked/exhausted attempts audit only when the matching row resolves unambiguously; ambiguous matches audit only if a single tenant can be resolved. A note is recorded (worker notes) that unauthenticated/global security events may need a separate, tenant-less table later. (5) Smaller defaults approved: no `DeviceCredential.Salt` column; six audit events (`DeviceRegistrationPinCreated`/`DeviceRegistrationPinRevoked`/`DeviceRegistered`/`DeviceRegistrationFailed`/`DeviceCredentialRotated`/`DeviceRevoked`); no `Device.IsActive` (revocation lives on credentials; revoked/lost devices re-register as a new `Device`); rate limit 10/minute per remote IP on the registration endpoint. A device token yields a partial `AuthContext` (`TenantId`/`OrganisationId`/`LocationId`/`DeviceId`, empty roles/permissions) — trusted device context only, never admin access; Milestone F combines it with staff PIN authentication.

## Human Decisions Needed — Recorded Answers (2026-07-02, Milestone F)

1. **Milestone F approved with amendments** (thirteen explicit decisions, recorded verbatim-equivalent; see the revised Milestone F steps above for how each lands):
   1. Flat routes approved, including the added `GET /api/v1/staff-members?locationId=` list endpoint. No nested routes.
   2. `StaffMember`/`StaffMemberRole` introduced now with the approved field lists, tenant-owned fail-closed filters, and the `auth_sessions.staff_member_id` FK added in migration `AddStaffMembers`.
   3. **`StaffCode` amended to alphanumeric**, not numeric-only: uppercase letters and digits, 2–20 chars, uppercase-normalised before storing/comparing, unique per organisation (venues may want codes like `DJ`, `MGR1`, `BAR01`). PIN: digits only, 4–10, `Pbkdf2PinHasher`, never raw.
   4. Staff PIN login requires a valid `DeviceToken` context first: anonymous → 401, `Bearer` session → 403; tenant/org/location/device context comes only from the device `AuthContext`; body `locationId` must match the device's location and the staff member's home location; no client-supplied `TenantId` trusted.
   5. Normal `AuthSession` rows for staff sessions (`AuthMethod = LocalStaffPin`, `UserId` null, `TerminalId` null for now — **deferred:** terminal-device mapping doesn't exist yet; staff sessions attach `TerminalId` once terminal assignment is implemented). Raw token returned once, only the hash stored.
   6. **Staff sessions do not reuse the admin session expiry:** POS staff defaults are 8h absolute / 30min idle, in their own policy class (`StaffSessionExpiryPolicy`).
   7. Permission snapshot captured at login from `StaffMemberRole → RolePermission → Permission`, stored on the session, never re-derived (audit accuracy).
   8. Sensitive-permission login guard approved using the current catalogue codes as a temporary list, **with a recorded follow-up:** permission metadata/category should later define staff-PIN eligibility — do not rely forever on a hard-coded list. `rejectStaffPin: true` remains the endpoint-level safety net; the login-time rejection is defense-in-depth.
   9. End-to-end tests must prove a real `LocalStaffPin` session receives 403 from every `rejectStaffPin: true` endpoint; new staff endpoints use `staff.manage, rejectStaffPin: true`.
   10. **PIN reset changed to server-generated:** the endpoint generates a new PIN, returns it raw once, stores only the hash, clears `FailedPinAttempts`/`LockedOutUntilUtc`, revokes the staff member's active sessions, and audits. No admin-supplied PIN reset in Milestone F.
   11. Disable: `IsActive = false`, revoke all active sessions, audit, prevent future login, immediately invalidate existing sessions.
   12. Six audited actions (`StaffMemberCreated`, `StaffMemberPinReset`, `StaffMemberRoleAssigned`, `StaffPinLoginSucceeded`, `StaffPinLoginFailed`, `StaffMemberDisabled`); unknown staff-code attempts audited (tenant/org/location/device known from the trusted device token); client responses stay generic — never disclose whether the code or the PIN was wrong.
   13. The full test list in the revised step 38.

## Human Decisions Needed — Recorded Answers (2026-07-03, Milestone G)

1. **Milestone G approved as test-and-documentation-only**, with the docs housekeeping/planning pass committed first (`60dedbe`, per explicit instruction). Allowed: offline/local verification tests, consolidated RBAC/auth/session tests, the `IgnoreQueryFilters()` source-scan guard test, adoption of `docs/testing/local-smoke-test.md`, testing/security doc updates, open issues for deferred risks, plan/worker-notes updates. Not allowed: new entities, migrations, new endpoints, product-code behaviour changes (unless required only to make an already-intended testable path work — none were needed), plus the standing scope guard (no orders/tax/payments/Stripe/receipts/sync/UI/KDS/`Modules.*`/Keycloak wiring/localisation). Approved test list: `HybridOfflineLoginTests.cs` (both auth chains end-to-end, Postgres only), `RbacTests.cs` (unauthenticated/garbage/revoked → 401; wrong-permission, `DeviceToken`, and `LocalStaffPin` → 403; cross-tenant → 404/empty, never 500), `IgnoreQueryFiltersUsageTests.cs` (documented-locations-only guard). Approved issues: OI-0011–OI-0015. **OI-0011 decision: create the issue only — user-management endpoints are a follow-up implementation plan, not Milestone G work.** Milestone G implementation is not to be committed until the result is reviewed and approved.

## Commit Sequence

```text
docs: propose ADR-0015 (tenant isolation and session token mechanism)
feat(identity): add AuthContext, tenant provider, and credential hashing ports
test(identity): add PBKDF2 and device-credential hasher unit tests
feat(tenancy): add TenantId columns and fail-closed EF Core global query filters
test(tenancy): add cross-tenant query filter isolation tests
feat(identity): add RBAC schema (roles, permissions, users) with seed data
feat(identity): add local username/password login and session authentication
feat(audit): add AuditEvent and domain-event audit handlers for login activity
test(identity): add local username/password login and audit tests
feat(identity): add rejectStaffPin support to RequirePermissionFilter
test(identity): add rejectStaffPin filter tests
feat(tenancy): add IsActive lifecycle column to organisation, location, and terminal
feat(tenancy): add organisation CRUD endpoints with tenant-scoped authorization
feat(tenancy): add location CRUD endpoints with organisation-scoped authorization
feat(tenancy): add terminal CRUD endpoints with organisation-scoped authorization
feat(audit): add lifecycle audit events for organisation, location, and terminal
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
