# Claude Code Handoff — Implement PLAN-0011

## Role

You are the primary implementation agent for this task. Implement the approved PLAN-0011 local
demo setup helper. Codex will remain review-focused and should not independently redesign or
duplicate your implementation.

## Required Context

Read these files completely before editing:

1. `AGENTS.md`
2. `docs/README.md`
3. `docs/adr/index.md`
4. `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`
5. `docs/issues/index.md`
6. `docs/issues/open/OI-0011-user-management-endpoints.md`
7. `docs/modules/devices.md`
8. `docs/deployment/docker.md`
9. `docs/testing/local-smoke-test.md`
10. `docs/superpowers/specs/2026-07-07-local-demo-setup-helper-design.md`
11. `docs/plans/active/PLAN-0011-local-demo-setup-helper.md`
12. `docs/plans/active/PLAN-0011-worker-notes.md`

Also inspect the relevant endpoint implementations under:

```text
src/DaxaPos.Api/Endpoints/Identity/
```

## Current State

- The approved design and PLAN-0011 planning documents are committed in `f36bd01`.
- The root Docker Compose stack is already implemented.
- `scripts/setup-local-demo.sh` does not exist yet.
- The current PWA flow works when its prerequisite data is created manually.
- There is no built-in default staff PIN or demo cashier.
- The user has manually created:
  - location `34692c6c-0f45-440a-a06e-cfcabc1c1f41`;
  - staff code `TEST01`;
  - staff display name `Test Cashier`;
  - current staff PIN `246810`;
  - a registered `KioskBrowser` device.
- Do not wipe Docker volumes or the local database.

## Required Implementation

Create:

```text
scripts/setup-local-demo.sh
```

The script must:

1. Use Bash with `set -euo pipefail`.
2. Require and validate `curl` and `jq`.
3. Use these configurable environment variables and defaults:

   ```text
   API_URL=http://localhost:5118
   PWA_URL=http://localhost:8080
   ADMIN_EMAIL=admin@daxapos.local
   ADMIN_PASSWORD=Local-Dev-Only-Passw0rd!
   DEMO_LOCATION_NAME=Local Demo Venue
   DEMO_STAFF_CODE=TEST01
   DEMO_STAFF_NAME=Test Cashier
   DEMO_STAFF_PIN=246810
   ```

4. Confirm `GET /health` succeeds before changing data.
5. Authenticate through `POST /api/v1/auth/local/login`.
6. Resolve the bootstrap organisation through `GET /api/v1/auth/me`.
7. Reuse the active location matching `DEMO_LOCATION_NAME`, or create it under the bootstrap
   organisation.
8. Reuse the active staff member matching `DEMO_STAFF_CODE` and the selected location, or create
   it.
9. When reusing staff, call the supported reset-PIN endpoint and print the new server-generated
   PIN. Do not query or modify PostgreSQL directly.
10. Treat matching inactive location or staff records as an actionable error. Do not silently
    reactivate them.
11. Always create a fresh single-use device registration PIN and print its expiry.
12. Print a concise final block containing:
    - PWA URL;
    - registration PIN;
    - registration-PIN expiry;
    - staff code;
    - current staff PIN;
    - location ID;
    - instructions to register the browser and then log in.
13. Never print the admin password or admin session token.
14. Capture unexpected HTTP statuses and print the failed step plus the API response body.
15. Leave browser registration manual. Do not write browser local storage or manufacture a device
    token.

The helper is explicitly local-development tooling. Do not add application startup seeding, a
Compose seeder service, hidden product defaults, or production behaviour.

## Documentation Changes

Update:

- `docs/deployment/docker.md`
  - Add the helper command after `docker compose up -d --build`.
  - Explain the printed registration and staff credentials.
- `docs/testing/local-smoke-test.md`
  - Add a short automated fast path using the helper.
  - Retain the detailed manual API walkthrough.
- `docs/README.md`
  - Add a link to `docs/testing/local-smoke-test.md` under Testing.
- `docs/plans/active/PLAN-0011-local-demo-setup-helper.md`
  - Refresh status and implementation/verification notes.
- `docs/plans/active/PLAN-0011-worker-notes.md`
  - Record exact files changed, assumptions, commands, outcomes, and handoff state.
- `docs/issues/index.md`
  - Keep it accurate. Do not create a new issue unless implementation reveals a genuine unresolved
    product or architecture decision.

No ADR is expected because this implements the accepted ADR-0013 flow without changing it.

## Verification

At minimum run:

```bash
bash -n scripts/setup-local-demo.sh
git diff --check
```

Run the helper twice against the active root Compose stack to prove rerun behaviour. Avoid changing
the user's current `TEST01` credentials during verification by using overrides such as:

```bash
DEMO_LOCATION_NAME="Local Demo Verification Venue" \
DEMO_STAFF_CODE="VERIFY01" \
DEMO_STAFF_NAME="Verification Cashier" \
DEMO_STAFF_PIN="135790" \
./scripts/setup-local-demo.sh
```

Run that same override a second time. Confirm:

- the location is reused;
- the staff member is reused;
- the second run returns a newly reset staff PIN;
- each run returns a fresh registration PIN;
- no duplicate location or staff record is created.

If practical, verify the printed second-run credentials through the PWA. Do not claim manual
browser verification unless it was actually performed.

## Git and Completion

- Preserve unrelated user changes.
- Do not rewrite or squash existing commits.
- Keep implementation and documentation commits focused.
- Do not push unless the user explicitly asks.
- Finish with a concise report containing:
  - files changed;
  - commits created;
  - verification commands and actual results;
  - the exact command the user should run;
  - any unresolved limitations.
