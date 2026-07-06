# Daxa POS local smoke test

Manual end-to-end walkthrough of the identity/tenancy/device surface built by PLAN-0003. Current to **Milestone F**, commit `585cd39`. The entire flow runs with only Postgres available — Keycloak stopped — which is itself the ADR-0013 offline confirmation this document exists to exercise. PLAN-0003 Milestone G formalises the same chains as automated tests (`HybridOfflineLoginTests.cs`, `RbacTests.cs`).

---

## Fast path (PLAN-0011)

If you just want a location, a demo staff member, and a device registration PIN so you can log
into the PWA — without stepping through the manual walkthrough below — start the root Compose
stack (`docker compose up -d --build`, see [docs/deployment/docker.md](../deployment/docker.md))
and run:

```bash
./scripts/setup-local-demo.sh
```

This requires only `curl` and `jq`. It authenticates as the bootstrap admin, reuses or creates a
`Local Demo Venue` location and a `TEST01` staff member under the bootstrap organisation, and
always issues a fresh device registration PIN. It prints the PWA URL, the registration PIN and
its expiry, the staff code, and the current staff PIN — never the admin password or admin
session token. It is safe to rerun: it reuses the same location and staff member and resets the
staff PIN, printing the new one each time. All defaults are overridable via environment
variables (`API_URL`, `PWA_URL`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `DEMO_LOCATION_NAME`,
`DEMO_STAFF_CODE`, `DEMO_STAFF_NAME`, `DEMO_STAFF_PIN`) — useful for keeping a second,
independent demo identity alongside one you're using interactively.

This fast path automates steps 6–13 below against the same API endpoints. The detailed manual
walkthrough remains below for exercising individual endpoints, negative cases, and the
Keycloak-stopped offline confirmation.

---

## Automated CI smoke test (PLAN-0012)

`.github/workflows/local-demo-smoke-ci.yml` runs the fast path twice against a fresh PostgreSQL
service container and live API. The first run confirms setup succeeds; the second confirms the
location and staff records are reused. Keycloak, Workers, and the PWA are intentionally absent.

---

## Important quirk

Every location, PIN, and staff operation is cross-checked against the caller's session `OrganisationId`.

The bootstrap admin's session is pinned to the auto-created **Bootstrap Organisation**.

You can create a new organisation and receive `201`, but you cannot then create locations or staff under it with the same session. There are no user-management endpoints yet, so there is currently no way to mint a login inside the new organisation.

All downstream steps must use the bootstrap organisation's ID, retrieved from:

```http
GET /api/v1/auth/me
```

---

## 0. Prerequisites and environment

You have:

- .NET `10.0.300`
- `dotnet-ef` `10.0.9`
- `jq`
- Docker Compose

The projects target `net9.0` with `RollForward=LatestMajor` in `Directory.Build.props`, so they run on the .NET 10 runtime.

```bash
export REPO=~/Developer/pos-platform
export API_URL=http://localhost:5118
export ADMIN_EMAIL=admin@daxapos.local
export ADMIN_PASSWORD='Local-Dev-Only-Passw0rd!'
```

---

## 1. Start Postgres with Docker Compose

```bash
cd "$REPO/deploy"
cp .env.example .env        # first time only; defaults are fine for dev
docker compose up -d db     # deliberately NOT "up -d" — see Keycloak note below
docker compose ps           # wait until db is healthy
```

Postgres binds to:

```text
127.0.0.1:5432
```

Using:

```text
username: daxapos
password: daxapos_dev_password
database: daxapos
```

This matches:

```text
src/DaxaPos.Api/appsettings.Development.json
```

---

## 2. Confirm Keycloak is not required

The compose file has a `keycloak` service, but the comment in `deploy/docker-compose.yml:18` says it is scoped to future cloud/admin identity work under `ADR-0013`.

Nothing in `DaxaPos.Api` depends on Keycloak.

The health check in `Program.cs:91` only covers the database.

The Milestone E/F worker notes record that tests were run with Keycloak stopped throughout.

Start only the database:

```bash
docker compose up -d db
```

Do not start Keycloak. Everything below should still work. That is the confirmation test.

---

## 3. Apply EF Core migrations

Migrations live in `DaxaPos.Persistence`. The API is the startup project.

The design-time host defaults to the Production environment, where no connection string is configured, so set the environment explicitly.

```bash
cd "$REPO"
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update \
  --project src/DaxaPos.Persistence \
  --startup-project src/DaxaPos.Api
```

This applies all six migrations:

```text
InitialCreate → AddStaffMembers
```

It also seeds the fixed RBAC catalogue:

- 5 roles
- 8 permissions

The RBAC catalogue is seeded via `HasData`.

---

## 4. Bootstrap admin seeding

`BootstrapAdminSeeder` runs at API startup, not during migration.

File:

```text
src/DaxaPos.Api/BootstrapAdminSeeder.cs
```

It reads two environment variables:

```text
DAXA_BOOTSTRAP_ADMIN_EMAIL
DAXA_BOOTSTRAP_ADMIN_PASSWORD
```

