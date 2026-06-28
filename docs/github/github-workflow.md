# GitHub Workflow — Daxa POS

## Purpose

This document defines the GitHub workflow for Daxa POS.

Daxa POS is a large project with multiple modules, deployment modes, and device types. GitHub workflow must keep feature work, documentation, ADRs, issues, tests, and human review aligned.

---

# Branching

Use feature branches for meaningful work.

## Branch naming

Use clear branch names.

Examples:

```text
docs/repository-foundation
docs/architecture-foundation
feature/platform-skeleton
feature/identity-tenancy-devices
feature/catalog-menu
feature/tax-pricing
feature/order-engine
feature/daxa-terminal-maui
feature/daxa-display
feature/payments-foundation
feature/receipts-printing
feature/back-office
feature/daxa-sync
feature/inventory-foundation
feature/kds-foundation
feature/payment-providers
infra/docker-compose
security/tenant-isolation
test/tax-receipts
```

## Branch rules

- Use one branch per meaningful feature or documentation package.
- Do not mix unrelated work.
- Do not combine large architecture changes with unrelated implementation work.
- Keep branches small enough for review.
- Update documentation on the same branch as implementation when relevant.

---

# Commits

Commit each logical change.

Do not combine unrelated work.

## Commit message examples

```text
docs: add Daxa deployment mode ADRs
docs: add Daxa product structure
feat(tenancy): add organisation and location model
feat(tax): calculate AU GST-free mixed baskets
feat(payments): add payment provider abstraction
feat(devices): add terminal registration model
feat(display): add customer display state model
test(tax): verify GST-free receipt marker
infra: add docker compose stack
security: enforce location-scoped authorization
```

## Commit rules

Each commit should:

- Have one purpose.
- Build or document a coherent change.
- Avoid unrelated formatting churn.
- Include tests where relevant.
- Include docs where relevant.

---

# Issues

Use GitHub issues to track implementation tasks and open questions where appropriate.

Documentation issues live in:

```text
docs/issues/open/
```

When mapped to GitHub, include the GitHub issue number in the Markdown file.

## Issue types

Use issues for:

- Open product questions.
- Architecture questions.
- Implementation tasks.
- Bugs.
- Testing gaps.
- Security gaps.
- Deployment gaps.
- Payment provider research.
- Hardware validation.
- Documentation gaps.

## Issue file format

Docs issue files should use:

```text
docs/issues/open/OI-xxxx-title.md
```

Include:

- Summary.
- Context.
- Impact.
- Options.
- Recommendation if known.
- Status.
- Related ADRs.
- Related GitHub issue if available.

---

# Pull Requests

Pull requests should include:

- Summary.
- Related issues.
- ADRs affected.
- Tests run.
- Documentation updated.
- Screenshots for UI changes where useful.
- Deployment notes where applicable.
- Security notes where applicable.
- Known limitations.
- Human review required.

## PR template

Use this structure:

```text
## Summary

## Related Issues

## ADRs Affected

## Changes

## Tests Run

## Documentation Updated

## Screenshots / UI Notes

## Deployment Notes

## Security Notes

## Known Gaps

## Human Review Required
```

---

# ADR Workflow

Significant decisions require ADRs.

## Proposed ADRs

Create proposed ADRs under:

```text
docs/adr/proposed/
```

## Accepted ADRs

Move to accepted only after human approval:

```text
docs/adr/accepted/
```

## Superseded ADRs

Do not rewrite accepted ADR history. Create a new ADR and supersede the old one.

```text
docs/adr/superseded/
```

## Decisions requiring ADRs

- Cloud/local/hybrid deployment.
- Database selection.
- MAUI/PWA app strategy.
- Identity provider.
- Multi-tenant model.
- Multi-location model.
- Payment adapter model.
- Tax engine model.
- Sync model.
- Printer/hardware strategy.
- Offline mode.
- Security boundaries.
- Significant provider integration decision.

---

# Claude Code GitHub Rule

Claude Code should not close GitHub issues unless:

- The implementation is complete.
- Tests are complete.
- Documentation is updated.
- ADRs are updated where relevant.
- The issue file is updated.
- Human review is not pending.

Claude Code must not close architectural issues without an ADR or explicit human decision.

---

# Required Status Updates

When finishing a branch or worker session, Claude Code must document:

```text
Completed:
- ...

Tests:
- ...

Docs updated:
- ...

Commits:
- ...

Open issues:
- ...

Human review:
- ...
```

This should be added to the active plan or worker notes.

---

# Release Tags

Release tags should not be created until:

- Build passes.
- Tests pass.
- Migrations are verified.
- Docker/local deployment smoke test passes.
- Documentation is updated.
- Security notes are reviewed.
- Human review is complete.

Suggested tag pattern:

```text
v0.1.0-alpha
v0.2.0-alpha
v0.3.0-beta
v1.0.0
```

---

# GitHub Project Board Suggested Columns

Suggested columns:

```text
Backlog
Ready
In Progress
Blocked
Needs Review
Testing
Docs Required
Done
```

---

# Protected Branch Recommendation

`main` should eventually require:

- Pull request review.
- Build passing.
- Tests passing.
- No direct force pushes.
- No unresolved required checks.
- Secret scanning.
- Dependabot/security alerts where available.

---

# Claude Code Review Expectations

Claude Code should call out:

- What changed.
- What did not change.
- What assumptions were made.
- What tests passed.
- What tests failed.
- What docs were updated.
- What issues remain.
- What needs human approval.

Do not claim production readiness unless production deployment, security, tests, and documentation are complete.
