# PLAN-0011 — Local Demo Setup Helper

## Status

Implemented and verified (2026-07-07). `scripts/setup-local-demo.sh` exists, is executable, and
was run twice against the root Compose stack with override environment variables — see
Verification Results below.

## Goal

Add a rerunnable local-development script that prepares the minimum location, device-registration
PIN, and staff credentials needed to test the current PWA against the root Docker Compose stack.

## Scope

- API-driven Bash helper using `curl` and `jq`.
- Reuse active demo location and staff records on rerun.
- Reset the reused staff PIN and create a fresh device registration PIN.
- Document the quick path and retain the detailed manual smoke test.

## Non-goals

- Production or automatic application seeding.
- Browser automation or direct browser-local-storage writes.
- New API endpoints, database migrations, UI, or authentication behaviour.
- Changes to the Docker Compose service graph.

## Context Read

- `AGENTS.md`
- `docs/README.md`
- `docs/adr/index.md`
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`
- `docs/issues/index.md`
- `docs/issues/open/OI-0011-user-management-endpoints.md`
- `docs/modules/devices.md`
- `docs/deployment/docker.md`
- `docs/deployment/docker-deployment.md`
- `docs/testing/local-smoke-test.md`
- `docs/plans/active/PLAN-0010-docker-compose-local-dev-stack.md`
- `compose.yaml`
- `.env.example`
- Relevant identity endpoint implementations under `src/DaxaPos.Api/Endpoints/Identity/`

## Files Likely To Change

```text
scripts/setup-local-demo.sh
docs/deployment/docker.md
docs/testing/local-smoke-test.md
docs/README.md
docs/plans/active/PLAN-0011-local-demo-setup-helper.md
docs/plans/active/PLAN-0011-worker-notes.md
docs/issues/index.md
```

## Architecture Assumptions

- The root Compose stack exposes the API at `http://localhost:5118` and PWA at
  `http://localhost:8080`.
- ADR-0013 requires a trusted registered device before staff code/PIN login.
- The API is authoritative; the helper will use public endpoints rather than direct database
  writes.
- Raw registration and reset PINs are intentionally disclosed once and may be printed for local
  development.

## Domain Assumptions

- The bootstrap admin belongs to the Bootstrap Organisation.
- Demo records must be created inside that organisation due to current organisation-scoping
  rules recorded in OI-0011.
- An inactive matching location or staff record is not silently reactivated.

## Risks

- Resetting an existing demo staff PIN invalidates the previous PIN; the helper must state this
  clearly.
- API failure bodies vary, so error reporting must preserve the response body.
- Registration PINs expire after 15 minutes and are single-use by default.

## Implementation / Documentation Steps

1. Add the Bash helper with dependency, health, authentication, lookup/create, reset, and output
   handling.
2. Run syntax and live two-pass rerun verification.
3. Update Docker, smoke-test, documentation-index, plan, worker-note, and issue-index records.

## Tests To Run Later

- `bash -n scripts/setup-local-demo.sh`
- Two consecutive live runs against the root Compose stack.
- Manual device registration and staff login using second-run output.
- `git diff --check`

## Documentation To Update

- `docs/deployment/docker.md`
- `docs/testing/local-smoke-test.md`
- `docs/README.md`
- `docs/plans/active/PLAN-0011-worker-notes.md`
- `docs/issues/index.md`

## ADRs Required

None. The helper follows ADR-0013 and does not change architecture.

## Open Issues Required

None. No unresolved product or architecture question was introduced.

## Commit Sequence

```text
docs: design local demo setup helper
dev: add rerunnable local demo setup helper
docs: document local demo setup workflow
```

## Rollback Notes

Remove the helper and its documentation links. Records created in a local developer database are
test data and can be removed by recreating the local Compose volume if explicitly desired.

## Handoff Notes

The approved design is in
`docs/superpowers/specs/2026-07-07-local-demo-setup-helper-design.md`. Do not add a Compose seeder
service or application startup seeding; this helper is an explicit developer action.