If either value is unset:

- seeding is skipped
- a warning is logged
- the app still starts

There is no fallback credential in source.

If a user with that email already exists, the seeder does not touch the password. This makes it idempotent and safe to restart.

On first run it creates:

- `Bootstrap Tenant`
- `Bootstrap Organisation`
- the admin user
- a `SystemAdmin` role assignment

Note: `deploy/.env` only feeds Docker Compose. The API run in the next step must receive the bootstrap environment variables directly.

---

## 5. Run `DaxaPos.Api`

```bash
cd "$REPO"
DAXA_BOOTSTRAP_ADMIN_EMAIL="$ADMIN_EMAIL" \
DAXA_BOOTSTRAP_ADMIN_PASSWORD="$ADMIN_PASSWORD" \
dotnet run --project src/DaxaPos.Api --launch-profile http
```

The `http` launch profile listens on:

```text
http://localhost:5118
```

Using:

```text
ASPNETCORE_ENVIRONMENT=Development
```

On first run, watch the logs for the bootstrap admin creation warning.

---

## 6. Health check

```bash
curl -i "$API_URL/health"
```

Expected:

```text
HTTP 200
Healthy
```

This checks the API and database only.

---

## 7. Admin login and `/auth/me`

```bash
LOGIN=$(curl -s -X POST "$API_URL/api/v1/auth/local/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")

export ADMIN_TOKEN=$(echo "$LOGIN" | jq -r .sessionToken)
echo "$LOGIN" | jq .
```

Expected:

- role: `SystemAdmin`
- all 8 permission codes

Then call `/auth/me`:

```bash
ME=$(curl -s "$API_URL/api/v1/auth/me" -H "Authorization: Bearer $ADMIN_TOKEN")
echo "$ME" | jq .

export ORG_ID=$(echo "$ME" | jq -r .organisationId)
```

`ORG_ID` is the **Bootstrap Organisation** ID. Use it everywhere below.

Failure behaviours worth spot-checking:

```text
wrong password → generic 401
5 failed logins → account temporarily locked
no token on /auth/me → 401
```

---

## 8. Create an organisation

This works and demonstrates the Milestone D endpoints.

However, per the quirk above, it is a dead end for the rest of the flow.

Do not send `tenantId`. Any request that includes it is rejected with `400` by design.

```bash
curl -s -X POST "$API_URL/api/v1/organisations" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"name":"Main Street Bakery"}' | jq .
```

Expected:

```text
201 Created
```

Locations under this new organisation will return `404` for the current session. That is expected and verifies the `ADR-0015` organisation cross-check.

Continue with `$ORG_ID` from `/auth/me`.

---

## 9. Create a location under the bootstrap organisation

```bash
LOC=$(curl -s -X POST "$API_URL/api/v1/locations" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"name\":\"Front Counter Test Venue\",\"organisationId\":\"$ORG_ID\"}")

export LOC_ID=$(echo "$LOC" | jq -r .id)
echo "$LOC" | jq .
```

---

## 10. Create a terminal

This is optional. It is not required by the device flow.

Device registration is keyed to a location, not a terminal. `Terminal.DeviceId` linking is not wired into any flow yet.

Create one to exercise the endpoint:

```bash
curl -s -X POST "$API_URL/api/v1/terminals" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"name\":\"Front Counter 1\",\"locationId\":\"$LOC_ID\"}" | jq .
```

---

## 11. Create a device registration PIN

The raw 6-digit PIN is returned exactly once.

It:

- expires in 15 minutes
- defaults to single-use

Do steps 11 and 12 together.

```bash
PIN_RESP=$(curl -s -X POST "$API_URL/api/v1/device-registration-pins" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$LOC_ID\"}")

export REG_PIN=$(echo "$PIN_RESP" | jq -r .pin)
echo "$PIN_RESP" | jq .
```

---

## 12. Register a device

Device registration is unauthenticated but PIN-gated.

It is rate-limited to:

```text
10 attempts / minute / IP
```

Valid `deviceType` values:

```text
WindowsPos
IPadKds
CustomerDisplay
PaymentTerminal
Printer
KioskBrowser
Other
```

The response is the only time the device token is disclosed.

Device token format:

```text
{credentialId}.{secret}
```

```bash
DEV=$(curl -s -X POST "$API_URL/api/v1/device-registration" \
  -H 'Content-Type: application/json' \
  -d "{\"pin\":\"$REG_PIN\",\"deviceType\":\"WindowsPos\",\"name\":\"Smoke Test POS\"}")

export DEVICE_TOKEN=$(echo "$DEV" | jq -r .deviceToken)
echo "$DEV" | jq .
```

Verify the device context works:

```bash
curl -s "$API_URL/api/v1/auth/me" \
  -H "Authorization: Device $DEVICE_TOKEN" | jq .
```

Expected:

```text
authMethod: DeviceToken
userId: null
staffMemberId: null
roles: []
permissions: []
```

---

## 13. Create a staff member

Staff code rules:

- 2–20 characters
- letters and digits only
- uppercased server-side

PIN rules:

