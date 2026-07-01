# Claude Worker Cycle — Daxa POS

## Purpose

This project should use focused Claude Code workers one at a time.

Daxa POS is a large platform with multiple modules, deployment modes, and device types. Broad, uncontrolled workers can easily create conflicting architecture.

Each worker must operate within a defined scope and leave enough documentation for the next worker to continue without rediscovering the same information.

---

# Core Rule

Use focused workers sequentially.

Do not run multiple broad workers trying to alter the same area at the same time.

---

# Worker Cycle

Each worker must:

1. Read context.
2. Create or update a plan.
3. Make no more than three meaningful changes.
4. Run relevant tests.
5. Update documentation.
6. Commit changes.
7. Leave handoff notes.

---

# Required Context Review

Before changing code, every worker must read:

- `CLAUDE.md`
- `docs/README.md`
- `docs/adr/index.md`
- Relevant accepted ADRs
- Relevant proposed ADRs
- Relevant open issues
- Relevant active plans
- Relevant module documentation
- Relevant deployment documentation
- Relevant testing documentation

If a required context file is missing, the worker must document the gap and either create it or create an open issue.

---

# Meaningful Change Limit

Claude Code must not make more than three meaningful changes without updating the active plan.

A meaningful change includes:

- Adding a module.
- Changing database schema.
- Changing API contracts.
- Changing tenant isolation.
- Changing location isolation.
- Changing identity/authentication.
- Changing authorization.
- Changing device registration.
- Changing payment logic.
- Changing tax logic.
- Changing pricing/surcharge logic.
- Changing order lifecycle.
- Changing receipt output.
- Changing sync behaviour.
- Changing deployment behaviour.
- Changing tests.
- Changing documentation structure.
- Adding major UI workflow behaviour.

---

# Worker Types

## Architecture Worker

Owns:

- ADRs.
- System design.
- Module boundaries.
- Deployment modes.
- Cloud/local/hybrid architecture.
- Multi-location model.
- Device strategy.
- Unresolved decisions.

Must not implement large features unless explicitly scoped.

## Database Worker

Owns:

- Schema.
- Migrations.
- Entity relationships.
- Tenant/location keys.
- Audit records.
- Payment ledgers.
- Refund records.
- Tax snapshots.
- Stock records.
- Sync records.
- Gift card/store credit records later.

Must include migration tests where possible.

## API Worker

Owns:

- API endpoints.
- Contracts.
- Validation.
- Authorization.
- Tenant/location scoping.
- API documentation.
- Error responses.
- Idempotency endpoints where relevant.

## MAUI POS Worker

Owns Daxa Terminal.

Responsible for:

- Windows MAUI app shell.
- Staff POS screen.
- Terminal registration.
- Venue/location/terminal context.
- Product tile rendering.
- Order panel.
- Payment workflow UI.
- Full-screen/borderless behaviour.
- Barcode scanner keyboard-wedge input.
- Connection state display.

## Customer Display Worker

Owns Daxa Display.

Responsible for:

- Second MAUI window.
- Customer-facing order summary.
- Idle branding.
- Payment prompts.
- Payment approved/declined views.
- Receipt prompt placeholder.
- Display assignment/config.
- Ensuring Daxa Display is separate from KDS.

## PWA Admin Worker

Owns Daxa Back Office.

Responsible for:

- Admin shell.
- Product/menu management.
- Tax/pricing/surcharge config.
- Payment provider setup.
- Device/terminal config.
- User/role management.
- Reporting views.
- Audit log views.

## PWA KDS Worker

Owns Daxa KDS.

Responsible for:

- Station login.
- Station routing.
- KDS ticket state.
- Bump/complete workflow.
- Realtime updates.
- Reconnect full-state reload.
- KDS PWA UI.

## Tax Worker

Owns:

- AU GST.
- AU GST-free.
- NZ GST.
- NZ zero-rated/exempt.
- Global tax-line model.
- Tax-inclusive/exclusive pricing.
- Tax snapshots.
- Tax reports.
- Receipt tax markers.

Must preserve the `F = GST-free` receipt marker model.

## Pricing and Surcharge Worker

Owns:

- Pricing rules.
- Modifier pricing.
- Location pricing.
- Happy hour.
- Public holiday surcharge.
- Sunday surcharge.
- Card surcharge.
- Service charge.
- Discount rules.
- Promotion rules later.

## Payments Worker

Owns:

- Cash payments.
- Manual external EFTPOS.
- Integrated payment abstraction.
- Split payment records.
- Refunds/voids.
- Payment ledger.
- Provider status handling.
- Payment idempotency.

## Payment Provider Worker

Owns provider-specific integrations:

