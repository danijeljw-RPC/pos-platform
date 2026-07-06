# PLAN-0012 Worker Notes — Local Demo CI Smoke Test

## Current Status

Implemented and locally verified (2026-07-07). GitHub-hosted execution remains pending until the
commits are pushed.

## Human Decisions

- Use the minimal stack: PostgreSQL and the API only.
- Install .NET SDK 10.
- Install the latest available `dotnet-ef`.
- Explicitly install `curl` and `jq`.
- Run the helper twice to verify rerun behavior.

## Repository Findings

- `scripts/setup-local-demo.sh` calls only the API; its `PWA_URL` is output for the operator and
  is not requested by the script.
- The API needs a migrated PostgreSQL database and both bootstrap-administrator environment
  variables.
- Keycloak is intentionally absent from local POS authentication under ADR-0013.
- GitHub Actions service containers start before repository build steps, so the source-built API
  is more direct and reliable as a background runner process.

## Assumptions

- The latest stable `dotnet-ef` remains able to operate on the repository's EF Core 9 migrations
  when run under .NET SDK 10.
- GitHub-hosted Ubuntu allows `apt-get` installation and provides Docker for the PostgreSQL
  service container.

## Files Changed

```text
.github/workflows/local-demo-smoke-ci.yml
docs/testing/local-smoke-test.md
docs/plans/active/PLAN-0012-local-demo-ci-smoke-test.md
docs/plans/active/PLAN-0012-worker-notes.md
docs/issues/index.md
```

## Verification To Record

- `test -f .github/workflows/local-demo-smoke-ci.yml` failed before creation as expected.
- `rhysd/actionlint:latest` reported no workflow errors; only the workflow file was mounted
  read-only into the validator container.
- `bash -n scripts/setup-local-demo.sh` passed.
- Local verification used .NET SDK `10.0.300` and `dotnet-ef` `10.0.9`.
- All 17 migrations applied to a fresh, isolated PostgreSQL 16 database on host port `55432`.
- The isolated API became healthy on host port `5119`.
- The first helper run created the workflow-specific location and staff member.
- The second helper run reused both IDs, reset the staff PIN, created a fresh registration PIN,
  and emitted every output fragment asserted by the workflow.
- The isolated API and PostgreSQL container were stopped. The pre-existing root Compose stack was
  not modified or stopped.
- GitHub-hosted execution has not occurred in this local session and is not claimed.

## Handoff

Push the three PLAN-0012 commits and confirm `Local Demo Smoke Test / local-demo-smoke` passes on
GitHub. If hosted execution differs from local verification, inspect the workflow's failure-only
API log before changing the helper. The existing `.github/workflows/ci.yml` and all unrelated
working-tree changes were preserved.
