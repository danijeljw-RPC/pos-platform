# Documentation Standards — Daxa POS

## Purpose

This document defines documentation standards for Daxa POS.

Daxa POS is a complex POS platform covering:

- Cloud, local, and hybrid deployments.
- Windows MAUI POS terminal.
- Customer-facing display.
- PWA admin/KDS/tablet/kiosk apps.
- Multi-tenant and multi-location architecture.
- AU/NZ tax first.
- Global tax later.
- Provider-agnostic payments.
- Retail, hospitality, food truck, bakery, pub, restaurant, and service workflows.

Documentation must remain clear, direct, current, and useful to Claude Code workers and human reviewers.

---

## General Documentation Rules

### Documentation should be

- Clear.
- Direct.
- Maintainable.
- Specific.
- Linked where useful.
- Updated with implementation.
- Honest about unresolved questions.
- Structured for future Claude Code workers.

### Documentation should avoid

- Speculative claims presented as fact.
- Outdated phase status.
- Hidden assumptions.
- Duplicated conflicting decisions.
- Product direction buried only in chat history.
- Unapproved architecture changes.
- Changing accepted ADR history.

---

## Required Documentation Updates

Update documentation when changing:

- Architecture.
- Deployment modes.
- Database schema.
- API contracts.
- Authentication.
- Authorization.
- Tenant isolation.
- Location isolation.
- Device registration.
- MAUI terminal behaviour.
- Customer display behaviour.
- PWA admin behaviour.
- KDS routing.
- Payment flows.
- Payment provider integration.
- Gift card logic.
- Store credit logic.
- Tax calculation.
- Pricing logic.
- Surcharge behaviour.
- Receipt output.
- Printing/cash drawer behaviour.
- Stock control.
- Sync/offline behaviour.
- Docker/deployment.
- Backup/restore.
- Security controls.
- Testing strategy.
- Roadmap/phases.

---

## Documentation Folder Structure

Recommended structure:

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

  integrations/
    payments/
      tyro.md
      zeller.md
      square-terminal.md
      stripe-terminal.md
      windcave.md
      adyen.md
    printers/
      escpos.md
```

---

## ADR Standards

Use ADRs for significant decisions.

### ADR locations

Proposed:

```text
docs/adr/accepted/ADR-xxxx-title.md
```

Accepted:

```text
docs/adr/accepted/ADR-xxxx-title.md
```

Superseded:

```text
docs/adr/superseded/ADR-xxxx-title.md
```

### ADR rules

- Do not edit accepted ADRs to change history.
- Create a new ADR when a decision changes.
- Supersede old ADRs instead of rewriting them.
- Link related issues and plans.
- Use clear status: Proposed, Accepted, Superseded.
- Include consequences.
- Include alternatives considered where useful.
- Accepted ADRs require human approval.

### Decisions requiring ADRs

- Single codebase.
- Cloud/local/hybrid deployment.
- Database.
- Identity provider.
- Multi-tenant model.
- Multi-location model.
- Device strategy.
- MAUI vs PWA boundaries.
- Payment adapter model.
- Tax engine model.
- Sync/offline model.
- Printer/hardware strategy.
- Deployment/security model.

---

## Issue Standards

Create open issues for unresolved questions.

Use:

```text
docs/issues/closed/OI-xxxx-title.md
```

Closed issues:

```text
docs/issues/closed/OI-xxxx-title.md
```

Update:

```text
docs/issues/index.md
```

### Issue file should include

```text
# OI-xxxx — Title

## Status

Open / Closed / Deferred

## Area

Architecture / Payments / Tax / Sync / Devices / etc.

## Summary

## Context

## Impact

## Options

## Recommendation

## Decision Needed

## Related Files

## Related ADRs

## Related GitHub Issue
```

### Issues should be created for

- Unresolved architecture decisions.
- Payment provider uncertainty.
- Hardware validation gaps.
- Security gaps.
- Testing gaps.
- Deployment gaps.
- Conflicting requirements.
- Product decisions needing human input.

---

## Plan Standards

Use plans for implementation tasks.

Use:

```text
docs/plans/active/PLAN-xxxx-title.md
```

Move completed plans to:

```text
docs/plans/completed/
```

### Plan file should include

```text
# PLAN-xxxx — Title

