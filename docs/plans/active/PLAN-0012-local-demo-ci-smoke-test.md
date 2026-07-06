# PLAN-0012 — Local Demo CI Smoke Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a GitHub Actions workflow that proves `scripts/setup-local-demo.sh` succeeds twice
against a fresh PostgreSQL database and a live API.

**Architecture:** PostgreSQL 16 runs as a job-scoped GitHub Actions service container. EF Core
migrations and the API run directly on the Ubuntu runner; the API is health-checked before the
script runs, logged for failure diagnosis, and stopped during unconditional cleanup.

**Tech Stack:** GitHub Actions, Ubuntu, PostgreSQL 16, .NET SDK 10, latest stable `dotnet-ef`,
Bash, `curl`, and `jq`.

## Global Constraints

- Use `.NET SDK 10` via `dotnet-version: "10.0.x"`.
- Install the latest available `dotnet-ef` without pinning a version.
- Install both `curl` and `jq` explicitly.
- Start only PostgreSQL and the API; do not start Keycloak, Workers, or the PWA.
- Run the helper twice and assert the documented rerun behaviour.
- Do not modify `.github/workflows/ci.yml`.
- Preserve all unrelated working-tree changes.

---

## Status

Complete and locally verified (2026-07-07). GitHub-hosted execution remains pending until the
commits are pushed and the workflow runs.

## Scope

- Add `.github/workflows/local-demo-smoke-ci.yml`.
- Run migrations and a live API against a PostgreSQL service container.
- Run and assert `scripts/setup-local-demo.sh` twice.
- Add bounded health waiting, failure diagnostics, and unconditional API cleanup.
- Document the automated smoke test and leave worker notes.

## Non-goals

- Running the full root Compose stack.
- Running Keycloak, Workers, or the PWA.
- Changing the setup helper, API, database schema, or existing CI workflow.
- Replacing the existing build-and-test workflow.

## Context Read

- `AGENTS.md`
- `docs/README.md`
- `docs/adr/index.md`
- `docs/adr/accepted/ADR-0012-docker-local-deployment-strategy.md`
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`
- `docs/issues/index.md`
- `docs/plans/templates/PLAN-template.md`
- `docs/plans/active/PLAN-0010-docker-compose-local-dev-stack.md`
- `docs/plans/active/PLAN-0011-local-demo-setup-helper.md`
- `docs/plans/active/PLAN-0011-worker-notes.md`
- `docs/superpowers/specs/2026-07-07-local-demo-ci-smoke-test-design.md`
- `docs/deployment/docker.md`
- `docs/testing/testing-strategy.md`
- `docs/testing/local-smoke-test.md`
- `.github/workflows/ci.yml`
- `compose.yaml`
- `scripts/setup-local-demo.sh`
- `src/DaxaPos.Api/Program.cs`
- `src/DaxaPos.Api/Dockerfile`
- `src/DaxaPos.Api/Properties/launchSettings.json`

## Files Likely To Change

```text
.github/workflows/local-demo-smoke-ci.yml
docs/testing/local-smoke-test.md
docs/plans/active/PLAN-0012-local-demo-ci-smoke-test.md
docs/plans/active/PLAN-0012-worker-notes.md
docs/issues/index.md
```

## Architecture Assumptions

- The API remains authoritative and the setup helper exercises only public HTTP endpoints.
- PostgreSQL service containers are created before job steps; the source-built API therefore
  runs as a background runner process instead of a service container.
- `Directory.Build.props` permits net9.0 applications to run on the .NET 10 runtime.
- ADR-0013 intentionally permits the tested local authentication path without Keycloak.

## Domain Assumptions

- CI-only bootstrap credentials may be static because they exist only inside an ephemeral job.
- A fresh database causes the first helper run to create the named location and staff member.
- The second run reuses those active records, resets the staff PIN, and creates a fresh device
  registration PIN.

## Risks

- Installing unpinned `dotnet-ef` can expose future compatibility changes; this is intentional
  because the human explicitly requested the latest tool.
- A background API process can hide startup failure without a bounded health loop and log output.
- Assertions against overly broad output could pass incorrectly; use exact stable message
  fragments emitted by the script.

## Task 1: Add the executable CI smoke test

**Files:**

- Create: `.github/workflows/local-demo-smoke-ci.yml`
- Test: `.github/workflows/local-demo-smoke-ci.yml`

**Interfaces:**

- Consumes: `scripts/setup-local-demo.sh`, the API endpoints it calls, EF Core migrations, and
  PostgreSQL on `127.0.0.1:5432`.
- Produces: a `Local Demo Smoke Test` status check with first-run and rerun verification.

- [x] **Step 1: Verify the workflow is absent**

Run:

```bash
test -f .github/workflows/local-demo-smoke-ci.yml
```

Expected: exit status `1`, proving the new check does not exist yet.

- [x] **Step 2: Create the workflow**

Create `.github/workflows/local-demo-smoke-ci.yml` with:

```yaml
name: Local Demo Smoke Test

