# PLAN-0003 Worker Notes — Planning Pass (2026-07-01)

## Session Purpose

Turn the architecture-level PLAN-0003 draft (written during the 2026-06-29 documentation session, later aligned to ADR-0013 on 2026-07-01) into an implementation-ready plan. No product code was written or modified in this session — planning/documentation only, per explicit instruction to stop after the plan and wait for approval.

## What Was Read

Full pass over: PLAN-0002 (to confirm exactly what the committed skeleton contains — `Tenant`/`Organisation`/`Location`/`Device`/`Terminal` entities, `DaxaDbContext`, the `IDomainEvent`/`IDomainEventDispatcher` scaffold, the current project reference graph and package versions), ADR-0013, ADR-0014, ADR-0008, ADR-0003, ADR-0010, OI-0006, OI-0007, `docs/architecture/tenancy.md`/`security.md`/`multi-location.md`, `docs/modules/devices.md`/`audit.md`/`13-multi-location-users-cash-audit.md`, `docs/testing/testing-strategy.md`/`security-tests.md`, all current `*.csproj` files, `Program.cs`, `DependencyInjection.cs` in both Persistence and Infrastructure, the existing health check test, the CI workflow, and the Docker Compose file.

## What Was Produced

1. `docs/adr/proposed/ADR-0015-tenant-isolation-and-session-token-mechanism.md` — new proposed ADR. Locks in three decisions that ripple across every later plan: denormalized `TenantId` + fail-closed EF Core global query filters (fed by a `Domain`-level `ICurrentTenantProvider` so `Persistence` doesn't need a new reference to `Application`); opaque hashed session tokens (not JWT) for `AuthSession`; and keeping DB-touching domain-event handlers in `DaxaPos.Api` rather than amending ADR-0014's reference graph a second time. **Not yet accepted — blocks Milestone B of PLAN-0003.**
2. `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md` — rewritten with concrete entity field lists, 5 named migrations, ~24 endpoints, 8 ordered milestones (A–H), a minimal permission catalogue, explicit test file names, and a full commit sequence.
3. `docs/adr/index.md` — added ADR-0015 under Proposed.
4. `docs/README.md` — one-line update noting ADR-0015's proposed status.

## Design Decisions Worth Flagging to a Future Reader

- **No new `Modules.*` project.** The skeleton has zero `Modules.*` projects today; adding `Modules.Identity` now would be speculative structure. Identity/tenancy code lives directly in `Domain`/`Application`/`Infrastructure`/`Persistence`/`Api`.
- **Reference graph is unchanged from PLAN-0002/ADR-0014.** This was the trickiest design constraint to satisfy — see ADR-0015 §3 for the reasoning. If a later plan (likely PLAN-0005 payments→receipts/audit or PLAN-0007 sync) needs a *second* DB-touching domain-event handler outside `Api`, that's the trigger to revisit adding `Persistence → Application`, not before.
- **Keycloak wiring is explicitly deferred**, not built. The task's scope list for PLAN-0003 doesn't ask for it, ADR-0013 explicitly permits WebAPI-native username/password for local manager/admin in MVP, and every endpoint this plan defines is reachable that way. The previous plan draft's step 2 ("configure a Keycloak realm/client... JWT bearer middleware") would have built unused infrastructure — removed.
- **"Hybrid mode using synced data" is scoped narrowly.** PLAN-0003 does not build any part of PLAN-0007's sync mechanism. It only guarantees the auth code path never depends on live cloud connectivity, verified directly in Milestone G by running the login tests with the `keycloak` container stopped.
- **Region/Country are still out of scope.** Only `TenantId` is added to `Location`/`Device`/`Terminal` for isolation; no `Region`/`Country` tables.

## Open Items Requiring the User's Explicit Sign-Off

See "Human Decisions Needed" in the plan itself — summarized: (1) accept/reject/amend ADR-0015; (2) confirm Region/Country descoping; (3) confirm PIN/session policy defaults (4-digit PIN, 5-attempt lockout, 12h/8h session expiry); (4) confirm dev-only bootstrap seeding approach; (5) confirm single-home-location `StaffMember` for MVP.

## Recommended Next Session

1. Human reviews and (dis)approves the plan and ADR-0015, with any amendments to the five items above.
2. Once ADR-0015 is accepted (moved to `docs/adr/accepted/`, `docs/adr/index.md` updated), start Milestone A — it has no dependency on ADR-0015's acceptance and can begin immediately if the human wants to unblock work before the ADR review completes.
3. Update this plan's Status section with ✅/⬜ per milestone as work proceeds — do not let more than ~3 commits pass without it, per CLAUDE.md's plan-refresh rule.

---

## Milestone A Report (2026-07-01)

Human approved the plan and ADR-0015 with five recorded decisions (see the plan's "Human Decisions Needed" section, now updated with the recorded answers). ADR-0015 was updated with the six explicitly-required statements and moved to `docs/adr/accepted/`. Milestone A implemented per the plan, using strict TDD for every piece with actual logic (hashers, token service, accessor/tenant-provider) — write failing test, confirm RED (compile error, since the type didn't exist), implement, confirm GREEN. Enums/interfaces/records with no branching logic (AuthMethod, ICurrentTenantProvider, AuthContext, the other port interfaces, Permissions constants) were added directly, consistent with the TDD skill's own scope (the Iron Law applies to behavior; a bare enum/interface has none).

### Files changed

New:
- `src/DaxaPos.Domain/Enums/AuthMethod.cs`
- `src/DaxaPos.Domain/Tenancy/ICurrentTenantProvider.cs`
- `src/DaxaPos.Application/Identity/AuthContext.cs`, `IAuthContextAccessor.cs`, `IPinHasher.cs`, `IDeviceCredentialHasher.cs`, `ISessionTokenService.cs`, `Permissions.cs`
- `src/DaxaPos.Infrastructure/Security/Pbkdf2PinHasher.cs`, `HmacDeviceCredentialHasher.cs`, `RandomSessionTokenService.cs`
- `src/DaxaPos.Infrastructure/Identity/HttpContextAuthContextAccessor.cs`, `CurrentTenantProvider.cs`
- `tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj` (new project, added to `DaxaPos.sln`)
- `tests/DaxaPos.UnitTests/Security/Pbkdf2PinHasherTests.cs`, `HmacDeviceCredentialHasherTests.cs`, `RandomSessionTokenServiceTests.cs`
- `tests/DaxaPos.UnitTests/Identity/AuthContextAccessorTests.cs` (contains both `AuthContextAccessorTests` and `CurrentTenantProviderTests`)
- `docs/adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md` (moved from `proposed/`, content updated)

Modified:
- `src/DaxaPos.Infrastructure/DependencyInjection.cs` — registers `IAuthContextAccessor`, `ICurrentTenantProvider`, `IPinHasher`, `IDeviceCredentialHasher`, `ISessionTokenService`, plus `AddHttpContextAccessor()`.
- `src/DaxaPos.Infrastructure/DaxaPos.Infrastructure.csproj` — added `FrameworkReference` to `Microsoft.AspNetCore.App` (see Deviations below).
- `DaxaPos.sln` — added `DaxaPos.UnitTests` under the `tests` solution folder.
- `docs/adr/index.md`, `docs/README.md` — ADR-0015 moved from Proposed to Accepted.
- `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md` — Status section updated with milestone checklist; all "ADR-0015 not yet accepted" language resolved; AuthSession field list corrected for the two-part session-expiry model; bootstrap-seeding mechanism corrected to use a startup routine reading env vars instead of `HasData` (see Deviations below); Human Decisions Needed section now records the actual answers.

No production `.cs` files outside the above were touched. No migrations were created (Milestone A has no DB dependency by design).

### Migration created

None — Milestone A is schema-free by design. The first migration (`AddTenantIsolationColumns`) is Milestone B.

### Tests added

18 new tests in `tests/DaxaPos.UnitTests` (all TDD'd — RED confirmed via compile failure, then GREEN):
- `Pbkdf2PinHasherTests` (4): correct-PIN verify, wrong-PIN rejection, distinct salts per hash call, hash never contains the raw PIN.
- `HmacDeviceCredentialHasherTests` (4): same shape, for device credential secrets.
- `RandomSessionTokenServiceTests` (5): non-empty/high-entropy token, distinct tokens per call, deterministic hash for the same token (needed for DB lookup), distinct hashes for distinct tokens, hash is never equal to the raw token.
- `AuthContextAccessorTests` (3) + `CurrentTenantProviderTests` (2): no-HttpContext and no-stashed-context both resolve to `null` (not an exception, not a default tenant), and the happy path returns the stashed `AuthContext`/its `TenantId` correctly. These directly demonstrate the fail-closed building block ADR-0015/the user's decisions require, ahead of Milestone B actually wiring it into EF Core.

Existing `HealthCheckTests` (1, from PLAN-0002) re-run as a regression check — still passes with the `keycloak` container stopped, confirming no regression from the new DI registrations.

### Commands run

```
dotnet sln add tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --solution-folder tests
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj   (run repeatedly through the RED/GREEN cycle)
dotnet build DaxaPos.sln
docker compose up -d db                                        (deploy/ — keycloak deliberately left stopped)
dotnet test DaxaPos.sln
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, `keycloak` stopped) — **19/19 passed** (18 new unit tests + 1 existing health check), 0 failed, 0 skipped.

### Deviations from the written plan (flagged, not silently made)

1. **`AuthContext` gained a `TerminalId` field** not listed in the plan's Milestone A step 3 field list. Added for consistency with the `AuthSession` entity (Milestone C), which does list `TerminalId?`, and because `docs/modules/audit.md` lists `TerminalId` as a required audit field — omitting it from `AuthContext` would have meant threading it through separately later. Low-risk, additive.
2. **`Microsoft.AspNetCore.Http.Abstractions` NuGet package → `FrameworkReference` to `Microsoft.AspNetCore.App`.** The plan's Files Likely To Change said "add Microsoft.AspNetCore.Http.Abstractions package reference"; the actually-correct mechanism for a non-Web-SDK class library to consume `IHttpContextAccessor`/`HttpContext`/`DefaultHttpContext` on .NET 9 is a `FrameworkReference`, not a NuGet package (several of the old standalone ASP.NET Core packages were folded into the shared framework after .NET Core 3.0). Applied to both `DaxaPos.Infrastructure.csproj` and `tests/DaxaPos.UnitTests.csproj`. Functionally equivalent intent, corrected mechanism.
3. **Test coverage beyond the two files the plan named** (`Pbkdf2PinHasherTests`, `HmacDeviceCredentialHasherTests`): added `RandomSessionTokenServiceTests` and `AuthContextAccessorTests`/`CurrentTenantProviderTests`. These directly serve explicit user requirements ("session tokens must never be stored raw," "tenant context must fail closed if missing") rather than being scope creep.
4. **Did not add `AuthContextPermissionTests.cs`**, which the plan's "Files Likely To Change" table listed for `tests/DaxaPos.UnitTests/` but which had no corresponding implementation step — permission-checking logic (`RequirePermission`) doesn't exist until Milestone C. This was an inconsistency in the plan's own file list, not a skipped requirement; noted so Milestone C adds this test alongside `RequirePermission`, not a second time redundantly.
5. **Bootstrap-seeding mechanism corrected** (see plan edits): the original Milestone A/C draft said the bootstrap `SystemAdmin` user would be seeded via EF Core `HasData` with "a fixed dev-only password." Per the user's explicit Decision 4 ("use environment variables ... do not seed production admin credentials"), `HasData` cannot do this correctly — `HasData` values are baked into migration source at design time, so a password hash from `HasData` would mean committing a hash to source control regardless of how it was generated. Corrected to: `Tenant`/`Organisation` still via `HasData` (non-secret), but the bootstrap `User` (with its password) is created by a small idempotent startup routine reading `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`DAXA_BOOTSTRAP_ADMIN_PASSWORD` env vars, refusing to run if they're unset (no guessable fallback). This is now the plan of record for Milestone C, not yet implemented (no `User` entity exists yet).
6. **Session-expiry policy corrected from a single fixed value to two-part** (absolute 12h cap + 8h idle timeout), per the user's Decision 3 wording being more specific than the plan's original draft assumed. `AuthSession`'s planned field list (Milestone C) now includes `LastActivityAtUtc`. Not yet implemented (no `AuthSession` entity exists yet) — recorded so Milestone C/F build the corrected model.

None of these deviations required backing out or redoing already-written code — all were caught either while implementing Milestone A itself (1, 2) or while updating the plan doc to record your decisions accurately (3–6, which are corrections to *future* milestones' design, not to anything built so far).

### Blockers before Milestone B

None. ADR-0015 is accepted, the plan is updated, `dotnet build`/`dotnet test` are clean. Milestone B (tenant isolation columns + fail-closed EF Core global query filters + cross-tenant isolation tests) can start on request.

One thing worth flagging before Milestone B specifically (not a blocker, a heads-up): Milestone B's isolation tests need to seed two tenants' `Location` rows directly via `DaxaDbContext` with a test-double `ICurrentTenantProvider` (per the plan). That test double can now reuse the same `FakeAuthContextAccessor` pattern established in this milestone's `CurrentTenantProviderTests`, so Milestone B doesn't need to invent a new fake — flagging so the next pass doesn't duplicate it.

---

## Milestone B Report (2026-07-01)

Human approved Milestone B scope (tenant isolation columns, fail-closed EF Core global query filters, cross-tenant isolation tests, migration) with explicit rules: no staff PIN login, no order/tax/payment/sync/UI/KDS, no `Modules.*`, no Keycloak, no weakening fail-closed behaviour, no trusting client-supplied tenant IDs. All observed. TDD used throughout: `TenantIsolationTests.cs` written first, confirmed RED via compile failure (`TenantId` didn't exist on `Location`, `DaxaDbContext` didn't take an `ICurrentTenantProvider`), then implemented to GREEN.

### Files changed

New:
- `tests/DaxaPos.Api.Tests/Support/FakeCurrentTenantProvider.cs` — test double for `ICurrentTenantProvider`.
- `tests/DaxaPos.Api.Tests/TenantIsolationTests.cs` — 6 tests (see below).
- `src/DaxaPos.Persistence/Migrations/20260701102312_AddTenantIsolationColumns.cs` (+ `.Designer.cs`, + updated `DaxaDbContextModelSnapshot.cs`).

Modified:
- `src/DaxaPos.Domain/Entities/Location.cs`, `Device.cs`, `Terminal.cs` — added `TenantId` property to each.
- `src/DaxaPos.Persistence/Configurations/LocationConfiguration.cs`, `DeviceConfiguration.cs`, `TerminalConfiguration.cs` — `TenantId` required, indexed, FK to `Tenant` (`OnDelete: Restrict`).
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — constructor now takes `ICurrentTenantProvider` (from `DaxaPos.Domain.Tenancy`, no new project reference needed); added fail-closed `HasQueryFilter` for `Organisation`, `Location`, `Device`, `Terminal`.
- `docs/architecture/tenancy.md` — added an "Implementation status" note (full consolidation still deferred to Milestone H per the plan).
- `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md` — Status/Milestone B section marked complete with actual-vs-planned notes.

### Migration created

`AddTenantIsolationColumns` (`20260701102312_AddTenantIsolationColumns`). Adds `TenantId` (uuid, not null, indexed, FK → `tenants.Id`, `ON DELETE RESTRICT`) to `locations`, `devices`, `terminals`. EF's migration-generation always requires a default-value expression for a new non-null column regardless of actual table contents (it doesn't inspect live data at generation time) — it used `Guid.Empty` as that expression, but this is inert: verified the tables were genuinely empty before applying (see below), so no row was ever assigned that value. No backfill logic was needed or written.

**Notable discovery while applying:** the `deploy-db-1` Postgres container had no tables at all — `\dt` returned "Did not find any relations." The `db` container had been recreated fresh in the Milestone A session (new Docker volume) and `InitialCreate` had never actually been applied to it; only `dotnet ef database update` running both `InitialCreate` and `AddTenantIsolationColumns` together brought the dev DB to the expected schema. `HealthCheckTests` didn't catch this because its DB health check only verifies connectivity, not schema/table existence. Flagging in case a future worker sees an unexpectedly empty dev DB and wonders why — it's not a bug, just a fresh volume from earlier in this session that had never been migrated.

### Tests added

`tests/DaxaPos.Api.Tests/TenantIsolationTests.cs` (6):
- `Location_IsVisible_ToItsOwnTenant`
- `Location_IsNotVisible_ToADifferentTenant`
- `Location_IsNotVisible_WhenTenantContextIsMissing`
- `Organisation_IsNotVisible_ToADifferentTenant`
- `Organisation_IsNotVisible_WhenTenantContextIsMissing`
- `LocationList_OnlyContainsCallersTenant_WhenOtherTenantsHaveData`

Each test seeds fresh, randomly-GUID'd `Tenant`/`Organisation`/`Location` rows directly via `DaxaDbContext` (no cleanup/transaction wrapping — consistent with the existing `HealthCheckTests` convention of hitting the persistent dev Postgres directly; safe because every test uses fresh GUIDs, so accumulated rows across runs never collide with a given test's assertions).

### Commands run

```
dotnet build tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj    (RED)
dotnet build DaxaPos.sln                                          (GREEN, after implementation)
dotnet ef migrations add AddTenantIsolationColumns \
  --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj \
  --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj --output-dir Migrations
docker compose exec -T db psql -U daxapos -d daxapos -c "\dt"     (discovered empty DB)
dotnet ef database update \
  --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj \
  --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj
docker compose exec -T db psql -U daxapos -d daxapos -c "\d locations" -c "\d devices" -c "\d terminals"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj
dotnet build DaxaPos.sln && dotnet test DaxaPos.sln
docker compose ps                                                  (confirmed Keycloak still not running)
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, both migrations applied, `keycloak` stopped) — **25/25 passed** (18 unit tests + 7 API tests: 1 health check + 6 new tenant isolation tests), 0 failed, 0 skipped.

### Deviations from the written plan (flagged, not silently made)

1. **`Organisation` also received the fail-closed filter**, not just `Location`/`Device`/`Terminal` as step 9's literal wording said. `Organisation` already had `TenantId` from PLAN-0002 but no filter — leaving it unfiltered would have contradicted the user's explicit Milestone B instruction to "ensure TenantId is applied consistently to tenant-owned entities." Two extra tests (`Organisation_IsNotVisible_*`) cover this.
2. **Discovered and fixed a pre-existing gap, not introduced by this milestone:** the dev Postgres volume had never had `InitialCreate` applied. Both migrations were applied together; no code change was needed for this, just running `dotnet ef database update` instead of assuming the schema already matched.

### Blockers before Milestone C

None. `dotnet build`/`dotnet test` are clean, Keycloak remains stopped and unused, fail-closed behaviour is verified by 6 passing tests, and no migration/entity/endpoint work strayed into Milestone C–H territory (no `Role`/`Permission`/`User`/`StaffMember`/`AuthSession` entities, no login endpoints, no `RequirePermission`, nothing added under `DaxaPos.Modules.*`).

---

## Milestone C Report (2026-07-02)

Human approved Milestone C scope (RBAC schema foundation, permission catalogue seed data, local WebAPI-native admin/bootstrap login, staff/user model foundation, audit context plumbing foundation) with explicit rules: no order/tax/payment/Stripe/sync/UI/KDS, no `Modules.*`, no weakening tenant filters, no raw password/PIN/session-token storage, no database-primary-key-as-login-identifier, local auth kept separate from Keycloak/OIDC. All observed.

**Scope clarification recorded before coding:** "staff/user model foundation required for later staff PIN login" was interpreted as the `User`/RBAC/`AuthSession`/`AuditEvent` mechanism Milestone F's staff-PIN login will reuse — **not** as pulling the `StaffMember` table forward into this milestone. `StaffMember`/`StaffMemberRole` remain Milestone F, per the already-approved plan's milestone ordering. Confirmed via `find` that no `StaffMember`-named file exists anywhere in `src/`.

TDD used throughout: `LoginLockoutPolicyTests`/`SessionExpiryPolicyTests` (pure logic, unit-level) and `RequirePermissionFilterTests` (constructed directly via `EndpointFilterInvocationContext.Create`, no HTTP) were each written first and confirmed RED via compile failure before implementing. `LocalUserLoginTests.cs` (12 HTTP-level tests) was written as a full vertical-slice acceptance test after the supporting pieces existed — consistent with how `TenantIsolationTests` was handled in Milestone B, since a login flow spanning entities → migration → auth handler → endpoints → audit handlers can't meaningfully "fail for the right reason" one piece at a time.

### Files changed

New (selected — see git diff for the full list):
- `src/DaxaPos.Domain/Entities/Role.cs`, `Permission.cs`, `RolePermission.cs`, `User.cs`, `UserRole.cs`, `AuthSession.cs`, `AuditEvent.cs`.
- `src/DaxaPos.Domain/Events/LocalUserLoginSucceededDomainEvent.cs`, `LocalUserLoginFailedDomainEvent.cs`, `AuthSessionRevokedDomainEvent.cs`.
- `src/DaxaPos.Application/Identity/LoginLockoutPolicy.cs`, `SessionExpiryPolicy.cs`.
- `src/DaxaPos.Persistence/Seed/RbacSeedIds.cs`; `Configurations/RoleConfiguration.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs`, `UserConfiguration.cs`, `UserRoleConfiguration.cs`, `AuthSessionConfiguration.cs`, `AuditEventConfiguration.cs`.
- `src/DaxaPos.Persistence/Migrations/20260701152344_AddIdentityAndRbacCore.cs` (+ `.Designer.cs`).
- `src/DaxaPos.Api/BootstrapAdminSeeder.cs`; `Authentication/SessionAuthenticationHandler.cs`; `Authorization/RequirePermissionFilter.cs`; `Audit/DomainEventAuditHandlers.cs`; `Endpoints/Identity/AuthEndpoints.cs`.
- `tests/DaxaPos.UnitTests/Identity/LoginLockoutPolicyTests.cs`, `SessionExpiryPolicyTests.cs`.
- `tests/DaxaPos.Api.Tests/RequirePermissionFilterTests.cs`, `LocalUserLoginTests.cs`.

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — new `DbSet`s, fail-closed filters for `User`/`UserRole`/`AuthSession`/`AuditEvent`.
- `src/DaxaPos.Api/Program.cs` — authentication/authorization wiring, audit handler registration, `MapAuthEndpoints()`, `BootstrapAdminSeeder.SeedAsync()` call before `app.Run()`.
- `deploy/.env.example` — documented `DAXA_BOOTSTRAP_ADMIN_EMAIL`/`PASSWORD` as dev/local-only.
- `docs/deployment/local.md`, `docs/architecture/security.md` — implementation-status notes.
- Plan doc Status/Milestone C section.

### Migration created

`AddIdentityAndRbacCore` (`20260701152344_AddIdentityAndRbacCore`). Creates `roles`, `permissions`, `role_permissions`, `users`, `user_roles`, `auth_sessions`, `audit_events`, plus seed `InsertData` for the role/permission catalogue. Verified against a database dropped and recreated from scratch (`dotnet ef database drop --force` then `dotnet ef database update`, applying both `AddTenantIsolationColumns` and `AddIdentityAndRbacCore` in sequence), followed by a `psql` count check confirming exactly: SystemAdmin=8 permissions, OrganisationOwner=7, VenueManager=4, SupportAccess=2, Staff=0 (no row — matches "Staff gets none of these" by design).

### Seed data

`Role`: `SystemAdmin`, `OrganisationOwner`, `VenueManager`, `Staff`, `SupportAccess`. `Permission`: the 8-code catalogue from the plan. `RolePermission`: mapped exactly per the plan's table. No `Tenant`/`Organisation`/`User` seeded via migration — see the bootstrap-seeding deviation above; those are created by `BootstrapAdminSeeder` at app startup instead, gated on env vars.

### Login/session/audit pieces

`POST /api/v1/auth/local/login`, `POST /api/v1/auth/logout`, `GET /api/v1/auth/me` (permanent endpoint, not a test-only route). `SessionAuthenticationHandler` (scheme name `"Session"`, the only scheme registered — no `DeviceToken` scheme yet, that's Milestone E). `RequirePermissionFilter` implemented without the `rejectStaffPin` parameter (see deviation note above — nothing to reject yet). Audit: `LocalUserLoginSucceededAuditHandler`, `LocalUserLoginFailedAuditHandler`, `AuthSessionRevokedAuditHandler`, each a thin `IDomainEventHandler<T>` writing one `AuditEvent` row, hosted in `DaxaPos.Api` per ADR-0015 §4 (no reference-graph change).

### Tests added

`DaxaPos.UnitTests` (+13): `LoginLockoutPolicyTests` (6), `SessionExpiryPolicyTests` (7).
`DaxaPos.Api.Tests` (+15): `RequirePermissionFilterTests` (3: no-context→401, wrong-permission→403, correct-permission→calls next), `LocalUserLoginTests` (12: correct login, wrong password, unknown email, inactive user, lockout-after-5-then-still-locked-with-correct-password, `/me` happy path, `/me` no token, `/me` garbage token, logout-then-old-token-rejected, audit row on success, audit row on failure, no audit row for unknown email).

### Commands run

```
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj      (RED then GREEN, policy classes)
dotnet build tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj      (RED then GREEN, RequirePermissionFilter)
dotnet build src/DaxaPos.Persistence/DaxaPos.Persistence.csproj
dotnet ef migrations add AddIdentityAndRbacCore \
  --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj \
  --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj --output-dir Migrations
dotnet ef database drop --force \
  --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj \
  --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj
dotnet ef database update \
  --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj \
  --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj
docker compose exec -T db psql -U daxapos -d daxapos -c "SELECT ... FROM roles/permissions/role_permissions ..."
dotnet build DaxaPos.sln
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~LocalUserLoginTests"
dotnet test DaxaPos.sln
docker compose ps                                                  (confirmed Keycloak still not running)
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, fresh migrations applied, `keycloak` stopped) — **53/53 passed** (31 unit tests + 22 API tests: 1 health check + 6 tenant isolation + 3 permission filter + 12 login), 0 failed, 0 skipped.