## PLAN-0006 Sales Demo Addendum (2026-07-07)

### Goal

Add a local-development-only sales demo setup path that prepares enough data for PLAN-0006 manual UI
testing: device setup, terminal assignment, staff PIN login, resolved menu loading, required
modifier selection, real order line creation, cash/manual EFTPOS payment, receipt, and display.

### Scope

- Keep `scripts/setup-local-demo.sh` as the minimal identity/device helper.
- Add `scripts/setup-local-sales-demo.sh` as the sales-ready helper.
- Use existing API endpoints for location, device PIN, terminal, tax, catalogue, modifier, menu,
  and resolved-menu verification.
- Use a narrow direct database insert only for assigning the seeded `Staff` role to the demo staff
  member, because no staff-role assignment API exists yet.
- Document exact commands and manual browser steps.

### Non-goals

- Backend schema changes, migrations, endpoint changes, or product-behaviour shortcuts.
- Production seeding or automatic application startup seeding.
- Browser automation.
- New Back Office role-management UI.

### Files Likely To Change

```text
scripts/setup-local-sales-demo.sh
docs/testing/local-smoke-test.md
README.md
docs/README.md
docs/plans/active/PLAN-0011-local-demo-setup-helper.md
docs/plans/active/PLAN-0011-worker-notes.md
```

### Architecture Assumptions

- The root Compose stack exposes the API at `http://localhost:5118`, PWA at
  `http://localhost:8080`, and PostgreSQL at `localhost:5432`.
- PLAN-0006 `/sales` uses `GET /api/v1/menus/resolved?locationId=...`, which fails closed without
  `VenueTaxConfiguration` and returns no usable tiles without active product/menu/section/item data.
- A terminal must be assigned to the registered browser device before staff PIN login can create a
  session with `terminalId`.
- The seeded `Staff` role already carries `orders.manage`, `payments.record`, and
  `receipts.reprint`.

### Domain Assumptions

- The sales demo remains under the bootstrap organisation.
- AU GST 10% and tax-inclusive pricing are the first local sales demo defaults.
- A single required modifier group with one option is sufficient to prove the PLAN-0006 modifier
  flow without creating a full industry template.

### Risks

- Catalogue/menu endpoints do not reject duplicate names, so reruns must reuse active records where
  list endpoints allow it.
- The Staff-role grant uses direct SQL and must be clearly documented as local-dev-only because the
  API currently has no staff-role assignment endpoint.
- Device-to-terminal assignment cannot be automated until the browser registers the device and
  produces a device row; the helper must print a Back Office manual step.

### Tests To Run

- `test -f scripts/setup-local-sales-demo.sh` before implementation to prove the missing helper.
- `bash -n scripts/setup-local-demo.sh`
- `bash -n scripts/setup-local-sales-demo.sh`
- `./scripts/setup-local-sales-demo.sh` against the root Compose stack.
- `git diff --check`

### Documentation To Update

- `docs/testing/local-smoke-test.md`
- `README.md`
- `docs/README.md` if needed for index wording.
- `docs/plans/active/PLAN-0011-worker-notes.md`

### Commit Sequence

```text
dev: add local sales demo setup helper
docs: document local sales demo workflow
```

### Rollback Notes

Remove `scripts/setup-local-sales-demo.sh` and its documentation references. Local demo records can
be abandoned or removed by recreating the local Compose database volume if explicitly desired.

## Implementation Notes (2026-07-07)

- `scripts/setup-local-demo.sh` implements the full data flow: health check, admin login,
  `/auth/me` organisation resolution, location reuse/create, staff reuse (with reset-PIN)/create,
  and a fresh device registration PIN every run. It never queries or writes PostgreSQL directly
  and never prints the admin password or admin session token.
- **Staff inactive-record detection**: `GET /api/v1/staff-members` hides inactive staff, but
  `POST /api/v1/staff-members` conflict-checks the staff code regardless of `IsActive`. The script
  uses this: if the active-staff list has no match but creation still returns `409 Conflict`, that
  can only mean a matching staff member exists and is inactive. It fails with an actionable
  message rather than attempting to reactivate anything (Requirement 10).