on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:

permissions:
  contents: read

jobs:
  local-demo-smoke:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB: daxapos
          POSTGRES_USER: daxapos
          POSTGRES_PASSWORD: daxapos_dev_password
        ports:
          - 5432:5432
        options: >-
          --health-cmd "pg_isready -U daxapos -d daxapos"
          --health-interval 5s
          --health-timeout 5s
          --health-retries 10

    env:
      API_URL: http://127.0.0.1:5118
      PWA_URL: http://127.0.0.1:8080
      ConnectionStrings__DaxaDb: Host=127.0.0.1;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password
      DAXA_BOOTSTRAP_ADMIN_EMAIL: local-demo-ci@daxapos.test
      DAXA_BOOTSTRAP_ADMIN_PASSWORD: Local-Demo-CI-Only-Passw0rd!
      ADMIN_EMAIL: local-demo-ci@daxapos.test
      ADMIN_PASSWORD: Local-Demo-CI-Only-Passw0rd!
      DEMO_LOCATION_NAME: Local Demo CI Venue
      DEMO_STAFF_CODE: CI001
      DEMO_STAFF_NAME: CI Cashier
      DEMO_STAFF_PIN: "246810"

    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: "10.0.x"

      - name: Install smoke-test dependencies
        run: |
          sudo apt-get update
          sudo apt-get install --yes curl jq
          dotnet tool install --global dotnet-ef

      - name: Restore API
        run: dotnet restore src/DaxaPos.Api/DaxaPos.Api.csproj

      - name: Apply database migrations
        run: >-
          "$HOME/.dotnet/tools/dotnet-ef" database update
          --project src/DaxaPos.Persistence/DaxaPos.Persistence.csproj
          --startup-project src/DaxaPos.Api/DaxaPos.Api.csproj

      - name: Start API
        run: |
          nohup dotnet run \
            --project src/DaxaPos.Api/DaxaPos.Api.csproj \
            --no-launch-profile \
            --urls "$API_URL" \
            >"$RUNNER_TEMP/daxapos-api.log" 2>&1 &
          echo "$!" >"$RUNNER_TEMP/daxapos-api.pid"

      - name: Wait for API health
        run: |
          for attempt in {1..60}; do
            if curl --fail --silent --show-error "$API_URL/health" >/dev/null; then
              echo "API became healthy after ${attempt} attempt(s)."
              exit 0
            fi
            sleep 1
          done

          echo "API did not become healthy within 60 seconds." >&2
          exit 1

      - name: Run local demo setup
        run: |
          set -euo pipefail
          ./scripts/setup-local-demo.sh | tee "$RUNNER_TEMP/local-demo-first.log"
          grep -Fq "Daxa POS local demo environment is ready" \
            "$RUNNER_TEMP/local-demo-first.log"

      - name: Confirm local demo setup is rerunnable
        run: |
          set -euo pipefail
          ./scripts/setup-local-demo.sh | tee "$RUNNER_TEMP/local-demo-second.log"
          grep -Fq "Reusing existing active location" \
            "$RUNNER_TEMP/local-demo-second.log"
          grep -Fq "Reusing existing active staff member" \
            "$RUNNER_TEMP/local-demo-second.log"
          grep -Fq "Daxa POS local demo environment is ready" \
            "$RUNNER_TEMP/local-demo-second.log"
          echo "Local demo setup smoke test passed."

      - name: Print API log on failure
        if: ${{ failure() }}
        run: |
          if [[ -f "$RUNNER_TEMP/daxapos-api.log" ]]; then
            cat "$RUNNER_TEMP/daxapos-api.log"
          fi

      - name: Stop API
        if: ${{ always() }}
        run: |
          if [[ -f "$RUNNER_TEMP/daxapos-api.pid" ]]; then
            kill "$(cat "$RUNNER_TEMP/daxapos-api.pid")" 2>/dev/null || true
          fi
```

- [x] **Step 3: Validate structure and shell syntax**

Run:

```bash
test -f .github/workflows/local-demo-smoke-ci.yml
rg -n "postgres:16-alpine|dotnet-version: \"10.0.x\"|dotnet tool install --global dotnet-ef|sudo apt-get install --yes curl jq|setup-local-demo.sh" .github/workflows/local-demo-smoke-ci.yml
bash -n scripts/setup-local-demo.sh
```

Expected: the file exists, every required workflow fragment is found, and Bash reports no syntax
error.

- [x] **Step 4: Run the live two-pass smoke sequence**

Run the workflow-equivalent migration, API startup, bounded health check, and two helper
invocations against an available PostgreSQL database. Use the exact environment values from the
workflow.

Expected: the first run prints `Daxa POS local demo environment is ready`; the second additionally
prints `Reusing existing active location` and `Reusing existing active staff member`.

- [x] **Step 5: Commit the workflow**

```bash
git add -- .github/workflows/local-demo-smoke-ci.yml
git commit -m "ci: test local demo setup helper"
```

## Task 2: Document and close out PLAN-0012

**Files:**

- Modify: `docs/testing/local-smoke-test.md`
- Modify: `docs/plans/active/PLAN-0012-local-demo-ci-smoke-test.md`
- Modify: `docs/plans/active/PLAN-0012-worker-notes.md`
- Modify: `docs/issues/index.md`

**Interfaces:**

- Consumes: verified workflow behavior from Task 1.
- Produces: durable CI usage, verification evidence, and handoff documentation.

- [x] **Step 1: Add the automated-CI section**

Add this after the fast-path section in `docs/testing/local-smoke-test.md`:

```markdown
## Automated CI smoke test (PLAN-0012)