- Tyro.
- Zeller.
- Square Terminal.
- Stripe Terminal.
- Windcave.
- Adyen later.
- Worldline later.
- Global Payments later.

Must not hard-code provider assumptions into the core order/payment model.

## Printing Worker

Owns:

- Receipt rendering.
- Print queue.
- ESC/POS.
- Printer config.
- Cash drawer commands.
- Kitchen/bar dockets.
- Label printing later.
- Reprint audit.

## Inventory Worker

Owns:

- Stock items.
- Stock movements.
- Stock adjustments.
- Waste/spoilage.
- Sold-out states.
- Daily production counts.
- Recipes/BOM later.
- Stocktake.
- Supplier records later.

## Sync / Offline Worker

Owns:

- Daxa Sync.
- Local-to-cloud sync.
- Cloud-to-local sync.
- Offline queues.
- Idempotency.
- Conflict handling.
- Sync status.
- Backup/export hooks.

## Infrastructure Worker

Owns:

- Docker.
- Deployment.
- Volumes.
- Health checks.
- Reverse proxy.
- TLS/certs.
- Backup/restore.
- Local server setup.
- Cloud deployment.
- Hybrid deployment.

## Testing Worker

Owns:

- Test strategy.
- Test coverage.
- Smoke tests.
- Integration tests.
- Migration tests.
- Tax tests.
- Payment tests.
- Sync tests.
- Receipt tests.

## Documentation Worker

Owns:

- Documentation consistency.
- Changelog.
- Issue hygiene.
- ADR updates.
- Roadmap updates.
- Worker backlog updates.
- Cross-doc consistency.

## Security Worker

Owns:

- Security overview.
- Tenant isolation.
- Location isolation.
- Permission model.
- Device token security.
- Payment credential security.
- Support access security.
- Audit requirements.
- Secrets handling.

---

# Handoff Note Template

At the end of work, leave a handoff note in the active plan or worker notes.

Use this structure:

```text
Completed:
- ...

Tests:
- ...

Docs updated:
- ...

Commits:
- ...

Assumptions:
- ...

Risks:
- ...

Next recommended worker:
- ...

Open questions:
- ...
```

---

# Plan File Requirements

Each worker must create or update:

```text
docs/plans/active/PLAN-xxxx-title.md
docs/plans/active/<worker-name>-notes.md
```

The plan must include:

- Goal.
- Scope.
- Non-goals.
- Files likely to change.
- Architecture assumptions.
- Domain assumptions.
- Risks.
- Tests to run.
- Documentation to update.
- Commit sequence.
- Rollback notes if relevant.

---

# Issue Handling

If the worker discovers unresolved questions, create an issue:

```text
docs/issues/closed/OI-xxxx-title.md
```

Update:

```text
docs/issues/index.md
```

Issues must include:

- Summary.
- Context.
- Impact.
- Options if known.
- Recommended next step.
- Owner/worker type if known.
- Status.

---

# ADR Handling

If the worker makes or proposes an architectural decision, create an ADR.

Proposed ADRs:

```text
docs/adr/accepted/ADR-xxxx-title.md
```

Accepted ADRs after human approval:

```text
docs/adr/accepted/ADR-xxxx-title.md
```

Superseded ADRs:

```text
docs/adr/superseded/ADR-xxxx-title.md
```

Do not rewrite accepted ADR history. Create a new ADR and supersede the old one.

---

# Commit Rules

Each completed logical change must be committed.

Good commit examples:

```text
docs: add Daxa deployment mode ADRs
feat(tax): add AU GST-free mixed basket calculation
feat(payments): add provider adapter abstraction
feat(devices): add terminal registration model
test(receipts): verify GST-free marker output
infra: add local docker compose stack
```

Do not batch unrelated changes.

---

# Pull Request Notes

When creating a PR, include:

- Summary.
- Related issues.
- ADRs affected.
- Tests run.
- Documentation updated.
- Screenshots for UI changes where useful.
- Deployment notes where applicable.
- Known gaps.
- Human review required.

---

# Worker Stop Conditions

A worker may stop for human input only when:

- Credentials or secrets are required.
- A destructive operation is required.
- A production-impacting operation is required.
- A product or architecture decision is genuinely ambiguous.
- Required source files are missing and no further useful inspection can be completed.

Otherwise, the worker should proceed with reasonable assumptions and document them.

---

# Worker Completion Criteria

A worker is complete only when:

- Scoped work is done.
- Tests were run.
- Test results are documented.
- Docs were updated.
- ADRs/issues were updated where needed.
- Commit(s) were made.
- Handoff notes exist.
- Remaining risks and open questions are recorded.

Do not claim production readiness unless production deployment, security, tests, and documentation are complete.
