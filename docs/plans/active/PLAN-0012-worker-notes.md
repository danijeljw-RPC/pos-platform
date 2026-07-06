# PLAN-0012 Worker Notes — Local Demo CI Smoke Test

## Current Status

Planning complete; implementation pending.

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

## Intended Files

```text
.github/workflows/local-demo-smoke-ci.yml
docs/testing/local-smoke-test.md
docs/plans/active/PLAN-0012-local-demo-ci-smoke-test.md
docs/plans/active/PLAN-0012-worker-notes.md
docs/issues/index.md
```

## Verification To Record

- Workflow structural validation.
- Bash syntax validation.
- Migration result.
- API health result.
- First helper run result.
- Second helper run reuse result.
- Whitespace check.
- GitHub Actions run URL or explicit note that hosted verification remains pending.

## Handoff

Follow `PLAN-0012-local-demo-ci-smoke-test.md`. Do not modify the existing
`.github/workflows/ci.yml`; it already has unrelated working-tree changes.
