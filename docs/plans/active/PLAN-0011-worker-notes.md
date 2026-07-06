# PLAN-0011 Worker Notes — Local Demo Setup Helper

## Current Status

Implemented and verified (2026-07-07). See "Implementation Notes" and "Verification Results" in
[PLAN-0011-local-demo-setup-helper.md](PLAN-0011-local-demo-setup-helper.md) for full detail;
this file summarises files changed, commands run, and handoff state.

## Human Decisions

- Create a runnable script, not only a command reference.
- Make the script safe to rerun against an existing local database.
- Keep browser device registration manual.

## Repository Findings

- The root `compose.yaml` runs the complete local stack.
- The PWA has no default staff PIN or demo cashier.
- Bootstrap admin credentials configure admin API access only.
- Location and staff list endpoints allow API-level reuse.
- Existing staff PINs cannot be recovered; the supported reset endpoint returns a new raw PIN.
- Device registration PINs are single-use by default and expire after 15 minutes.

## Assumptions

- Local developers have `bash`, `curl`, and `jq`.
- The script's default credentials match `.env.example`, but environment overrides remain
  available.

## Files Changed (2026-07-07 implementation)

```text
scripts/setup-local-demo.sh                                (new)
docs/deployment/docker.md                                  (updated)
docs/testing/local-smoke-test.md                           (updated)
docs/README.md                                              (updated)
docs/plans/active/PLAN-0011-local-demo-setup-helper.md      (updated)
docs/plans/active/PLAN-0011-worker-notes.md                 (updated, this file)
```

`docs/issues/index.md` was reviewed and left unchanged: implementation revealed one
implementation-surface gap (see below), not a new unresolved product/architecture decision, so no
new open issue was filed.

## Commands Run

```bash
docker compose up -d --build          # fresh sandbox, no prior containers/volumes existed
curl -s http://localhost:5118/health   # 200 Healthy, confirmed before any script run
bash -n scripts/setup-local-demo.sh    # syntax check, run twice (before/after the bash-3.2 fix)
git diff --check                       # (staged) no whitespace errors

DEMO_LOCATION_NAME="Local Demo Verification Venue" \
DEMO_STAFF_CODE="VERIFY01" \
DEMO_STAFF_NAME="Verification Cashier" \
DEMO_STAFF_PIN="135790" \
./scripts/setup-local-demo.sh          # run twice in a row
```

## Outcomes

- Run 1 failed on first attempt (`${VAR^^}` unsupported on macOS's default bash 3.2), fixed with
  `tr '[:lower:]' '[:upper:]'`, then succeeded: created the verification location and staff
  member.
- Run 2 (same overrides) reused the same location and staff IDs, reset the staff PIN to a new
  server-generated value, and issued a new registration PIN — confirmed no duplicate rows via
  direct API queries.
- Confirmed the second run's printed registration PIN and reset staff PIN both work by driving
  `POST /api/v1/device-registration` and `POST /api/v1/auth/staff-pin/login` directly — the same
  endpoints the PWA's `/device-setup` and `/login` pages call. No actual browser session was
  opened, so this is API-level verification, not manual browser verification.
- The user's pre-existing manually-created `TEST01` staff member, its location
  (`34692c6c-0f45-440a-a06e-cfcabc1c1f41`), and its registered `KioskBrowser` device were not
  touched by any verification step.

## Known Gap Surfaced During Implementation

`GET /api/v1/locations` hides inactive locations, and unlike staff creation, `POST
/api/v1/locations` has no name-uniqueness check — so there is no way for this script to detect an
existing-but-inactive location with the same name (the 409-conflict signal that works for staff
has no location equivalent). Documented in the script and in
[PLAN-0011-local-demo-setup-helper.md](PLAN-0011-local-demo-setup-helper.md#implementation-notes-2026-07-07).
Not filed as a new open issue — it's an implementation-surface observation about
`LocationEndpoints`, not an unresolved product/architecture decision.

## Handoff

Nothing further required for PLAN-0011. If a later worker adds location list filtering or
name-uniqueness (e.g. while touching `LocationEndpoints` for other reasons), revisit the gap
above and tighten the script's location-reuse check to match.