## Status

Active / Complete / Blocked

## Goal

## Scope

## Non-goals

## Context Read

## Files Likely To Change

## Architecture Assumptions

## Domain Assumptions

## Risks

## Implementation Steps

## Tests To Run

## Documentation To Update

## Commit Sequence

## Rollback Notes

## Handoff Notes
```

---

## Module Documentation Standards

Module documentation should explain:

- Purpose.
- Scope.
- Non-goals.
- Domain entities.
- API responsibilities.
- UI responsibilities if any.
- Data rules.
- Security rules.
- Audit rules.
- Test requirements.
- Open questions.

Example module files:

```text
docs/modules/tax.md
docs/modules/payments.md
docs/modules/orders.md
docs/modules/receipts.md
docs/modules/devices.md
docs/modules/sync.md
```

---

## Deployment Documentation Standards

Deployment docs must be split by deployment mode.

### Required deployment docs

```text
docs/deployment/cloud.md
docs/deployment/local.md
docs/deployment/hybrid.md
docs/deployment/docker.md
docs/deployment/windows-terminal.md
docs/deployment/linux-kiosk-pwa.md
```

### Deployment docs should include

- Purpose.
- Target environment.
- Required services.
- Environment variables.
- Secrets.
- Ports.
- Volumes.
- Health checks.
- Backup.
- Restore.
- Upgrade.
- Rollback.
- Monitoring.
- Troubleshooting.
- Security notes.

---

## Testing Documentation Standards

Testing docs should include:

- Required test categories.
- Critical behaviours.
- Known gaps.
- How to run tests.
- Test data.
- Manual test checklists where needed.
- Provider-specific test notes.
- Hardware-specific test notes.

Testing docs must be updated when adding:

- Tax logic.
- Payment logic.
- Receipt logic.
- Sync logic.
- Device logic.
- Security logic.
- Deployment logic.

---

## Integration Documentation Standards

Each external integration should have its own document.

### Payment provider docs should include

- Provider overview.
- Supported countries.
- Supported currencies.
- Supported hardware.
- Merchant onboarding model.
- Credential fields.
- Terminal pairing.
- Payment request flow.
- Refund flow.
- Webhooks/polling.
- Idempotency.
- Sandbox/test process.
- Certification requirements.
- Known limitations.
- Open questions.

### Printer integration docs should include

- Printer type.
- Protocol.
- ESC/POS commands.
- Receipt rendering limits.
- Cash drawer kick support.
- Retry/error handling.
- Test hardware.
- Known limitations.

---

## Changelog Standards

Maintain a changelog.

Use clear entries grouped by date or version.

Entries should include:

- Added.
- Changed.
- Fixed.
- Deprecated.
- Removed.
- Security.

Do not use the changelog as a substitute for ADRs or module docs.

---

## Cross-Document Consistency

When changing product direction, update all affected docs.

Examples:

If changing payment provider strategy, update:

- `CLAUDE.md`
- `docs/modules/payments.md`
- `docs/architecture/payment-adapters.md`
- `docs/integrations/payments/*`
- `docs/issues/index.md`
- Relevant ADRs.

If changing tax strategy, update:

- `CLAUDE.md`
- `docs/modules/tax.md`
- `docs/architecture/tax-engine.md`
- `docs/testing/tax-tests.md`
- Relevant ADRs.

If changing deployment strategy, update:

- `CLAUDE.md`
- `docs/architecture/deployment-modes.md`
- `docs/deployment/cloud.md`
- `docs/deployment/local.md`
- `docs/deployment/hybrid.md`
- Relevant ADRs.

---

## Claude Code Documentation Rule

Claude Code must not treat documentation as optional.

Every meaningful implementation change must include documentation changes or explicitly state why no documentation change was needed.

Claude Code must leave enough notes that a future worker can continue without rediscovering the same information.
