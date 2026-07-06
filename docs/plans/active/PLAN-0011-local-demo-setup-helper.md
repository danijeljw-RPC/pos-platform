# PLAN-0011 — Local Demo Setup Helper

## Status

Draft — design approved; implementation awaits design-document review.

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
