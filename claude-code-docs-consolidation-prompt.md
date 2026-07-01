# Claude Code Prompt — Daxa POS Documentation Consolidation

Use this prompt in Claude Code after committing the completed ADR and Open Issue answers.

```text
You are working in the Daxa POS repository.

I have now answered all ADRs and Open Issues. I have committed the current documentation changes.

Your task is to review the full `./docs` directory and bring the documentation set into a consistent, implementation-ready state.

## Primary goals

1. Review all ADRs, Open Issues, architecture docs, module docs, deployment docs, testing docs, integration docs, region docs, and planning docs.
2. Apply the accepted ADR and OI decisions consistently across the documentation.
3. Update outdated, contradictory, or incomplete content.
4. Preserve the project direction:
   - Daxa POS is one configurable platform.
   - It supports cloud, local, and hybrid deployment modes.
   - Multi-location is built in from day zero.
   - Tax handling must be country-agnostic and configuration-driven.
   - Payment integrations must be provider-agnostic.
   - Local POS operation must remain resilient and offline-aware.
   - Cloud/admin identity and local POS authentication are separate concerns.
   - Source financial/order data must be immutable once created, except lifecycle/status changes such as void/refund.
5. Keep a clear audit trail of documentation changes.

## Important ADR/OI handling rules

- Do not reopen ADRs or OIs unless there is a serious contradiction or missing decision.
- If an ADR is accepted, treat it as authoritative.
- If an OI has a decision addendum, treat it as closed/resolved.
- If ADR-0009 exists, ensure it is marked as superseded/rejected by ADR-0013.
- Treat ADR-0013 as the accepted identity/authentication strategy.
- Do not silently change accepted decisions. If a decision seems wrong or incomplete, create a new proposed follow-up ADR or Open Issue instead.
- If a document still says “AU/NZ only” where the current direction is country-agnostic, update it to describe configurable tax/region behaviour while still allowing AU/NZ as examples.
- Keep examples where useful, but do not make AU/NZ the core architecture assumption.

## Documentation update rules

For every file you edit:

1. Preserve useful existing content.
2. Remove or rewrite contradictions.
3. Prefer additive clarification over destructive rewriting.
4. Keep Markdown clean and consistent.
5. Keep headings predictable.
6. Use relative links.
7. Update indexes and manifest files where needed.
8. Update status tables where ADRs/OIs have moved state.
9. Do not invent implementation details that have not been decided.
10. If you need a decision from me, create an Open Issue instead of guessing.

## Changelog requirements

Create or update `./docs/CHANGELOG.md`.

Track documentation work in reverse chronological order.

Each entry should include:

- Date
- Summary
- Files changed
- ADRs/OIs applied
- Follow-up items created

Use this format:

```md
## YYYY-MM-DD

### Summary

- ...

### Files changed

- `docs/...`
- `docs/...`

### Decisions applied

- ADR-0001 — ...
- OI-0001 — ...

### Follow-up items

- None
```

If `CHANGELOG.md` already exists, append a new entry at the top.

## Work tracking requirements

Create or update:

```text
./docs/plans/active/PLAN-docs-consolidation.md
```

Use it to track:

- What you reviewed.
- What you changed.
- What is still pending.
- Any new ADR/OI candidates.
- Any unresolved contradictions.
- Any documents that need human review.

Use checkboxes so progress is visible.

## Review order

Work in this order:

1. `docs/README.md`
2. `docs/MANIFEST.md`
3. `docs/adr/index.md`
4. All ADR files
5. `docs/issues/index.md`
6. All OI files
7. `docs/architecture/`
8. `docs/modules/`
9. `docs/deployment/`
10. `docs/testing/`
11. `docs/integrations/`
12. `docs/regions/`
13. `docs/plans/active/`
14. Any remaining documentation files

## Specific consistency checks

Check and update references for:

### Tax

- Tax engine is line-based.
- Tax configuration is country-agnostic.
- Tax rates, rounding, markers, and receipt labels are configurable.
- Tax-free marker defaults to `F`, but is configurable per location.
- AU/NZ can appear as examples only, not as hard-coded architecture assumptions.
- Historical tax snapshots must remain immutable.

### Payments

- Payment architecture is provider-agnostic.
- Stripe Terminal is the first implemented provider.
- Stripe BBPOS WisePOS E is the first reference terminal.
- Future providers remain possible through adapters.
- Core POS flow must not depend directly on Stripe-specific code.

### Identity and auth

- ADR-0013 replaces ADR-0009.
- Cloud/admin login uses Keycloak or equivalent identity provider.
- Local POS staff login uses trusted device identity plus staff ID/PIN.
- Local manager/admin login can use username/password through Daxa WebAPI for MVP simplicity.
- Daxa WebAPI owns application-level authorization.
- Staff PIN login must not be used for sensitive back-office/admin operations.

### Sync

- Local/hybrid sync must distinguish operational data from configuration data.
- Operational data should be append-only/event-like where possible.
- Configuration conflicts should use deterministic rules and admin review where required.
- Conflicts should be surfaced in the admin UI by client and location.
- Multi-location must be assumed from day zero.

### Financial records and audit

- Financial source records are immutable once created.
- Refund/void/status fields can change through controlled lifecycle events.
- Retention is configurable in days.
- Default retention is 7 years expressed as days.
- Daily cleanup should use the configured retention value.
- PDFs should be generated from immutable source data and should not require storing hundreds of static PDFs unless needed.
- Audit logs must capture serious changes.

### Local deployment

- Local server runs Linux and Docker Compose.
- Hardware reference is small form-factor PC, likely Intel CPU, 64GB RAM, 512GB storage initially.
- Final hardware minimum must be proven through testing/certification.
- Docker images should eventually be signed and distributed through a private registry.

### Device strategy

- Devices have identity separate from user identity.
- Installed/local clients may store device registration config locally.
- PWA devices use server-issued device identity stored in browser/device storage.
- Device registration uses a configured PIN/enrolment code.
- Device registration should be auditable and revocable.

## Output expectations

Proceed with the review and updates.

Do not ask me questions before starting.

If you find uncertainty:

1. Make the safest documentation-only improvement possible.
2. Create a follow-up Open Issue if a decision is required.
3. Record it in `PLAN-docs-consolidation.md`.
4. Record it in `CHANGELOG.md`.

At the end, provide:

1. Summary of work completed.
2. Files changed.
3. ADR/OI decisions applied.
4. New issues or ADRs created.
5. Remaining questions for me, if any.
6. Recommended next Claude Code prompt for moving from documentation into implementation planning.

Do not modify source code yet unless the repository currently contains only documentation and no implementation code. Focus on documentation consolidation and planning.
```