- **Location inactive-record detection — known gap**: `GET /api/v1/locations` also hides inactive
  locations, but unlike staff creation, `POST /api/v1/locations` has no name-uniqueness check at
  all (see `src/DaxaPos.Api/Endpoints/Identity/LocationEndpoints.cs`), so there is no equivalent
  conflict signal. If a location named `DEMO_LOCATION_NAME` exists but is inactive, this helper
  cannot detect it through the API and will create a second, active location with the same name
  rather than erroring. This is documented in a comment in the script. It is not a silent
  reactivation (a new row is created, the inactive one is left untouched), so it does not violate
  Requirement 10 as written, but it also does not fully satisfy the spirit of "actionable error for
  a matching inactive record." Closing this gap would require either a location list endpoint that
  can include inactive rows, or a name-uniqueness check on location creation — both are endpoint
  changes outside this task's scope (no new API endpoints/behaviour per the Non-goals). Flagged
  here for whoever next touches `LocationEndpoints`; not filed as a new open issue because it is an
  implementation-surface observation, not an unresolved product/architecture decision.
- macOS ships bash 3.2 (GPL licensing), which does not support `${VAR^^}` (bash 4+ only). The
  script normalises the staff code with `tr '[:lower:]' '[:upper:]'` instead, to stay portable to
  the default `/bin/bash` on the machine the design/implementation/verification actually ran on.

## Verification Results (2026-07-07)

Environment: this session's sandbox had no pre-existing containers/volumes for this repo, so the
root stack was started fresh with `docker compose up -d --build` (no `-v`, nothing was wiped —
there was nothing running to wipe). `GET /health` returned `200 Healthy` before any script run.

1. `bash -n scripts/setup-local-demo.sh` — passed (run before and after the bash-3.2 fix below).
2. `git diff --check` (staged) — no whitespace errors.
3. First live run used the required override variables:

   ```bash
   DEMO_LOCATION_NAME="Local Demo Verification Venue" \
   DEMO_STAFF_CODE="VERIFY01" \
   DEMO_STAFF_NAME="Verification Cashier" \
   DEMO_STAFF_PIN="135790" \
   ./scripts/setup-local-demo.sh
   ```

   First attempt failed fast with `bad substitution` on `${DEMO_STAFF_CODE^^}` (bash 3.2 on
   macOS does not support that expansion) — fixed to use `tr`, no other changes needed. Re-run:
   created location `Local Demo Verification Venue` and staff `VERIFY01`, PIN `135790` as
   requested, registration PIN `009456`.
4. Second run with the same overrides: reused the same location ID
   (`f3745fc2-6dab-4401-9f94-7a18d042419e`) and the same staff member ID
   (`a51e7bf2-c096-48dc-87b8-21de97e6a32c`); the staff PIN was reset to a new server-generated
   value (`473953`, different from the first run's `135790`); a new registration PIN was issued
   (`917791`, different from the first run's `009456`).
5. Confirmed no duplicates directly via the admin API: querying `GET /api/v1/locations` and
   `GET /api/v1/staff-members` and filtering for the verification name/code each returned exactly
   one match.
6. Verified the second run's printed credentials are genuinely functional by calling the same
   endpoints the PWA's `/device-setup` and `/login` pages call: `POST /api/v1/device-registration`
   with PIN `917791` succeeded and returned a device token; `POST /api/v1/auth/staff-pin/login`
   with that device token, staff code `VERIFY01`, and PIN `473953` succeeded and returned a staff
   session. This is API-level verification of the printed output, not an actual browser session —
   no browser was opened, so browser verification is not claimed.
7. The user's own manually-created `TEST01`/location `34692c6c-...` records were never touched —
   all verification used the `VERIFY01`/`Local Demo Verification Venue` overrides as instructed.
