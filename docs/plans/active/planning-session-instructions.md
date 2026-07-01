# Claude Code Planning Session Instructions — Daxa POS

## Purpose

You are running a planning and documentation preparation session for the **Daxa POS** repository.

The goal is to review the repository, clean and organise `./docs`, prepare planning documents, propose ADRs, update documentation to match standards, track all changes in `CHANGELOG.md`, and leave the repository ready for human review before implementation begins.

This is a planning/documentation pass.

Do not begin product implementation unless explicitly instructed after human review.

---

# Operating Mode

The current Claude Code session is running in plan mode with permissions skipped.

You may inspect, create, edit, move, and update documentation files as needed.

You must still operate safely and deliberately.

## Core behaviour

You must:

1. Review all relevant files.
2. Understand the current repository structure.
3. Clean up and standardise `./docs`.
4. Build out missing planning documents.
5. Propose ADRs under `docs/adr/accepted/`.
6. Update existing documents to match Daxa POS standards.
7. Create or update active plan files under `docs/plans/active/`.
8. Create or update issue files under `docs/issues/open/`.
9. Update `docs/issues/index.md`.
10. Update `docs/adr/index.md`.
11. Track every meaningful documentation change in `CHANGELOG.md`.
12. Spawn sub-tasks as needed for focused review, research, planning, or cleanup.
13. Keep clear handoff notes for human review.

---

# Product Context

The project is **Daxa POS**.

Daxa POS is a configurable point-of-sale platform for:

- Cafes
- Bakeries
- Cake shops
- Food trucks
- Pubs and bars
- Restaurants
- Fast food
- Clothing stores
- Electronics stores
- General retail
- Computer repair stores
- Service businesses
- Multi-location chains
- Franchise-style organisations

## Product line

```text
Daxa POS
├─ Daxa Cloud
├─ Daxa Local
├─ Daxa Hybrid
├─ Daxa Terminal
├─ Daxa Display
├─ Daxa Back Office
├─ Daxa Payments
├─ Daxa Inventory
├─ Daxa KDS
├─ Daxa Sync
├─ Daxa Hospitality
└─ Daxa Retail
```

## Product principle

Daxa POS must be built as one configurable platform, not separate disconnected products.

Cloud, local, and hybrid are deployment modes.

Hospitality and retail are configuration/module sets, not separate codebases.

---

# Established Direction

Do not re-litigate these unless you create a proposed ADR explaining why the decision should change.

## Core decisions

1. Daxa POS uses a single codebase.
2. Daxa Cloud, Daxa Local, and Daxa Hybrid are deployment modes.
3. Every tenant supports multi-location by default.
4. A single-location customer is simply one location.
5. Windows counter POS terminals use .NET MAUI.
6. Windows customer-facing display uses a second .NET MAUI window.
7. PWA is used for admin, KDS, non-Windows devices, Linux kiosks, tablets, and future self-ordering.
8. Daxa Display is not the KDS.
9. Daxa KDS is a separate device/session.
10. Device identity and user identity are separate.
11. Payment providers use an adapter architecture.
12. AU/NZ tax support comes first.
13. The global tax model must be tax-line based.
14. Product names remain product names.
15. Tax treatment is metadata.
16. GST-free items may be marked on receipts using `F = GST-free`.
17. Financial records must not be silently edited.
18. Use voids, refunds, reversals, and adjustment records.
19. Audit logs are mandatory.
20. Offline/local resilience must be designed early.
21. Sync must use idempotency.
22. Every meaningful change must update documentation.

---

# Required First Step — Repository Review

Before changing files, inspect the repository.

Run appropriate read-only commands such as:

```bash
pwd
ls -la
find . -maxdepth 3 -type f | sort
git status
git branch
git log --oneline -10
```

Review:

```text
CLAUDE.md
CHANGELOG.md
docs/
docs/README.md
docs/adr/
docs/issues/
docs/plans/
docs/architecture/
docs/modules/
docs/deployment/
docs/testing/
docs/integrations/
```

If files or folders do not exist, create the missing structure during the docs cleanup step.

---

# Required Documentation Structure

Ensure `./docs` follows this structure.

Create missing folders and index files.

