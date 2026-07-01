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

*Milestone A completed: 2026-07-01.*