`.github/workflows/local-demo-smoke-ci.yml` runs the fast path twice against a fresh PostgreSQL
service container and live API. The first run confirms setup succeeds; the second confirms the
location and staff records are reused. Keycloak, Workers, and the PWA are intentionally absent.
```

- [x] **Step 2: Record verification evidence**

Update this plan to `Complete` and add the exact validation commands and outcomes. Update
`PLAN-0012-worker-notes.md` with files changed, assumptions, commands, results, and any
GitHub-hosted verification still pending.

- [x] **Step 3: Record the issue-index review**

Update the introduction of `docs/issues/index.md` to state PLAN-0012 reviewed the index and
introduced no unresolved product or architecture question.

- [x] **Step 4: Run final checks**

Run:

```bash
bash -n scripts/setup-local-demo.sh
git diff --check
git status --short
```

Expected: Bash syntax passes, no whitespace errors are reported, and only the intended PLAN-0012
files plus pre-existing unrelated working-tree changes are listed.

- [x] **Step 5: Commit documentation**

```bash
git add -- docs/testing/local-smoke-test.md docs/plans/active/PLAN-0012-local-demo-ci-smoke-test.md docs/plans/active/PLAN-0012-worker-notes.md docs/issues/index.md
git commit -m "docs: document local demo CI smoke test"
```

## Tests To Run

- Workflow absence check before creation.
- Workflow structural assertions after creation.
- `bash -n scripts/setup-local-demo.sh`.
- EF Core migration application against PostgreSQL.
- Live API health check.
- First helper run success assertion.
- Second helper run reuse assertions.
- `git diff --check`.

## Documentation To Update

- `docs/testing/local-smoke-test.md`
- `docs/plans/active/PLAN-0012-local-demo-ci-smoke-test.md`
- `docs/plans/active/PLAN-0012-worker-notes.md`
- `docs/issues/index.md`

## ADRs Required

None. This implements ADR-0012 and confirms the Keycloak-independent path in ADR-0013.

## Open Issues Required

None expected. Review `docs/issues/index.md` and record that conclusion.

## Commit Sequence

```text
docs: plan local demo CI smoke test
ci: test local demo setup helper
docs: document local demo CI smoke test
```

## Rollback Notes

Remove `.github/workflows/local-demo-smoke-ci.yml` and its documentation references. No production
data, schema, or runtime behavior is changed.

## Handoff Notes

GitHub-hosted execution is the definitive validation of Actions orchestration. Local verification
can prove the same migrations/API/script data path but cannot fully emulate the hosted runner's
service-container lifecycle.

## Verification Results (2026-07-07)

- The pre-implementation `test -f .github/workflows/local-demo-smoke-ci.yml` check exited `1`,
  confirming the workflow was absent.
- Structural checks found PostgreSQL 16, .NET 10, unpinned `dotnet-ef` installation, explicit
  `curl`/`jq` installation, and both helper invocations in the completed workflow.
- `bash -n scripts/setup-local-demo.sh` passed.
- `rhysd/actionlint:latest` reported no errors with only the new workflow mounted read-only.
- Local runtime versions were .NET SDK `10.0.300` and the latest installed `dotnet-ef`, `10.0.9`.
- A separate ephemeral `postgres:16-alpine` container used database `daxapos_ci` on host port
  `55432`. It did not modify or stop the repository's already-running root Compose stack.
- `dotnet ef database update` applied all 17 migrations to the fresh test database and exited
  successfully.
- A separate API process started in the `CI` environment on `http://127.0.0.1:5119`, created the
  CI bootstrap administrator, and reported healthy.
- First helper run with `CI901` / `Local Demo CI Verification Venue` created one location and one
  staff member and printed `Daxa POS local demo environment is ready`.
- Second helper run reused the same location and staff IDs, reset the staff PIN, issued a fresh
  registration PIN, and printed all three workflow assertion fragments.
- The isolated API process and PostgreSQL container were stopped after verification.
- GitHub-hosted verification is not claimed; it will occur after these commits are pushed.