### Deviations from the written plan (flagged, not silently made)

1. **Bootstrap seeding consolidated into one startup routine** rather than split between `HasData` and code — see step 13 above and the plan's own "ADRs Required"/Architecture Assumptions section, now corrected there too.
2. **`LocalUserLoginFailedDomainEvent` only fires for a matched real `User`**, not for unknown emails — keeps `AuditEvent.TenantId` non-nullable; verified by a dedicated test.
3. **`RequirePermissionFilter` does not yet implement `rejectStaffPin`** — deferred to Milestone F when there's an actual `LocalStaffPin` session to reject. Flagged in the plan so Milestone F doesn't forget to retrofit it onto Milestones C/D/E's endpoints, not just its own.
4. **Two extra pure-logic classes** (`LoginLockoutPolicy`, `SessionExpiryPolicy`) not itemised in the original step list — extracted so the 5-attempt/15-minute and 12-hour/8-hour policy values live in one obvious, independently-unit-tested place rather than as inline literals in the login endpoint and session handler.
5. **`GET /api/v1/auth/me` added** as a permanent endpoint (not originally itemised, though implied by step 21's "authenticated call to a protected endpoint") — a legitimate, ordinary identity-introspection endpoint, not a throwaway test fixture.

### Blockers before Milestone D

None. `dotnet build`/`dotnet test` are clean, Keycloak remains stopped and unused, no client-supplied route/body value is trusted as tenant authority anywhere (there are no resource-scoping route params yet — `/auth/*` endpoints take no tenant/org/location identifiers at all), and nothing added strays into Milestone D–H territory (no `Organisation`/`Location`/`Terminal` write endpoints yet, no `StaffMember`, no `DeviceCredential`, no `Modules.*`).

---

*Milestone C completed: 2026-07-02.*

---

## Milestone D Planning Pass (2026-07-02)

No code written this session — planning only, per explicit instruction to stop after the plan and wait for approval. Full context re-read: the plan doc itself, this notes file, ADR-0013, ADR-0015, and current source for every entity/endpoint/filter/handler touched by Milestones B–C (`Organisation`/`Location`/`Terminal`/`Tenant`/`User`/`AuthSession`/`AuditEvent`/`Role`/`Permission`/`RolePermission`/`UserRole`, `DaxaDbContext`, `RequirePermissionFilter`, `SessionAuthenticationHandler`, `AuthEndpoints`, `Program.cs`, and the existing test suite/conventions).

### Scope clarification recorded before writing the plan

The already-approved plan (step 22) scoped Milestone D to create+read only, 8 nested-route endpoints. The task description asked for "CRUD" with update/delete authorization tests — a direct conflict with the approved plan, and consequential enough (migration or not, hard vs. soft delete, endpoint count) to ask rather than assume. Presented three options; human chose **non-destructive full CRUD** (create/read/rename/deactivate/reactivate, no hard `DELETE`) with a detailed, prescriptive follow-up specifying flat routes, explicit endpoint list, explicit-400 rejection of client-supplied `TenantId`, and pulling `rejectStaffPin` forward from Milestone F into Milestone D. See the plan's "Human Decisions Needed" entry dated 2026-07-02 for the verbatim-equivalent record, and Milestone D's own "Design Decisions" subsection for the reasoning behind choices the human's answer didn't fully spell out (Organisation-scope-by-tenant vs. Location/Terminal-scope-by-`AuthContext.OrganisationId`; one lifecycle domain event per entity rather than per action; 201 vs. 200 on create).

### Entities/tables affected

No new entities. `Organisation`, `Location`, `Terminal` each gain one new column: `IsActive` (`bool`, required, default `true`). No other schema changes — every field Milestone D's endpoints need (`Name`, `TenantId`, `OrganisationId`/`LocationId`, `CreatedAtUtc`) already exists from PLAN-0002/Milestone B.

### Migration required

Yes — one small additive migration, `AddIsActiveToOrganisationLocationTerminal` (adds `is_active boolean not null default true` to all three tables). Unlike Milestone B's `TenantId` column, `true` is the semantically-correct default for every existing row, not an inert placeholder — no backfill logic or verification-of-emptiness needed, just the standard apply-and-`psql`-check pattern every prior migration in this plan used.

### Endpoints to add (18 total — see the plan's Milestone D steps 25–27 for full detail)

Flat routes, six per entity (create, list, get-by-id, rename, deactivate, reactivate):
```
POST/GET   /api/v1/organisations            GET/PATCH  /api/v1/organisations/{organisationId}
POST       /api/v1/organisations/{organisationId}/deactivate | /reactivate

POST/GET   /api/v1/locations                GET/PATCH  /api/v1/locations/{locationId}
POST       /api/v1/locations/{locationId}/deactivate | /reactivate

POST/GET   /api/v1/terminals                GET/PATCH  /api/v1/terminals/{terminalId}
POST       /api/v1/terminals/{terminalId}/deactivate | /reactivate
```

### Request/response DTOs to add

Per entity (in the new `OrganisationEndpoints.cs`/`LocationEndpoints.cs`/`TerminalEndpoints.cs`, colocated with the endpoint mapping class, matching `AuthEndpoints.cs`'s existing convention): a `Create*Request` (required `Name`; `Location`/`Terminal` additionally carry the required parent id — `OrganisationId`/`LocationId`; all three carry an **optional `TenantId` that must be null or the request is rejected with 400** — this is how "reject with 400 Bad Request" is actually implemented, since ASP.NET Core's default `PropertyNameCaseInsensitive` JSON binding will populate it if a client sends `tenantId` in any casing), an `Update*Request` (required `Name` only, same `TenantId` rule), and a `*Response` (`Id`, `TenantId`, the parent id where applicable, `Name`, `IsActive`, `CreatedAtUtc`). Full field lists are in the plan document itself, not repeated here.

### Authorization checks per endpoint

- Every endpoint requires the matching permission (`organisations.manage` / `locations.manage` / `terminals.manage`) via the existing `RequirePermissionFilter`, now also passing `rejectStaffPin: true`.
- Every entity lookup goes through the tenant-filtered `DbSet` (EF Core's existing fail-closed global query filter, unchanged) — a route/body id for a row outside the caller's tenant simply doesn't resolve, surfacing as 404.
- **Organisation** endpoints stop there — no further scoping below "the caller's tenant." Rationale: `organisations.manage` is `SystemAdmin`-only, and a tenant may own more than one `Organisation`; restricting to the caller's own `AuthContext.OrganisationId` would make a `SystemAdmin` unable to manage a second organisation they'd just created. Flagged explicitly in the plan as an interpretation call, not a re-litigation of ADR-0015.
- **Location** and **Terminal** endpoints add one more check: the resolved (or, for create, the requested) parent `OrganisationId` must equal `AuthContext.OrganisationId` — the literal ADR-0015 Context Provenance example — else 404, never 403 (don't confirm the row exists under a different organisation).
- **Terminal** specifically has to walk `Terminal → Location → OrganisationId` since `Terminal` has no `OrganisationId` column of its own and no EF navigation property to `Location` exists today (`TerminalConfiguration.cs` only defines the FK, not a navigation) — implemented as an explicit second lookup or LINQ join, not `.Include()`.

### How `rejectStaffPin` is represented

A `bool rejectStaffPin = false` parameter added to both `RequirePermissionFilter`'s constructor and the `RequirePermission<TBuilder>` extension method. Default `false` preserves every existing Milestone C call site (`/auth/logout`, `/auth/me` use `.RequireAuthorization()` directly, not `RequirePermission`, so they're unaffected regardless). When `true`, the filter independently checks `AuthContext.AuthMethod == AuthMethod.LocalStaffPin` and returns 403 if so — evaluated as a separate condition from the permission-code check, not folded into it, so a future misconfigured role can't accidentally bypass this by matching the permission string. Proven for now via a unit test constructing an `AuthContext` with `AuthMethod.LocalStaffPin` directly (no real staff-PIN login exists until Milestone F) — this was explicitly called out as acceptable in the task instructions and mirrors how `RequirePermissionFilterTests.cs` already builds contexts without a real HTTP round-trip.

### How route/body IDs are validated against server-side tenant context

Three layers, all already-established patterns applied consistently, not new mechanisms:
1. `TenantId` is never accepted as an override — present-and-non-null in a request body is a hard 400, not a soft ignore.
2. Every entity fetch uses the tenant-filtered `DbSet`, so a foreign tenant's row is invisible before any application code runs.
3. Parent-scoping ids that survive the tenant filter (an `OrganisationId` on a location-create body, a resolved location's `OrganisationId` on any location/terminal operation) are explicitly compared against `AuthContext.OrganisationId`, sourced only from the caller's validated session — never trusted as-is from the request.

### Tests to add/update

`OrganisationEndpointsTests.cs`, `LocationEndpointsTests.cs`, `TerminalEndpointsTests.cs` (new, one per entity) plus a new shared `Support/RbacTestSeeder.cs` helper (seeds `Tenant`+`Organisation`+`User`+`UserRole` for a named seeded role, logs in, returns the token — avoids near-duplicating `LocalUserLoginTests.cs`'s `SeedTestUserAsync` three times with no role/permission assignment, which that helper doesn't do today). Each entity's test file covers: happy-path create/list/get/update/deactivate/reactivate; 400 on a client-supplied `TenantId` (create and update); 403 for a role lacking the permission; 404 for a different tenant's row; and, for Location/Terminal only, 404 for a *different organisation within the same tenant*'s row (requires seeding two organisations under one tenant — the specific case that exercises the `AuthContext.OrganisationId` check independently of the tenant filter). Plus an `AuditEvent` row assertion per lifecycle action. `RequirePermissionFilterTests.cs` gets three new cases for the `rejectStaffPin` parameter (staff-pin-rejected-even-with-permission; unaffected admin session with the flag on; unaffected session with the flag at its default `false`).

### Docs to update (after implementation, not this session)

`docs/architecture/tenancy.md` (the `AuthContext.OrganisationId` cross-check now has a concrete implementation to point to), `docs/architecture/multi-location.md` (Location/Terminal CRUD implemented), `docs/modules/audit.md` (three new lifecycle event types), this plan doc's Status section and Milestone D checkbox, and this worker-notes file's own "Milestone D Report" section (mirroring the Milestone A/B/C report structure).

### Ambiguity/risk flagged

- **Resolved via the AskUserQuestion above:** CRUD scope (this session's main open question).
- **Flagged, not blocking:** the Organisation-scopes-to-tenant / Location-and-Terminal-scope-to-`AuthContext.OrganisationId` asymmetry is this plan's own reading of ADR-0015's example, applied to a case the ADR didn't spell out explicitly (what does "the caller's own resource" mean for the top-level entity with no parent). Low risk — it only matters once a tenant actually has more than one `Organisation`, which nothing before Milestone D creates — but flagged in the plan's Design Decisions for the human to override if a stricter reading is preferred.
- **Flagged, not blocking:** `IsActive` deactivation is deliberately non-cascading (deactivating an `Organisation` does not touch its `Location`/`Terminal` rows), per the explicit "do not add complex lifecycle rules yet" instruction. A future milestone will need to decide what, if anything, an inactive parent should mean for its children — not solved here.
- **No new ADR required.** Milestone D operates entirely inside ADR-0013/ADR-0015's already-accepted model; nothing here is a new architecture decision.
- **No new permission codes.** Read and write operations on the same entity share one permission code (`organisations.manage` gates both `GET` and `POST`/`PATCH`), matching the plan's existing "intentionally minimal" catalogue — flagged here only so a future worker doesn't assume a `*.read` code exists.

### Recommended next session

1. Human reviews this Milestone D plan (this file + the plan document's Milestone D section) and approves, amends, or rejects.
2. On approval, implement in the order the plan's steps 22–29 lay out — `rejectStaffPin` first (smallest, most isolated, unlocks the "apply to every endpoint" requirement for everything after it), then the `IsActive` migration, then the three lifecycle domain events + handlers, then the three endpoint files in Organisation → Location → Terminal order (each depends conceptually on the previous one's patterns), then the shared test seeder and the three test files.
3. Update this plan's Status section and this notes file with a "Milestone D Report" once implemented, before starting Milestone E — do not let the report lag behind the code, per CLAUDE.md's plan-refresh rule and this plan's own repeated practice in Milestones A–C.

---

## Milestone D Report (2026-07-02)

Human approved the Milestone D plan with four clarifications not fully spelled out in the planning pass: (1) do not call this "full CRUD" in any documentation — use "create / read / list / update basic editable fields / deactivate / reactivate"; (2) `IsActive` must **not** be added to the global EF tenant query filter — tenant isolation and lifecycle visibility are separate concerns; (3) list endpoints hide inactive records by default, but single `GET` may return an inactive record for a manage-permission caller; (4) deactivate/reactivate must be able to find their target row even when it's already inactive. All four were incorporated (see the plan's Design Decisions, updated). TDD used for the one piece with real branching logic (`rejectStaffPin`); the rest (entity properties, EF config, domain events, endpoint handlers) were implemented directly and proven via the acceptance-style HTTP test files, consistent with how `LocalUserLoginTests`/`TenantIsolationTests` were handled in earlier milestones — a CRUD-lifecycle-plus-authorization flow spanning entity → migration → endpoint → audit handler doesn't have a meaningful "fail for the right unit reason" one piece at a time.

### Files changed

New:
- `src/DaxaPos.Domain/Events/OrganisationLifecycleDomainEvent.cs`, `LocationLifecycleDomainEvent.cs`, `TerminalLifecycleDomainEvent.cs`.
- `src/DaxaPos.Api/Endpoints/Identity/OrganisationEndpoints.cs`, `LocationEndpoints.cs`, `TerminalEndpoints.cs`.
- `src/DaxaPos.Persistence/Migrations/20260701171012_AddIsActiveToOrganisationLocationTerminal.cs` (+ `.Designer.cs`, + updated `DaxaDbContextModelSnapshot.cs`).
- `tests/DaxaPos.Api.Tests/Support/RbacTestSeeder.cs`.
- `tests/DaxaPos.Api.Tests/OrganisationEndpointsTests.cs` (12 tests), `LocationEndpointsTests.cs` (12 tests), `TerminalEndpointsTests.cs` (12 tests).

Modified:
- `src/DaxaPos.Domain/Entities/Organisation.cs`, `Location.cs`, `Terminal.cs` — added `IsActive` (bool, default `true`).
- `src/DaxaPos.Persistence/Configurations/OrganisationConfiguration.cs`, `LocationConfiguration.cs`, `TerminalConfiguration.cs` — `IsActive` column (`HasDefaultValue(true)`), explicitly **not** referenced by any query filter (that lives only in `DaxaDbContext`, and remains `TenantId`-only).
- `src/DaxaPos.Api/Authorization/RequirePermissionFilter.cs` — added `rejectStaffPin` parameter (default `false`) to the filter constructor and the `RequirePermission` extension method.
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — added `OrganisationLifecycleAuditHandler`, `LocationLifecycleAuditHandler`, `TerminalLifecycleAuditHandler`.
- `src/DaxaPos.Api/Program.cs` — registered the three new audit handlers; mapped `MapOrganisationEndpoints()`, `MapLocationEndpoints()`, `MapTerminalEndpoints()`.
- `tests/DaxaPos.Api.Tests/RequirePermissionFilterTests.cs` — added 3 tests for `rejectStaffPin` (rejects `LocalStaffPin` even with matching permission; unaffected `LocalUsernamePassword` session with the flag on; unaffected session with the flag at its default `false`).
- `docs/architecture/tenancy.md`, `multi-location.md`, `docs/modules/audit.md` — implementation-status notes.
- This plan document (Status/Milestone D section, Design Decisions) and this notes file.

### Migration created

`AddIsActiveToOrganisationLocationTerminal` (`20260701171012_AddIsActiveToOrganisationLocationTerminal`). Adds `IsActive boolean not null default true` to `organisations`, `locations`, `terminals`. Verified twice: once applied incrementally on top of the existing dev database (`psql \d` confirming the column/default on all three tables), and once again via `dotnet ef database drop --force` + `dotnet ef database update` from a completely empty database — all four migrations (`InitialCreate`, `AddTenantIsolationColumns`, `AddIdentityAndRbacCore`, `AddIsActiveToOrganisationLocationTerminal`) applied cleanly in sequence with no manual intervention.

### Endpoints implemented (18)

```
POST/GET /api/v1/organisations · GET/PATCH /api/v1/organisations/{id} · POST .../deactivate | /reactivate
POST/GET /api/v1/locations     · GET/PATCH /api/v1/locations/{id}     · POST .../deactivate | /reactivate
POST/GET /api/v1/terminals     · GET/PATCH /api/v1/terminals/{id}     · POST .../deactivate | /reactivate
```

All gated by `organisations.manage`/`locations.manage`/`terminals.manage` respectively, all with `rejectStaffPin: true`. No hard delete anywhere. Organisation endpoints scope to the caller's tenant only; Location/Terminal endpoints additionally cross-check `AuthContext.OrganisationId` (Terminal walks `Terminal → Location → OrganisationId`, since it has no `OrganisationId` column or navigation property). A client-supplied `TenantId` in any request body is rejected with 400. List endpoints exclude inactive rows; single `GET`, `PATCH`, `deactivate`, and `reactivate` do not filter on `IsActive`.

### A bug caught by the test suite, not by build

The first test run failed every write endpoint with a 500. Root cause: `AuditEvent.BeforeValue`/`AfterValue` are `jsonb` columns (per the existing, already-documented `AuditEventConfiguration.cs`), but the lifecycle domain events were built passing bare strings (a plain organisation name, or `bool.ToString()` for `IsActive`) straight through to those columns — Postgres correctly rejected `"Second Org"` and `"True"` as invalid JSON. Fixed by having each endpoint handler `JsonSerializer.Serialize` a small snapshot object (e.g. `{ "Name": "Second Org" }`, `{ "IsActive": true }`) before constructing the domain event, in all three endpoint files. This is exactly the kind of thing the plan's "tests to add" section existed to catch before merge — flagged here so a future worker adding a fourth lifecycle-audited entity doesn't repeat it.

### Tests added

36 new HTTP-level tests (12 per entity in `OrganisationEndpointsTests.cs`/`LocationEndpointsTests.cs`/`TerminalEndpointsTests.cs`) plus 3 new unit-level tests in `RequirePermissionFilterTests.cs`. Coverage per entity: happy-path create/list/get/update/deactivate/reactivate; list hides inactive by default while `GET`-by-id still finds it; 400 on a client-supplied `TenantId` (create and update); 403 for a role lacking the permission; 404 for a different tenant's row (read, update, deactivate, reactivate); for Location/Terminal only, 404 for a *different organisation within the same tenant*'s row (create, read, update, deactivate/reactivate, and confirmed absent from that caller's list) — this is the case that specifically exercises the `AuthContext.OrganisationId` check independently of the tenant filter; and an `AuditEvent` row assertion covering all four lifecycle actions in one test per entity.

### Commands run

```
dotnet build tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj      (RED, rejectStaffPin constructor param didn't exist)
dotnet build src/DaxaPos.Api/DaxaPos.Api.csproj                     (GREEN, after RequirePermissionFilter change)
dotnet ef migrations add AddIsActiveToOrganisationLocationTerminal \
  --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj \
  --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj --output-dir Migrations
dotnet ef database update --project src/DaxaPos.Persistence/... --startup-project src/DaxaPos.Api/...
docker compose exec -T db psql -U daxapos -d daxapos -c "\d organisations" -c "\d locations" -c "\d terminals"
dotnet build DaxaPos.sln
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~OrganisationEndpointsTests"   (initial 500s, then fixed, then 12/12)
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~LocationEndpointsTests"       (12/12)
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~TerminalEndpointsTests"       (12/12)
dotnet test DaxaPos.sln                                                                                                 (92/92)
dotnet ef database drop --force --project src/DaxaPos.Persistence/... --startup-project src/DaxaPos.Api/...
dotnet ef database update --project src/DaxaPos.Persistence/... --startup-project src/DaxaPos.Api/...                   (fresh-DB migration verification)
dotnet test DaxaPos.sln                                                                                                 (92/92 again, against the freshly-migrated DB)
docker compose ps                                                                                                        (confirmed Keycloak still not running throughout)
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, all four migrations applied fresh, `keycloak` stopped) — **92/92 passed** (31 unit tests + 61 API tests: 1 health check + 6 tenant isolation + 6 permission filter + 12 login + 36 new Milestone D endpoint tests), 0 failed, 0 skipped.

### Working tree status

**Committed as `c592b49`** (2026-07-02, `feat(identity): add organisation location terminal management endpoints`), after human review and approval. (This section originally read "Not committed"; corrected 2026-07-03 during Milestone G housekeeping, per the stale-wording flag left by the Milestone F planning pass.)

### Deferred / not built (explicitly out of scope, per the task's scope guard)

`StaffMember`, staff PIN login, orders, tax, payments, Stripe, receipts, sync, UI, KDS, `DaxaPos.Modules.*`, Keycloak JWT wiring — none touched. Tenant query filters unchanged (still `TenantId`-only, fail-closed). No client-supplied tenant ID is trusted anywhere. No raw secret/PIN/password/session-token storage introduced.

### Blockers before Milestone E

None. `dotnet build`/`dotnet test` are clean against a freshly-migrated database, Keycloak remains stopped and unused, and nothing added strays into Milestone E–H territory (no `DeviceCredential`, no `DeviceRegistrationPin`, no `StaffMember`, no `Modules.*`).

---

*Milestone D completed: 2026-07-02, committed as `c592b49`.*

---

## Milestone E Planning Pass (2026-07-02)

No code written this session — planning only, per explicit instruction to stop after the plan and wait for approval. Full context re-read: the plan doc, this notes file, ADR-0008 (device identity, registration PIN model, rotation model, audit requirements), ADR-0013, ADR-0015, and current source for `SessionAuthenticationHandler`, `RequirePermissionFilter`, `AuthEndpoints`, `Organisation`/`Location`/`Terminal` endpoints, `DaxaDbContext` filters, `Device`/`DeviceType`/`Terminal`/`AuthSession`/`AuditEvent` entities, `HmacDeviceCredentialHasher`, `RandomSessionTokenService`, and `Program.cs`.

### Scope confirmed

Milestone E = plan steps 24–31: `DeviceCredential` + `DeviceRegistrationPin` entities, one migration, device-lifecycle domain events + audit handlers, registration-PIN issuance endpoint, pre-auth PIN-gated device registration, `DeviceTokenAuthenticationHandler` (zero-permission partial `AuthContext`), rotate/revoke/list endpoints, and `DeviceRegistrationTests`. Explicitly **not** in scope (per the task's scope guard and the plan's milestone ordering): `StaffMember`, staff PIN login, orders/tax/payments/receipts/sync/UI/KDS, `Modules.*`, Keycloak wiring.

### Plan-level corrections proposed (need human approval before implementation)

1. **Route shape conflict.** Steps 27/30 sketch nested routes (`POST /api/v1/locations/{locationId}/device-registration-pins`, `GET /api/v1/locations/{locationId}/devices`), written before the human's Milestone D flat-route decision existed. Recommendation: flat routes (`POST /api/v1/device-registration-pins` with `LocationId` in the body; `GET /api/v1/devices?locationId=`) for consistency with the now-established convention — the parent id is cross-checked against `AuthContext.OrganisationId` identically either way.
2. **Device-token lookup mechanism correction.** Step 29 says the handler "hashes it, looks up `DeviceCredential`" — impossible as written, because `HmacDeviceCredentialHasher` uses a random per-record salt, so hashing the presented secret is non-deterministic and cannot be a DB lookup key (the same reason `AuthSession` lookup works is that `ISessionTokenService.Hash` is deterministic SHA-256). Recommendation: the device token issued at registration is `{deviceCredentialId}.{secret}`; the handler splits it, loads that credential row by primary key (`IgnoreQueryFilters()` — the third documented bootstrap call site), and `Verify()`s the secret against the salted hash in constant time. Matches ADR-0008's `DeviceCredentialReference` concept. The alternative (deterministic SHA-256 for device secrets, like session tokens) was considered and is defensible for high-entropy secrets, but the id-prefixed token keeps the already-built, already-tested salted hasher and gives an indexed lookup.
3. **PIN validation is a verify-scan, not a hash lookup** — same salted-hash reasoning. The registration endpoint loads all live candidate PINs (unexpired, unrevoked, uses remaining) via the documented `IgnoreQueryFilters()` bootstrap path and `Verify()`s the presented PIN against each. The live-PIN set is tiny (15-minute expiry, low `MaxUses`). If **more than one** live PIN verifies (a 1-in-10⁶ cross-tenant collision), registration fails closed per ADR-0015's "ambiguous → zero rows" rule rather than picking one.
4. **Drop `DeviceCredential.Salt` as a separate column** (step 24 lists it) — the salt is already embedded in the `{salt}.{hash}` string `HmacDeviceCredentialHasher` produces, exactly like `User.PasswordHash` and every other credential column to date.
5. **Add a fifth domain event, `DeviceRegistrationPinCreatedDomainEvent`** — ADR-0008's audit requirements explicitly list "PIN generated," which step 26's four events don't cover.
6. **Unknown-PIN failures write no `AuditEvent` row** — when no PIN row matches, there is no tenant to attach the row to (`AuditEvent.TenantId` is non-nullable by design), exactly the Milestone C unknown-email precedent. `DeviceRegistrationFailedDomainEvent` fires only when a real PIN row matched but was expired/exhausted/revoked. Rate limiting + server logs cover the unknown-PIN case.
7. **No `Device` schema changes.** Revocation state lives on `DeviceCredential` (`Status`, `RevokedAtUtc`); "revoke device" revokes all of that device's non-revoked credentials, after which the device cannot authenticate (fail closed: no active credential → no `AuthContext`). A revoked device re-registers as a **new** `Device` row per ADR-0008 ("treated as a new registration"). No `IsActive` on `Device` this milestone.
8. **No PIN-revoke endpoint this milestone.** `DeviceRegistrationPin.RevokedAtUtc` exists in the schema (step 24) and is respected by validation, but with a 15-minute expiry a dedicated revoke endpoint is deferred; flagged so a future milestone can add it without a schema change.

### Design decisions proposed (implementation-level, recorded before coding)

- **Policy values** in a new `DeviceRegistrationPinPolicy` (Application/Identity, same pattern as `LoginLockoutPolicy`): 6-digit numeric PIN (ADR-0008), 15-minute expiry, `MaxUses` default 1 (request may set 1–10, else 400).
- **Rate limiting** via ASP.NET Core's built-in rate limiter: a fixed-window policy (10 attempts/minute, partitioned by remote IP) applied only to `POST /api/v1/device-registration`. Constants live beside the policy class. The rate-limit test uses its own `WebApplicationFactory` instance so tripping the limiter can't poison sibling tests.
- **Scheme selection**: register `DeviceTokenAuthenticationHandler` under scheme `"DeviceToken"` with header form `Authorization: Device {credentialId}.{secret}`, and make the default authentication scheme a policy scheme that forwards to `Session` (header starts with `Bearer`) or `DeviceToken` (starts with `Device`) — so existing `.RequireAuthorization()` endpoints accept both without touching every call site. A device-token `AuthContext` carries empty `Roles`/`Permissions`, so every `RequirePermission`-gated endpoint returns 403 for it — proven by tests, per the plan's "a device token alone grants nothing" domain assumption.
- **Registration response** returns, once: `DeviceId`, `TenantId`, `OrganisationId`, `LocationId`, `DeviceType`, `Name`, and the raw `DeviceToken` — the fields ADR-0008 says an installed/PWA device stores locally. Tenant/org/location come exclusively from the matched PIN row, never from the request body.
- **`DeviceType`** arrives as a string in the request and must parse (case-insensitive) to the existing `DeviceType` enum, else 400.

### Recommended next session

Human reviews the Milestone E plan (chat summary + this section). On approval: implement in order — entities/configs/migration → policy class + unit tests → PIN-issuance endpoint → registration endpoint + rate limiter → `DeviceTokenAuthenticationHandler` + policy scheme → rotate/revoke/list → HTTP test files → docs → plan status update. Update the plan doc's Milestone E steps with the approved corrections before writing code.

---

## Milestone E Report (2026-07-02)

Human approved the Milestone E plan with amendments (recorded verbatim-equivalent in the plan's "Human Decisions Needed" entry dated 2026-07-02 (Milestone E)): flat routes plus a new `POST /api/v1/device-registration-pins/{pinId}/revoke`; the `Device {credentialId}.{secret}` token format; verify-scan PIN validation with fail-closed ambiguity handling; unknown-PIN attempts unaudited (no tenant for the non-nullable `AuditEvent.TenantId`); six audit events; no `DeviceCredential.Salt` column; no `Device.IsActive`; 15-min expiry / MaxUses 1–10 / 10-per-minute-per-IP rate limit. Scope guard fully observed: no `StaffMember`, no staff PIN login, no orders/tax/payments/Stripe/receipts/sync/UI/KDS, no `Modules.*`, no Keycloak wiring, no localisation, tenant filters untouched (two filters *added*, none weakened), no raw secret/PIN/password/token stored anywhere.

TDD used for the pure-logic piece (`DeviceRegistrationPinPolicy` — test written first, RED confirmed via compile failure, then GREEN, 13 unit tests). The vertical slice (entities → migration → auth handler → endpoints → audit handlers) was proven via acceptance-style HTTP tests, consistent with Milestones B–D. All 35 new HTTP tests passed on the first run after implementation.

### Files changed

New:
- `src/DaxaPos.Domain/Entities/DeviceCredential.cs`, `DeviceCredentialStatus.cs`, `DeviceRegistrationPin.cs`.
- `src/DaxaPos.Domain/Events/DeviceRegistrationPinCreatedDomainEvent.cs`, `DeviceRegistrationPinRevokedDomainEvent.cs`, `DeviceRegisteredDomainEvent.cs`, `DeviceRegistrationFailedDomainEvent.cs`, `DeviceCredentialRotatedDomainEvent.cs`, `DeviceRevokedDomainEvent.cs`.
- `src/DaxaPos.Application/Identity/DeviceRegistrationPinPolicy.cs`.
- `src/DaxaPos.Persistence/Configurations/DeviceCredentialConfiguration.cs`, `DeviceRegistrationPinConfiguration.cs`.
- `src/DaxaPos.Persistence/Migrations/20260702033413_AddDeviceCredentialsAndRegistrationPins.cs` (+ `.Designer.cs`, updated snapshot).
- `src/DaxaPos.Api/Authentication/DeviceTokenAuthenticationHandler.cs`.
- `src/DaxaPos.Api/Endpoints/Identity/DeviceRegistrationPinEndpoints.cs`, `DeviceRegistrationEndpoints.cs`, `DeviceEndpoints.cs`.
- `tests/DaxaPos.UnitTests/Identity/DeviceRegistrationPinPolicyTests.cs` (13 tests).
- `tests/DaxaPos.Api.Tests/Support/DeviceTestHelper.cs`; `DeviceRegistrationPinEndpointsTests.cs` (13 tests), `DeviceRegistrationTests.cs` (10), `DeviceEndpointsTests.cs` (11), `DeviceRegistrationRateLimitTests.cs` (1).

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — two new `DbSet`s, two new fail-closed filters, bootstrap-callers comment updated.
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — six new handlers (JSON-serializing snapshots for the `jsonb` columns, per the Milestone D bug note).
- `src/DaxaPos.Api/Program.cs` — default authentication is now a policy scheme forwarding by Authorization-header prefix (`Bearer` → `Session`, `Device` → `DeviceToken`); built-in rate limiter wired (`UseRateLimiter` + a fixed-window per-remote-IP policy on the registration endpoint only, permit limit configuration-overridable via `DeviceRegistration:RateLimitPermitLimit` for tests); six audit-handler registrations; three endpoint maps.
- `docs/modules/devices.md`, `docs/modules/audit.md`, `docs/architecture/tenancy.md`, `docs/architecture/security.md` — Milestone E implementation-status sections.
- Plan doc (Status, Milestone E steps revised to the approved shape, Human Decisions entry #7) and this notes file.

### Migration created

`AddDeviceCredentialsAndRegistrationPins` (`20260702033413`). Creates `device_credentials` (salted-HMAC `CredentialHash`, string-converted `Status`, FKs to `devices`/`tenants`) and `device_registration_pins` (hashed `PinHash`, FKs to `tenants`/`organisations`/`locations`/`users`, indexed `CreatedAtUtc` for the bounded candidate scan). Verified twice: applied incrementally with `psql \d` column/index/FK checks, and from a completely empty database (`dotnet ef database drop --force` + `update` — all five migrations apply cleanly in sequence).

### Design/implementation notes worth flagging to a future reader

- **Decisions 3 and 4 reconciliation:** validation only *accepts* live PINs, but the candidate scan covers rows created in the last 24 hours regardless of state — that bounded window is what lets a matched-but-expired/revoked/exhausted attempt be audited against its tenant (decision 4) without an unbounded all-rows scan. Ambiguity (fail-closed) is judged on **live** matches only; a dead digit-collision alongside one live match does not block registration.
- **Ambiguity audit rule as implemented:** >1 live match → 401; audited (`Reason = "AmbiguousPinMatch"`, `EntityId = null`) only when all matched rows share one `TenantId`, with organisation/location included only when they also agree. Cross-tenant collisions write nothing.
- **Recorded gap (per the human's explicit instruction):** unauthenticated/global security events — unknown registration-PIN attempts, and Milestone C's unknown-email login failures — cannot be audited in `audit_events` because `TenantId` is non-nullable by design. If these need auditing, a separate tenant-less security-event table is required; noted in `docs/modules/audit.md` as a future decision, not solved here.
- **Rotation of a device with no active credential returns 409** — revocation is terminal (approved: revoked/lost devices re-register as a new `Device`), so rotate must not resurrect a revoked device. Covered by `Rotate_OnARevokedDevice_ReturnsConflict`.
- **Deferred risk (accepted 2026-07-02): registration-PIN `MaxUses` is not concurrency-safe under simultaneous registration attempts.** Two registrations racing the last use of a PIN could both pass the in-memory `UsedCount < MaxUses` check (no row lock, no optimistic concurrency token, no atomic UPDATE). Correct for normal MVP usage — the window is milliseconds on a 15-minute single-venue enrolment secret — the race is known and deferred, not silently ignored. A future fix may use row locking, optimistic concurrency (a concurrency token on `UsedCount`), or an atomic conditional update.
- **Deferred risk (accepted 2026-07-02): device authentication does not check inactive parent lifecycle state.** `DeviceTokenAuthenticationHandler` validates that the credential is `Active`; it does **not** block authentication because a parent `Organisation`/`Location`/`Terminal` has `IsActive = false` — deactivating a location today does not cut off its devices' tokens. Deferred because cascading lifecycle rules were explicitly out of scope for Milestones D and E ("no complex lifecycle rules yet"); the future milestone that decides what an inactive parent means for its children (already flagged in the Milestone D planning pass) should revisit device auth at the same time.
- **Rate-limit testability:** the permit limit reads `DeviceRegistration:RateLimitPermitLimit` from configuration (default = the approved 10/minute). The three device test classes raise it to 1000 via `UseSetting` so their many registration calls never trip it; `DeviceRegistrationRateLimitTests` builds its own isolated factory at the default limit and asserts the 11th attempt gets 429. In production nothing sets the override, so the approved default applies.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj          (RED — policy class didn't exist)
dotnet test tests/DaxaPos.UnitTests/... --filter DeviceRegistrationPinPolicyTests   (GREEN, 13/13)
dotnet build DaxaPos.sln                                                (0 warnings, 0 errors)
dotnet ef migrations add AddDeviceCredentialsAndRegistrationPins --project src/DaxaPos.Persistence/... --startup-project src/DaxaPos.Api/...
dotnet ef database update ...                                           (applied)
docker compose exec -T db psql -U daxapos -d daxapos -c "\d device_credentials" -c "\d device_registration_pins"
dotnet test tests/DaxaPos.Api.Tests/... --filter "Device..."            (35/35, first run)
dotnet test DaxaPos.sln                                                 (140/140)
dotnet ef database drop --force ... && dotnet ef database update ...    (fresh-DB verification, 5 migrations in sequence)
dotnet test DaxaPos.sln                                                 (140/140 again, freshly-migrated DB)
docker compose ps                                                       (Keycloak not running throughout)
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, all five migrations applied fresh, `keycloak` stopped) — **140/140 passed** (44 unit tests + 96 API tests: 1 health check + 6 tenant isolation + 6 permission filter + 12 login + 36 Milestone D + 35 Milestone E), 0 failed, 0 skipped.

### Working tree status

**Committed as `06bebfe`** (2026-07-02, `feat(devices): add device registration and credentials`), after human review and approval.

### Blockers before Milestone F

None. Milestone F (StaffMember + staff PIN login) now has everything it needs: the `DeviceToken` scheme provides the trusted-device context its login endpoint requires, `rejectStaffPin` is already applied to every sensitive endpoint built so far, and `AuthSession`/`LoginLockoutPolicy`/`Pbkdf2PinHasher` are ready to reuse. Nothing added strays into Milestone F–H territory (no `StaffMember`, no staff-PIN login endpoint).

---

*Milestone E completed: 2026-07-02, committed as `06bebfe`.*

---

## Milestone F Planning Pass (2026-07-02)

No code written this session — planning only, per explicit instruction to stop after the plan and wait for approval. Full context re-read: the plan doc, this notes file, ADR-0013, ADR-0015, and current source for `AuthEndpoints`, `SessionAuthenticationHandler`, `DeviceTokenAuthenticationHandler`, `RequirePermissionFilter`, `AuthSession`, `User`, `UserRole`, `AuditEvent`, `AuthContext`, `Permissions`, `LoginLockoutPolicy`, `SessionExpiryPolicy`, `Pbkdf2PinHasher` usage, `DaxaDbContext` filters, and `Program.cs`. Housekeeping done first: Milestone E's "not yet committed / awaiting approval" wording in the plan Status and this file corrected to record commit `06bebfe`.

### Scope confirmed

Milestone F = plan steps 32–38: `StaffMember` + `StaffMemberRole` entities, one migration, staff lifecycle/login domain events + audit handlers, staff-member management endpoints (`staff.manage`, `rejectStaffPin: true`), `POST /api/v1/auth/staff-pin/login` requiring a trusted `DeviceToken` context, emergency disable with immediate session revocation, and `StaffPinLoginTests`. Explicitly **not** in scope: orders/tax/payments/Stripe/receipts/sync/UI/KDS, `Modules.*`, Keycloak/OIDC wiring, ADR-0016 localisation, weakening tenant filters, trusting client-supplied tenant IDs.

### Key structural properties (verified against current source, not assumed)

- **No new `IgnoreQueryFilters()` bootstrap call sites are needed for staff PIN login.** Unlike the email-login and device-token handlers, the staff-PIN login endpoint runs *after* `DeviceTokenAuthenticationHandler` has stashed a partial `AuthContext`, so `CurrentTenantProvider` already returns the device's `TenantId` — the `StaffMember` lookup happens under the normal fail-closed tenant filter. The bootstrap-callers comment in `DaxaDbContext` stays accurate at four call sites.
- **`AuthSession.StaffMemberId` already exists** (Milestone C), deliberately without an FK; the Milestone F migration adds the FK to the new `staff_members` table additively, per the comment left on the entity.
- **`rejectStaffPin` is already implemented and applied** to every sensitive endpoint (pulled forward in Milestone D); Milestone F's job is the first *end-to-end* proof with a real `LocalStaffPin` HTTP session.
- **`LoginLockoutPolicy`/`SessionExpiryPolicy`/`Pbkdf2PinHasher`/`ISessionTokenService` are reused as-is** — no second hashing scheme, no new policy values beyond a small `StaffPinPolicy` (format rules), matching Decision 3 (4-digit-minimum PIN, 5 attempts/15 min lockout, 12h/8h two-part expiry shared by both `AuthSession` flows for MVP).
- **No verify-scan is needed** (unlike registration PINs): `StaffCode` is the lookup key (unique per organisation, resolved under the tenant filter + device's organisation), then exactly one `PinHash` is verified — PINs themselves need no uniqueness.

### Plan-level corrections/additions proposed (need human approval before implementation)

1. **Flat route for staff-member creation.** Step 35's `POST /api/v1/organisations/{organisationId}/staff-members` predates the Milestone D flat-route decision. Proposed: `POST /api/v1/staff-members` with `LocationId` in the body; organisation derives from the resolved location and is cross-checked against `AuthContext.OrganisationId` (404 on mismatch), exactly the Milestone D Location pattern.
2. **A `GET /api/v1/staff-members?locationId=` list endpoint** (parity with `GET /api/v1/devices?locationId=`), not in step 35's original list.
3. **A fourth domain event, `StaffMemberLifecycleDomainEvent`** (`Action`: `"Created"` / `"PinReset"` / `"RoleAssigned"`), Milestone D single-event-with-Action pattern — staff creation, PIN reset, and role assignment are identity/permission changes ADR-0013 says must be audited; step 34's three events don't cover them. `StaffMemberDisabledDomainEvent` stays its own event (it also carries session-revocation semantics).
4. **Unknown staff-code login attempts ARE audited** — a deliberate, justified departure from the unknown-email (Milestone C) and unknown-PIN (Milestone E) precedents: here the tenant is already known from the trusted device context, so `AuditEvent.TenantId` is satisfiable (`Reason = "UnknownStaffCode"`, `StaffMemberId = null`).
5. **PIN reset semantics:** admin supplies the new PIN in the request body (policy-validated); reset clears `FailedPinAttempts`/`LockedOutUntilUtc` and revokes the staff member's active sessions.
6. **Sensitive-permission login guard (step 36):** the check set is defined as all eight current `Permissions` catalogue codes (every one is gated `rejectStaffPin: true` where mapped); login fails with a generic 401 and an audited `Reason = "RoleGrantsSensitivePermissions"` configuration error.

### Deferred/flagged, not built

- `TerminalId` remains null on staff sessions — device→terminal mapping doesn't exist yet.
- Staff "fast switching" is just per-staff login/logout; multiple concurrent staff sessions per device are allowed in MVP.
- Shorter/configurable staff-session lifetime (ADR-0013 "short-lived") — Decision 3 applies the shared 12h/8h policy for MVP.
- ~~The Milestone D report in this file still carries stale "Not committed" wording (D was committed as `c592b49`); left untouched per the explicit E-only housekeeping instruction — flagged for the human.~~ Corrected 2026-07-03 during Milestone G housekeeping.

---

## Milestone F Report (2026-07-02)

Human approved the Milestone F plan with thirteen explicit decisions (recorded verbatim-equivalent in the plan's "Human Decisions Needed" entry dated 2026-07-02 (Milestone F)). The notable amendments over the planning pass: `StaffCode` is **alphanumeric** (uppercase letters + digits, 2–20, uppercase-normalised), not numeric-only; staff PIN sessions get their own shorter expiry (**8h absolute / 30min idle**, `StaffSessionExpiryPolicy`) instead of reusing the admin 12h/8h policy; and PIN reset is **server-generated** (raw PIN returned once, cryptographic RNG) rather than admin-supplied. Scope guard fully observed: no orders/tax/payments/Stripe/receipts/sync/UI/KDS, no `Modules.*`, no Keycloak wiring, no localisation, tenant filters untouched (two filters *added*, none weakened), no client-supplied tenant ID trusted anywhere, no raw secret/PIN/password/token stored anywhere.

TDD used for the pure-logic pieces (`StaffCodePolicy`/`StaffPinPolicy`/`StaffSessionExpiryPolicy` — tests written first, RED confirmed via compile failure, then GREEN, 37 unit tests). The vertical slice (entities → migration → login endpoint → management endpoints → audit handlers) was proven via acceptance-style HTTP tests, consistent with Milestones B–E. All 38 new HTTP tests passed on the first run after implementation.

### Files changed

New:
- `src/DaxaPos.Application/Identity/StaffCodePolicy.cs`, `StaffPinPolicy.cs`, `StaffSessionExpiryPolicy.cs`.
- `src/DaxaPos.Domain/Entities/StaffMember.cs`, `StaffMemberRole.cs`.
- `src/DaxaPos.Domain/Events/StaffMemberLifecycleDomainEvent.cs`, `StaffPinLoginSucceededDomainEvent.cs`, `StaffPinLoginFailedDomainEvent.cs`, `StaffMemberDisabledDomainEvent.cs`.
- `src/DaxaPos.Persistence/Configurations/StaffMemberConfiguration.cs` (unique `(OrganisationId, StaffCode)` index), `StaffMemberRoleConfiguration.cs`.
- `src/DaxaPos.Persistence/Migrations/20260702050118_AddStaffMembers.cs` (+ `.Designer.cs`, updated snapshot).
- `src/DaxaPos.Api/Endpoints/Identity/StaffMemberEndpoints.cs` (create/list/get/reset-pin/roles/disable, all `staff.manage` + `rejectStaffPin: true`).
- `tests/DaxaPos.UnitTests/Identity/StaffCodePolicyTests.cs` (16), `StaffPinPolicyTests.cs` (14), `StaffSessionExpiryPolicyTests.cs` (7).
- `tests/DaxaPos.Api.Tests/Support/StaffTestHelper.cs`; `StaffMemberEndpointsTests.cs` (21 test cases incl. theories), `StaffPinLoginTests.cs` (17).

Modified:
- `src/DaxaPos.Application/Identity/Permissions.cs` — added `Permissions.AdminSensitive` (all eight current codes) with the Decision 8 follow-up note in its doc comment.
- `src/DaxaPos.Domain/Entities/AuthSession.cs` — `StaffMemberId` comment updated (FK now exists).
- `src/DaxaPos.Persistence/Configurations/AuthSessionConfiguration.cs` — `StaffMember` FK added.
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — two new `DbSet`s + fail-closed filters, with a note that staff PIN login needs no bootstrap bypass.
- `src/DaxaPos.Api/Authentication/SessionAuthenticationHandler.cs` — expiry branches by `AuthMethod`: `LocalStaffPin` → `StaffSessionExpiryPolicy` (8h/30min), everything else → `SessionExpiryPolicy` (12h/8h).
- `src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs` — `POST /api/v1/auth/staff-pin/login` + `StaffPinLoginRequest`/`StaffPinLoginResponse`.
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — four new handlers (six audited event types: `StaffMemberCreated`/`StaffMemberPinReset`/`StaffMemberRoleAssigned` via the lifecycle event's `Action`, plus `StaffPinLoginSucceeded`/`StaffPinLoginFailed`/`StaffMemberDisabled`).
- `src/DaxaPos.Api/Program.cs` — four audit-handler registrations, `MapStaffMemberEndpoints()`.
- `docs/architecture/security.md`, `docs/architecture/tenancy.md`, `docs/modules/audit.md` — Milestone F implementation-status sections.
- Plan doc (Status, Milestone F steps revised to approved shape, Human Decisions entry #8) and this notes file.

### Migration created

`AddStaffMembers` (`20260702050118`). Creates `staff_members` (unique `(OrganisationId, StaffCode)` index; FKs to `tenants`/`organisations`/`locations`/`users` (LinkedUserId), all `RESTRICT`) and `staff_member_roles` (PK `(StaffMemberId, RoleId)`, mirroring `user_roles`; `LocationId` nullable scope column for later multi-location staff assignment), **and adds the deferred `auth_sessions.staff_member_id` FK** flagged in Milestone C. Verified twice: applied incrementally with `psql \d` checks, and from a completely dropped database — all six migrations apply cleanly in sequence.

### Design/implementation notes worth flagging to a future reader

- **Staff PIN login runs entirely under the tenant filter** — no new `IgnoreQueryFilters()` call sites. The `DeviceToken` authentication handler establishes the tenant context before the endpoint executes, so the `StaffMember` lookup, role/permission joins, and session insert are all normally filtered. The documented bootstrap set remains the four Milestone C–E call sites.
- **Failure-reason ordering in the login endpoint:** location-mismatch → unknown code → inactive → locked-out → home-location-mismatch → PIN verify. Lockout is checked *before* PIN verification, so a correct PIN during lockout is still rejected (audited `LockedOut`) and does not reset the counter. Failed attempts only increment on an actual PIN mismatch; wrong-location attempts don't count toward lockout.
- **Unknown staff-code attempts are audited** (deliberate departure from the Milestone C unknown-email and Milestone E unknown-PIN precedents): the device context supplies the tenant, so `AuditEvent.TenantId` is satisfiable. Client responses stay generic across all failure reasons.
- **The sensitive-permission login guard makes a misconfigured staff session impossible through the API**, so the end-to-end "staff session with a sensitive permission still gets 403" proof seeds the `AuthSession` row directly in the test (`StaffSession_MisconfiguredWithSensitivePermissions_...`) — demonstrating the endpoint-level `rejectStaffPin` net holds independently of the login-time guard.
- **Recorded follow-up (Decision 8):** `Permissions.AdminSensitive` is a hard-coded list of the current eight catalogue codes. Permission metadata/category should eventually define staff-PIN eligibility per permission — do not extend the hard-coded list indefinitely as later plans add permission codes.
- **Deferred (Decision 5):** `AuthSession.TerminalId` stays null on staff sessions — terminal-device mapping doesn't exist yet; staff sessions attach `TerminalId` once terminal assignment is implemented.
- **PIN reset and disable both revoke the staff member's active sessions** (reasons `"StaffPinReset"`/`"StaffMemberDisabled"` on the session rows) but deliberately raise only their own single audit event (with the revoked-session count in the payload) rather than an additional `AuthSessionRevoked` per session — one action, one audit row.
- Staff management endpoints follow the Milestone D conventions exactly: list hides inactive staff, single `GET` doesn't; disable is idempotent; cross-tenant and cross-organisation access is always 404, never 403.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj                 (RED — policy classes didn't exist)
dotnet test tests/DaxaPos.UnitTests/... --filter "Staff*PolicyTests"          (GREEN, 37/37)
dotnet ef migrations add AddStaffMembers --project src/DaxaPos.Persistence/... --startup-project src/DaxaPos.Api/...
dotnet ef database update ...                                                  (applied incrementally)
docker compose exec -T db psql -U daxapos -d daxapos -c "\d staff_members" -c "\d staff_member_roles"
dotnet build DaxaPos.sln                                                       (0 warnings, 0 errors)
dotnet test tests/DaxaPos.Api.Tests/... --filter "StaffMemberEndpointsTests|StaffPinLoginTests"   (38/38, first run)
dotnet test DaxaPos.sln                                                        (215/215)
dotnet ef database drop --force ... && dotnet ef database update ...           (fresh-DB verification, 6 migrations in sequence)
dotnet test DaxaPos.sln                                                        (215/215 again, freshly-migrated DB)
docker compose ps                                                              (Keycloak not running throughout)
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, all six migrations applied fresh, `keycloak` stopped) — **215/215 passed** (81 unit tests + 134 API tests: 1 health check + 6 tenant isolation + 6 permission filter + 12 login + 36 Milestone D + 35 Milestone E + 38 new Milestone F), 0 failed, 0 skipped.

### Working tree status

**Committed as `585cd39`** (2026-07-02, `feat(identity): add staff members and staff PIN login`), after human review and approval.

### Blockers before Milestone G

None. Milestone G (offline verification + consolidated RBAC tests) has everything it needs: both auth paths (`LocalUsernamePassword`, `LocalStaffPin`) exist end-to-end and already run with Keycloak stopped in every test session; `RbacTests.cs` can consolidate the cross-method assertions using `RbacTestSeeder`/`DeviceTestHelper`/`StaffTestHelper` as-is. Nothing added strays into Milestone G–H territory.

---

*Milestone F completed: 2026-07-02, committed as `585cd39`.*

---

## Milestone G Planning Pass (2026-07-03)

No code written this session — planning only, per explicit instruction to stop after the plan and wait for approval. Housekeeping done first: the plan Status and this file's Milestone F report corrected to record commit `585cd39`, and the Milestone D report's stale "Not committed" wording (flagged by the Milestone F planning pass) corrected to record `c592b49`. Context re-read: the plan doc, this notes file, ADR-0013, ADR-0015, and current source for the session/device-token authentication handlers, staff PIN login, `RequirePermissionFilter`, `DaxaDbContext` filters and bootstrap `IgnoreQueryFilters()` call sites, `Program.cs`, the full test tree, and `.github/workflows/ci.yml` (which defines **only** a Postgres service — no Keycloak container exists in CI at all).

Also noted: an untracked `docs/testing/local-smoke-test.md` exists in the working tree (a human-authored manual smoke-test walkthrough of the full Milestone A–F surface, written against commit `585cd39`). It is effectively Milestone G's manual precursor and is folded into this plan below.

### Goal and scope

Milestone G = plan steps 39–40 plus closing hardening. It is **test-and-documentation-only**: prove both Daxa WebAPI-native auth paths work with zero Keycloak/cloud dependency (ADR-0013's offline guarantee), consolidate cross-cutting authorization assertions across all auth methods into one place, add the `IgnoreQueryFilters()` usage guard ADR-0015 §Risks calls for, and convert the deferred risks accumulated in Milestones C–F from worker-note prose into real open issues. No new entities, no schema changes, no migrations, no new endpoints, no behaviour changes to product code.

### Entities/tables affected

None. `src/` is expected to be untouched (if a consolidated test exposes a real defect, fixing it would be flagged and approved separately, not silently folded in).

### Migrations required

None.

### Tests to add

1. **`tests/DaxaPos.Api.Tests/HybridOfflineLoginTests.cs`** (plan step 39): two end-to-end chain tests run with only Postgres available —
   - Admin chain: seed via `RbacTestSeeder` → `POST /auth/local/login` → `GET /auth/me` → a permission-gated endpoint succeeds → `POST /auth/logout` → old token rejected.
   - Staff chain: registration PIN issued → device registered → device-token `GET /auth/me` (empty roles/permissions) → `POST /auth/staff-pin/login` → staff `GET /auth/me` → a `rejectStaffPin: true` endpoint returns 403 → logout.
   These re-exercise flows individual test files already cover, deliberately in one place, as the named, self-documenting proof of ADR-0013's "local POS runtime authentication must not require live cloud access."
2. **`tests/DaxaPos.Api.Tests/RbacTests.cs`** (plan step 40): a consolidated authorization matrix driven by a shared inventory of every protected endpoint (one list, so future endpoints are added in one place): unauthenticated → 401 everywhere; garbage/revoked token → 401; admin session lacking the permission → 403; a real `LocalStaffPin` session → 403 on **every** `rejectStaffPin: true` endpoint (sweep, extending Milestone F's spot checks); a `DeviceToken` context → 403 on every permission-gated endpoint; a valid tenant-A session against tenant-B rows → 404/empty list, never 500 and never data. Reuses `RbacTestSeeder`/`DeviceTestHelper`/`StaffTestHelper` unchanged.
3. **`tests/DaxaPos.UnitTests/` — `IgnoreQueryFiltersUsageTests.cs`** (hardening): a source-scan guard test asserting `IgnoreQueryFilters()` occurs only in the documented bootstrap files (`BootstrapAdminSeeder`, `AuthEndpoints`, `SessionAuthenticationHandler`, `DeviceTokenAuthenticationHandler`, `DeviceRegistrationEndpoints`) — directly implementing ADR-0015 §Risks' "the plan's own tests should assert it is used in exactly the … documented location[s]," so a future contributor cannot quietly add an undocumented tenant-filter bypass.

### Offline/local (Keycloak-stopped) verification approach

- Local: `docker compose up -d db` only; `docker compose ps` recorded (keycloak absent/stopped); full `dotnet test DaxaPos.sln` (215 existing + new tests) against the real Postgres; API boot + `/health` Healthy — matching PLAN-0002's verification pattern.
- CI: `.github/workflows/ci.yml` defines only a `postgres` service, so every CI run is a standing, machine-enforced Keycloak-absent verification — no workflow change needed, recorded as the mechanism.
- Manual: adopt `docs/testing/local-smoke-test.md` (commit it, trimming its now-actioned self-referential "Proposal"/"Documentation observations" sections) and run its flow once against the finished milestone.

### Follow-up issues to create (per CLAUDE.md rule: every unresolved question is an open issue)

All are already-recorded deferrals living only in this notes file; Milestone G promotes them to `docs/issues/open/` + `docs/issues/index.md`:

- **OI-0011 — User management endpoints.** `users.manage` is seeded but no endpoint consumes it; consequence (found by the manual smoke test): a `SystemAdmin` can create a second organisation but can never mint a login inside it — the bootstrap-organisation dead end. Recommended: defer to its own small follow-up plan, not built in G (it adds endpoints, which G's test-only scope excludes) — **human decision requested**. *(Decided at approval, 2026-07-03: create the issue only; user-management endpoints are a follow-up implementation plan.)*
- **OI-0012 — Inactive-parent lifecycle semantics.** Device auth and staff login ignore parent `IsActive = false` (Milestone E deferred risk).
- **OI-0013 — Registration-PIN `MaxUses` concurrency race** (Milestone E deferred risk).
- **OI-0014 — Tenant-less security-event auditing** (unknown-email/unknown-PIN attempts unauditable while `AuditEvent.TenantId` is non-nullable; Milestones C/E).
- **OI-0015 — Permission metadata for staff-PIN eligibility** (replace the hard-coded `Permissions.AdminSensitive` list; Milestone F Decision 8 follow-up).

### Documentation updates

`docs/testing/local-smoke-test.md` (adopt/commit), `docs/testing/testing-strategy.md` + `docs/testing/security-tests.md` (consolidated RBAC/offline coverage notes), the five new open issues + `docs/issues/index.md`, plan Status/Milestone G checkbox, and this file's Milestone G report.

### Is PLAN-0003 complete after Milestone G?

**No — Milestone H is still required** (docs-only closeout: consolidate `docs/architecture/tenancy.md`/`security.md`/`multi-location.md` and `docs/modules/devices.md`/`audit.md` against what was actually built, finalise the plan Status, write handoff notes for PLAN-0004, move the plan to `docs/plans/completed/`). Milestone G is the last milestone that touches the test codebase; after G, the identity/tenancy/device foundation is code-complete pending H's documentation pass.

---

## Milestone G Report (2026-07-03)

Human approved the Milestone G plan as test-and-documentation-only, with the docs housekeeping/planning pass committed first (`60dedbe`: Milestone F status → `585cd39`, Milestone D stale wording → `c592b49`, the Milestone G planning pass, and the adopted `docs/testing/local-smoke-test.md` with its self-referential "Proposal"/"Documentation observations" sections trimmed). OI-0011 decided as issue-only — no user-management endpoints built. Scope guard fully observed: **zero `src/` files changed**, no entities, no migrations, no endpoints, no orders/tax/payments/Stripe/receipts/sync/UI/KDS, no `Modules.*`, no Keycloak wiring, no localisation, tenant filters untouched, no client-supplied tenant ID trusted, no raw credential stored.

These are verification tests of already-committed behaviour (the deliverable *is* the tests), so the Milestones B–F acceptance-test convention applies rather than red/green TDD: all 156 new tests passed on their first run, which is the expected outcome for consolidation tests over working code — a failure would have indicated a real defect to flag for separate approval (none appeared).

### Files changed

New:
- `tests/DaxaPos.Api.Tests/HybridOfflineLoginTests.cs` (2 chain tests — plan step 39).
- `tests/DaxaPos.Api.Tests/RbacTests.cs` (+ `RbacScenarioFixture`; 153 test cases — plan step 40).
- `tests/DaxaPos.UnitTests/Architecture/IgnoreQueryFiltersUsageTests.cs` (1 guard test — plan step 41).
- `docs/issues/open/OI-0011-user-management-endpoints.md`, `OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md`, `OI-0013-device-registration-pin-maxuses-concurrency.md`, `OI-0014-tenantless-security-event-auditing.md`, `OI-0015-permission-metadata-for-staff-pin-eligibility.md`.

Modified:
- `docs/issues/index.md` — Open Issues section rebuilt (grouped by area: Identity/Security, Devices, Audit), five entries.
- `docs/testing/security-tests.md`, `docs/testing/testing-strategy.md` — implementation-status sections mapping this document's requirements to the actual test files.
- Plan doc (Status, Milestone G steps revised to approved shape with step 41 inserted and Milestone H renumbered 42–44, Human Decisions entry #9) and this notes file.

### Tests added (156)

- **`HybridOfflineLoginTests` (2):** the full admin username/password chain (seed → login → `/auth/me` → permission-gated create → logout → dead token) and the full staff chain (registration PIN → anonymous device registration → device-token `/auth/me` with empty roles/permissions → staff PIN login → staff `/auth/me` → 403 from a `rejectStaffPin` endpoint → logout → dead token), both against local Postgres only.
- **`RbacTests` (153):** a 32-endpoint inventory (29 permission-gated — all `rejectStaffPin: true` — plus `/auth/me`, `/auth/logout`, `/auth/staff-pin/login`) swept by five theory scenarios — unauthenticated → 401 (32), garbage token → 401 (32), authenticated `Staff`-role session without the permission → 403 (29), `DeviceToken` → 403 (29), real `LocalStaffPin` session → 403 (29) — plus two facts: revoked session → 401 immediately everywhere, and a tenant-A `SystemAdmin` against a fully-seeded tenant B (13 single-row attempts all 404, five list endpoints all excluding B's rows; never 500, never 403, never data). Scenario state (staff/device/staff-session tokens) is seeded once in `RbacScenarioFixture`.
- **`IgnoreQueryFiltersUsageTests` (1):** scans `src/**/*.cs` (excluding `obj`/`bin`) for `.IgnoreQueryFilters(` and asserts exact two-way equality with the five documented files (`DeviceTokenAuthenticationHandler`, `SessionAuthenticationHandler`, `BootstrapAdminSeeder`, `AuthEndpoints`, `DeviceRegistrationEndpoints`) — an unapproved new bypass *or* a silently removed documented one fails the build. Implements ADR-0015 §Risks directly.

### Design notes worth flagging to a future reader

- **The RBAC matrix sends `{}` as the JSON body on non-GET requests** — minimal-API body binding runs *before* endpoint filters, so a missing body would surface as 400 and mask the 401/403 under test. A future endpoint whose request DTO cannot bind from `{}` (e.g. a `required` property) would break the matrix with 400s — bind-tolerant DTOs or an inventory-level body override is the fix, noted here so it isn't a surprise.
- **New protected endpoints must be added to `RbacTests.PermissionGatedEndpoints`** to inherit matrix coverage — one line per endpoint. Flagged in `docs/testing/security-tests.md` too.
- **The guard test's approved-files list and the bootstrap-callers comment in `DaxaDbContext.cs` must move together** — the test's doc comment says so explicitly.

### Commands run

```
docker compose ps                                                       (db only; keycloak not running throughout)
dotnet build DaxaPos.sln                                                (0 warnings, 0 errors)
dotnet test tests/DaxaPos.UnitTests/... --filter IgnoreQueryFiltersUsageTests        (1/1)
dotnet test tests/DaxaPos.Api.Tests/... --filter "HybridOfflineLoginTests|RbacTests" (155/155, first run)
dotnet test DaxaPos.sln                                                 (371/371)
dotnet ef database drop --force ... && dotnet ef database update ...    (fresh-DB verification: six existing migrations, none added)
dotnet test DaxaPos.sln                                                 (371/371 again, freshly-migrated DB)
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` (Postgres `db` running, all six migrations applied fresh, `keycloak` stopped) — **371/371 passed** (82 unit tests + 289 API tests: 215 pre-existing + 156 new Milestone G), 0 failed, 0 skipped.

### Working tree status

**Not committed.** All changes are in the working tree only, per the explicit instruction not to commit the Milestone G implementation until the result is reviewed and approved.

### Blockers before Milestone H

None. Milestone H is docs-only closeout (plan steps 42–44): consolidate `docs/architecture/tenancy.md`/`security.md`/`multi-location.md` and `docs/modules/devices.md`/`audit.md` against what was actually built, finalise the plan Status, write the PLAN-0004 handoff summary, and move the plan to `docs/plans/completed/`. After Milestone G, the identity/tenancy/device foundation is code-complete.

---

*Milestone G completed: 2026-07-03 (uncommitted, pending review).*

---

## Unrelated Note: Multi-Language Planning Follow-Up (2026-07-02)

**This is not part of PLAN-0003 Milestone D or E.** A separate, planning-only follow-up was done in the same session (after Milestone D was approved and committed) to record a deferred multi-language/localisation strategy, at the human's explicit request. It produced:

- `docs/adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md` — new proposed ADR.
- `docs/plans/active/PLAN-localisation-multi-language.md` — planning-only placeholder plan, blocked on ADR-0016 acceptance, containing no actionable implementation steps.
- Index/cross-reference updates: `docs/adr/index.md`, `docs/README.md`, `docs/architecture/overview.md`, `docs/architecture/tax-engine.md`, `docs/modules/tax.md`, `docs/modules/receipts.md`, `docs/modules/catalog.md`, `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`, `docs/03-phase-roadmap.md`.

No PLAN-0003 implementation code was touched. No migrations were added. No localisation was implemented. This note exists only so a future reader of this file doesn't mistake the ADR-0016/localisation-plan commit for Milestone D or E work.
