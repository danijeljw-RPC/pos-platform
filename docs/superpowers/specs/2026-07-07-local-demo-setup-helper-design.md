# Local Demo Setup Helper Design

## Purpose

Provide a repeatable local-development command that prepares the minimum API data required to
test the current Blazor PWA flow after the root Docker Compose stack is running.

The helper removes the need to copy a long sequence of `curl` commands from the manual smoke
test. It does not add product seeding, production behaviour, or a hidden default staff PIN.

## Interface

The repository will provide:

```bash
./scripts/setup-local-demo.sh
```

The script will use these environment variables, with local-only defaults:

| Variable | Default |
| --- | --- |
| `API_URL` | `http://localhost:5118` |
| `PWA_URL` | `http://localhost:8080` |
| `ADMIN_EMAIL` | `admin@daxapos.local` |
| `ADMIN_PASSWORD` | `Local-Dev-Only-Passw0rd!` |
| `DEMO_LOCATION_NAME` | `Local Demo Venue` |
| `DEMO_STAFF_CODE` | `TEST01` |
| `DEMO_STAFF_NAME` | `Test Cashier` |
| `DEMO_STAFF_PIN` | `246810` for first creation only |

The script requires `bash`, `curl`, and `jq`.

## Data Flow

1. Check the required commands and `GET /health`.
2. Authenticate through `POST /api/v1/auth/local/login`.
3. Read the bootstrap organisation from `GET /api/v1/auth/me`.
4. List locations and reuse the active location whose name matches `DEMO_LOCATION_NAME`;
   otherwise create it under the bootstrap organisation.
5. List staff and reuse the active member whose code and location match the requested values;
   otherwise create that member.
6. When reusing a staff member, call its reset-PIN endpoint and report the server-generated PIN.
   This makes reruns deterministic from the operator's perspective without reading password
   hashes or reaching into PostgreSQL.
7. Create a fresh, single-use device registration PIN and print its expiry.
8. Print the PWA URL and the exact registration and staff credentials to enter.

The browser remains responsible for device registration because the returned device credential
belongs in browser local storage. The helper will not attempt to manufacture or write browser
state.

## Rerun Behaviour

The helper is safe to rerun against the same local database:

- It reuses the named location rather than creating duplicates.
- It reuses the matching staff member rather than creating duplicate staff codes.
- It resets the reused staff member's PIN and clearly prints the replacement.
- It always creates a new short-lived device registration PIN because raw registration PINs are
  intentionally disclosed once and expire after 15 minutes.

An inactive matching location or staff member is an error with an actionable message. The helper
will not silently reactivate domain records.

## Error Handling

The script will run with `set -euo pipefail`.

Every API call used for setup will capture both its HTTP status and body. Unexpected statuses will
produce a concise error naming the failed step and include the response body. Authentication
responses and IDs will be validated before later requests run.

The script will not print the admin password or session token. Output credentials are explicitly
local-development credentials.

## Documentation

Update:

- `docs/deployment/docker.md` with the post-start setup command and PWA login sequence.
- `docs/testing/local-smoke-test.md` with a fast-path section that points to the helper while
  retaining the detailed manual flow.
- `docs/README.md` to link the local smoke-test guide.
- The active PLAN-0011 plan and worker notes with implementation and verification evidence.

No ADR is required because the helper implements the existing ADR-0013 device-plus-staff
authentication flow without changing it. No open issue is required because the task introduces
no unresolved product or architecture decision.

## Verification

- `bash -n scripts/setup-local-demo.sh`
- Run the helper against the active root Compose stack.
- Run it a second time and verify it reuses the location and staff member while producing new
  staff and registration PINs.
- Use the printed credentials in a clean/private browser session to verify device registration
  and staff login.
- `git diff --check`
