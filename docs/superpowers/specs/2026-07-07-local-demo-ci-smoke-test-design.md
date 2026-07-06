# Local Demo CI Smoke Test Design

## Purpose

Add a focused GitHub Actions workflow that proves `scripts/setup-local-demo.sh` works against a
fresh PostgreSQL database and a live Daxa POS API. The workflow validates the helper's real HTTP
and persistence path without starting unrelated local-stack services.

## Workflow

Create:

```text
.github/workflows/local-demo-smoke-ci.yml
```

The workflow will run for pull requests, pushes to `main`, and manual dispatch. It will use
`permissions: contents: read`.

## Runtime Architecture

PostgreSQL 16 will run as a GitHub Actions service container with the same development database
name, username, password, port mapping, and `pg_isready` health check used by the existing CI
workflow.

The API will run directly on the `ubuntu-latest` runner. It will not be declared as a service
container because the API image is built from repository source after checkout, while GitHub
Actions service containers are created before job steps run.

The workflow will install and use:

- .NET SDK 10 (`10.0.x`);
- the latest available `dotnet-ef` tool;
- `curl`;
- `jq`.

Keycloak, the background worker, and the PWA will not run. The setup helper does not call them,
and ADR-0013 requires the local API authentication path to work without Keycloak.

## Data Flow

1. Check out the repository.
2. Install .NET SDK 10, `curl`, and `jq`.
3. Restore the API and install the latest `dotnet-ef`.
4. Apply all EF Core migrations to the PostgreSQL service.
5. Start the API on `http://127.0.0.1:5118` with:
   - the PostgreSQL service connection string;
   - CI-only bootstrap administrator credentials;
   - a non-development environment name.
6. Poll `GET /health` until it succeeds or a bounded timeout expires.
7. Run `scripts/setup-local-demo.sh` once and verify its ready message.
8. Run the helper a second time and verify it reports reuse of the existing active location and
   staff member, as well as the ready message.
9. Print a concise confirmation and exit successfully.
10. Always stop the background API process. Print its captured log when the smoke test fails.

The two helper runs use workflow-specific demo names and credentials so the assertions are
isolated from the script defaults.

## Failure Behaviour

The job must fail when:

- dependency installation fails;
- migrations fail;
- the API does not become healthy within the timeout;
- either helper invocation exits non-zero;
- the first run omits the final ready message;
- the second run does not report reuse of both the location and staff member;
- the second run omits the final ready message.

API output will be redirected to a temporary log file. Failure diagnostics will print that log
without printing the bootstrap administrator password or session token.

## Validation

Before completion:

- validate the workflow YAML syntax;
- run `bash -n scripts/setup-local-demo.sh`;
- reproduce the workflow's migration, API startup, health wait, and two helper runs locally
  against PostgreSQL where the environment permits;
- verify the expected first-run and second-run output assertions;
- run `git diff --check`.

The definitive GitHub-hosted verification occurs when the new workflow runs on GitHub Actions.

## Documentation

Create an active implementation plan and worker notes for this CI addition. Update
`docs/testing/local-smoke-test.md` to identify the automated workflow. Review
`docs/issues/index.md`; no new issue is expected because this work introduces no unresolved
product or architecture decision.

No ADR is required. The workflow implements the integration-test direction already accepted by
ADR-0012 and confirms the Keycloak-independent authentication path accepted by ADR-0013.