```text
docs/
  README.md

  adr/
    index.md
    proposed/
    accepted/
    superseded/

  plans/
    templates/
      PLAN-template.md
    active/
    completed/

  issues/
    index.md
    open/
    closed/

  architecture/
    overview.md
    deployment-modes.md
    tenancy.md
    multi-location.md
    sync.md
    security.md
    payment-adapters.md
    tax-engine.md
    device-strategy.md

  modules/
    catalog.md
    menus.md
    orders.md
    payments.md
    refunds.md
    tax.md
    pricing.md
    surcharges.md
    receipts.md
    printing.md
    inventory.md
    customers.md
    gift-cards.md
    devices.md
    reporting.md
    audit.md
    sync.md
    kds.md
    customer-display.md

  deployment/
    cloud.md
    local.md
    hybrid.md
    docker.md
    windows-terminal.md
    linux-kiosk-pwa.md

  testing/
    strategy.md
    tax-tests.md
    payment-tests.md
    sync-tests.md
    receipt-tests.md
    security-tests.md

  integrations/
    payments/
      tyro.md
      zeller.md
      square-terminal.md
      stripe-terminal.md
      windcave.md
      adyen.md
      worldline.md
      global-payments.md
    printers/
      escpos.md
```

Do not create empty placeholder documents unless they contain useful planning content, status, open questions, or TODOs.

---

# Documentation Cleanup Rules

When cleaning `./docs`:

## Preserve useful content

Do not delete useful documentation.

If content is outdated but useful:

- Rewrite it for Daxa POS.
- Move it to the correct folder.
- Add notes if it is superseded.
- Link it from an index if still relevant.

## Remove or quarantine stale content

If old project references remain, replace them.

Search for outdated names and terms, including:

```text
Enterprise POS
enterprise-pos
old project
taverns only
local-first only
PWA-only primary client
Phase 7 complete
Phase 8 stock next
Gift card phase complete
```

Update these to Daxa POS direction.

If a document is obsolete but should not be deleted, move or mark it clearly.

## Standardise naming

Use Daxa names consistently:

```text
Daxa POS
Daxa Cloud
Daxa Local
Daxa Hybrid
Daxa Terminal
Daxa Display
Daxa Back Office
Daxa Payments
Daxa Inventory
Daxa KDS
Daxa Sync
Daxa Hospitality
Daxa Retail
```

## Standardise deployment language

Use:

```text
Cloud
Local
Hybrid
```

Do not use “local-first only” as the overall product direction.

Daxa Local can be local-first for that deployment mode, but the whole product is not local-first-only.

---

# ADR Work

Create proposed ADRs under:

```text
docs/adr/accepted/
```

Do not mark ADRs as accepted unless explicitly instructed by the human.

## Required proposed ADRs

Create or update proposed ADRs for:

```text
ADR-0001-single-codebase.md
ADR-0002-cloud-local-hybrid-deployment.md
ADR-0003-multi-location-by-default.md
ADR-0004-windows-maui-and-pwa-device-strategy.md
ADR-0005-payment-provider-adapter-architecture.md
ADR-0006-tax-line-based-tax-engine.md
ADR-0007-local-hybrid-sync-principles.md
ADR-0008-device-identity-vs-user-identity.md
ADR-0009-keycloak-or-identity-provider-strategy.md
ADR-0010-financial-records-ledger-and-audit.md
ADR-0011-receipt-tax-marker-strategy.md
ADR-0012-docker-local-deployment-strategy.md
```

## ADR format

Each ADR must include:

```text
# ADR-xxxx — Title

## Status

Proposed

## Context

## Decision

## Consequences

## Alternatives Considered

## Open Questions

## Related Documents
```

## ADR index

Update:

```text
docs/adr/index.md
```

The index must group ADRs by status:

- Proposed
- Accepted
- Superseded

---

# Planning Documents

Create or update active plans under:

```text
docs/plans/active/
```

## Required active plan files

```text
PLAN-0000-repository-documentation-cleanup.md
PLAN-0001-architecture-foundation.md
PLAN-0002-platform-skeleton.md
PLAN-0003-identity-tenancy-locations-devices.md
PLAN-0004-catalog-menu-tax-pricing-planning.md
PLAN-0005-payments-receipts-printing-planning.md
PLAN-0006-terminal-display-pwa-planning.md
PLAN-0007-sync-local-hybrid-planning.md
PLAN-0008-testing-security-deployment-planning.md
```

## Plan format

Each plan must include:

```text
# PLAN-xxxx — Title

## Status

Active / Draft / Blocked / Complete

## Goal

## Scope

## Non-goals

## Context Read

## Files Likely To Change

## Architecture Assumptions

## Domain Assumptions

## Risks

## Implementation / Documentation Steps

## Tests To Run Later

## Documentation To Update

## ADRs Required

## Open Issues Required

## Commit Sequence

## Handoff Notes
```

---

# Issue Tracking

Create issues under:

```text
docs/issues/open/
```

Use this format:

```text
OI-xxxx-title.md
```

## Create issues for unresolved decisions

At minimum, create issues for:

```text
OI-0001-first-payment-provider.md
OI-0002-identity-provider-local-cloud-hybrid.md
OI-0003-local-server-reference-hardware.md
OI-0004-first-receipt-printer-reference-device.md
OI-0005-first-payment-terminal-reference-device.md
OI-0006-hybrid-sync-conflict-rules.md
OI-0007-tax-configuration-editing-permissions.md
OI-0008-cloud-data-region-strategy.md
OI-0009-maui-app-update-delivery.md
OI-0010-local-keycloak-vs-cloud-keycloak.md
```