- 4–10 digits
- chosen at creation
- resets are server-generated

```bash
export STAFF_CODE=DJ01
export STAFF_PIN=246810

STAFF=$(curl -s -X POST "$API_URL/api/v1/staff-members" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"displayName\":\"Test Cashier\",\"staffCode\":\"$STAFF_CODE\",\"pin\":\"$STAFF_PIN\",\"locationId\":\"$LOC_ID\"}")

export STAFF_ID=$(echo "$STAFF" | jq -r .id)
echo "$STAFF" | jq .
```

---

## 14. Assign a staff role

This is optional and mostly a no-op today.

Role assignment is not required for staff PIN login. A role-less staff member can log in with empty roles and permissions.

The seeded `Staff` role deliberately carries zero permissions. The current permission catalogue is admin-sensitive and staff-PIN-barred.

Assign the `Staff` role anyway to exercise the endpoint.

The seeded `Staff` role ID is:

```text
00000000-0000-0000-0001-000000000004
```

```bash
curl -s -X POST "$API_URL/api/v1/staff-members/$STAFF_ID/roles" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"roleId":"00000000-0000-0000-0001-000000000004"}' | jq .
```

Negative test worth running:

Assign `OrganisationOwner` instead:

```text
00000000-0000-0000-0001-000000000002
```

Then the staff PIN login below should fail with generic `401`.

This is audited as:

```text
RoleGrantsSensitivePermissions
```

That verifies the defense-in-depth guard.

---

## 15. Staff PIN login

This is the trust chain of the whole build.

Staff PIN login requires the device token as the `Authorization` header.

Expected failures:

```text
anonymous → 401
admin Bearer token → 403
```

Tenant, organisation, and location scope come from the device.

The request body's `locationId` is only a cross-check and must equal the device's location.

```bash
STAFF_LOGIN=$(curl -s -X POST "$API_URL/api/v1/auth/staff-pin/login" \
  -H "Authorization: Device $DEVICE_TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$LOC_ID\",\"staffCode\":\"$STAFF_CODE\",\"pin\":\"$STAFF_PIN\"}")

export STAFF_TOKEN=$(echo "$STAFF_LOGIN" | jq -r .sessionToken)
echo "$STAFF_LOGIN" | jq .
```

---

## 16. `/auth/me` as the staff session

```bash
curl -s "$API_URL/api/v1/auth/me" \
  -H "Authorization: Bearer $STAFF_TOKEN" | jq .
```

Expected:

```text
authMethod: LocalStaffPin
staffMemberId: populated
deviceId: populated
locationId: populated
userId: null
```

Good closing negative test:

```bash
curl -i "$API_URL/api/v1/staff-members" \
  -H "Authorization: Bearer $STAFF_TOKEN"
```

Expected:

```text
403 Forbidden
```

This proves `rejectStaffPin` works.

Session expiry rules:

```text
staff sessions: 8h absolute / 30min idle
admin sessions: 12h absolute / 8h idle
```

Logout:

```bash
curl -s -X POST "$API_URL/api/v1/auth/logout" \
  -H "Authorization: Bearer $STAFF_TOKEN" | jq .
```

`POST /api/v1/auth/logout` revokes whichever Bearer token is sent.

---

## Additional testable endpoints

Also testable, but not part of the main flow:

- `GET /api/v1/devices`
- `POST /api/v1/devices/{id}/rotate-credential`
- `POST /api/v1/devices/{id}/revoke`
- PIN revoke
- staff reset PIN, which returns a new server-generated PIN once
- staff disable, which revokes active staff sessions
- organisation list/get/rename/deactivate/reactivate
- location list/get/rename/deactivate/reactivate
- terminal list/get/rename/deactivate/reactivate

Audit coverage is DB-only for now:

```bash
docker compose exec db psql -U daxapos -d daxapos \
  -c 'select event_type, occurred_at_utc from audit_events order by occurred_at_utc;'
```

Adjust column names if needed. The table is:

```text
audit_events
```

---

# What cannot be tested yet

Nothing outside identity, tenancy, and devices exists yet.

Specifically not implemented:

- orders
- order lines
- void/refund endpoints
- payments
- cash handling
- EFTPOS handling
- payment adapter code
- tax engine
- tax categories
- tax rates
- tax snapshot logic
- catalog
- menus
- pricing
- surcharges
- discounts
- receipts
- printing
- inventory
- reporting endpoints
- audit read API
- MAUI terminal UI
- customer display UI
- admin PWA
- KDS UI
- sync/offline behaviour
- Keycloak/cloud identity
- user management endpoints
- Region/Country hierarchy

Audit rows exist, but there is no read API yet. Audit verification is DB-only.

There is no UI yet. Everything is curl-only.

There is no real sync/offline behaviour yet. The current local mode is a single API and database stack running on-site.

`AuthMethod.CloudIdentityProvider` is unwired per `ADR-0015`.

The `users.manage` permission is seeded but no endpoints consume it. This is why the bootstrap-organisation dead end exists.

The Region/Country hierarchy was descoped from `PLAN-0003` by human decision.