## Issue format

Each issue must include:

```text
# OI-xxxx — Title

## Status

Open

## Area

## Summary

## Context

## Impact

## Options

## Recommendation

## Decision Needed

## Related ADRs

## Related Documents
```

## Issue index

Update:

```text
docs/issues/index.md
```

Group issues by area:

- Architecture
- Deployment
- Payments
- Tax
- Identity/Security
- Devices/Hardware
- Sync
- Testing
- Product

---

# Changelog

Create or update:

```text
CHANGELOG.md
```

Use this structure:

```text
# Changelog

## Unreleased

### Added

### Changed

### Fixed

### Documentation

### ADRs

### Issues

### Planning
```

Every meaningful documentation change in this planning session must be recorded under `Unreleased`.

Do not over-log tiny formatting changes, but record meaningful structure/content changes.

---

# Sub-Tasks

Spawn sub-tasks as needed.

Use sub-tasks for focused work such as:

- Documentation inventory.
- ADR review.
- Architecture planning.
- Tax planning.
- Payment planning.
- Security review.
- Deployment review.
- Testing strategy review.
- Hardware/device review.
- Sync/local/hybrid review.
- Module documentation review.

Sub-tasks must report back with:

```text
Completed:
- ...

Findings:
- ...

Files touched:
- ...

Open questions:
- ...

Recommended follow-up:
- ...
```

Keep track of sub-task results in the relevant active plan or a worker notes file.

Create:

```text
docs/plans/active/planning-session-worker-notes.md
```

Use this file to track:

- Sub-tasks spawned.
- Findings.
- Decisions proposed.
- Files updated.
- Remaining work.
- Human review items.

---

# Recursion and Continuation

Continue recursively through the documentation until:

1. `./docs` has a clean structure.
2. Old project direction has been removed or rewritten.
3. Daxa POS naming is consistent.
4. Proposed ADRs exist for the major decisions.
5. Active plan files exist.
6. Open issues exist for unresolved questions.
7. Index files are updated.
8. `CHANGELOG.md` is updated.
9. Worker notes are updated.
10. Human review list is clear.

Do not stop after the first pass if there are obvious references or missing index updates.

After each pass, run searches for stale or inconsistent terms.

Suggested searches:

```bash
grep -R "Enterprise POS\|enterprise-pos\|local-first only\|PWA client application\|Phase 7\|Phase 8" -n .
grep -R "Daxa" -n docs CLAUDE.md CHANGELOG.md
find docs -type f | sort
```

---

# Do Not Do Yet

Do not implement product code yet unless explicitly instructed.

Do not:

- Create major application modules.
- Add database migrations.
- Add payment provider integrations.
- Add MAUI app code.
- Add PWA app code.
- Delete large folders without review.
- Mark ADRs as accepted without human approval.
- Close issues without human approval or clear resolution.
- Claim implementation is complete.

This session is for planning and documentation preparation.

---

# Git Behaviour

Use Git deliberately.

Before work:

```bash
git status
git branch
```

During work:

- Keep changes focused.
- Commit logical documentation batches if instructed or if appropriate.
- Do not push unless explicitly instructed.

Suggested commit sequence:

```text
docs: clean Daxa documentation structure
docs: add proposed architecture ADRs
docs: add Daxa planning documents
docs: add Daxa open issue register
docs: update changelog for planning session
```

If committing, include the summary of files changed in the worker notes.

---

# Final Output Required

At the end of the planning session, provide a final summary with:

```text
Completed:
- ...

Files created:
- ...

Files updated:
- ...

Proposed ADRs:
- ...

Open issues:
- ...

Plans created:
- ...

Changelog updated:
- Yes/No

Stale references remaining:
- None / list

Human review required:
- ...

Recommended next Claude Code session:
- ...
```

Also update:

```text
docs/plans/active/planning-session-worker-notes.md
```

with the same final handoff.

---

# Quality Bar

The result should be good enough that the human can review the proposed ADRs, accept/reject decisions, and then start implementation planning without needing to reconstruct product direction from chat history.

When uncertain:

1. Preserve the single-codebase direction.
2. Preserve cloud/local/hybrid deployment support.
3. Preserve multi-location by default.
4. Preserve Windows MAUI for Daxa Terminal and Daxa Display.
5. Preserve PWA for admin/KDS/non-Windows devices.
6. Preserve payment adapter architecture.
7. Preserve tax-line architecture.
8. Preserve AU/NZ first tax scope.
9. Create an open issue for unresolved decisions.
10. Update the changelog and worker notes.
