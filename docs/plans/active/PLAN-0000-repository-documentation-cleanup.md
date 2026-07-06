# PLAN-0000 — Repository Documentation Cleanup

## Status

Active

## Goal

Establish the correct `./docs` structure for Daxa POS. Create all required ADRs, plan files, issue files, and index documents. Ensure Daxa POS naming is consistent throughout. Leave the repository ready for human review before implementation begins.

## Scope

- All documentation under `./docs/`.
- `CHANGELOG.md`.
- `docs/README.md`.
- ADR index, issue index, plan files, worker notes.

## Non-goals

- Any application code (.NET, MAUI, PWA).
- Marking ADRs as accepted.
- Closing open issues.
- Pushing to remote.

## Context Read

- `CLAUDE.md`
- `docs/plans/active/planning-session-instructions.md`
- `docs/00-product-vision.md`
- `docs/01-platform-principles.md`
- `docs/02-mvp-scope.md`
- `docs/planning/03-adr-candidates.md`
- `docs/planning/04-open-questions.md`
- `docs/architecture/01-core-architecture.md`
- `docs/architecture/03-payment-adapter-architecture.md`
- `docs/architecture/04-tax-pricing-model.md`
- `.claude/settings.local.json`

## Files Likely To Change

All files under `docs/`. `CHANGELOG.md` (new).

## Architecture Assumptions

- Existing numbered docs contain valid Daxa POS content and are preserved (migrated into named files).
- Proposed ADRs reflect decisions already established in `CLAUDE.md` and existing docs.
- No ADRs are accepted without human review.

## Domain Assumptions

- Daxa POS product direction is as defined in `CLAUDE.md`.
- All 12 proposed ADRs align with established direction.

## Risks

- Cross-references in existing docs may break if files are renamed. Scan for links after moving files.
- Some existing docs are stubs with minimal content — they are preserved and marked as stubs.

## Implementation / Documentation Steps

1. ✅ Create missing directory structure.
2. ✅ Create `docs/README.md`.
3. ✅ Create `docs/plans/templates/PLAN-template.md`.
4. ✅ Create 12 proposed ADRs under `docs/adr/accepted/`.
5. ✅ Update `docs/adr/index.md`.
6. ✅ Create 9 active plan files under `docs/plans/active/`.
7. Create 10 open issue files under `docs/issues/open/`.
8. Create `docs/issues/index.md`.
9. Create missing architecture docs.
10. Create missing module docs.
11. Create missing deployment docs.
12. Create missing testing docs.
13. Create integration docs.
14. Create `CHANGELOG.md`.
15. Create `docs/plans/active/planning-session-worker-notes.md`.
16. Verify: run find/grep checks.

## Tests To Run Later

None (documentation only).

## Documentation To Update

Everything in `./docs/`.

## ADRs Required

All 12 proposed ADRs created in this session.

## Open Issues Required

All 10 open issues created in this session.

## Commit Sequence

```text
docs: build Daxa POS documentation structure and README
docs: add proposed ADRs for core architecture decisions
docs: add active planning documents
docs: add open issue register
docs: add architecture, module, deployment, testing, and integration docs
docs: update changelog for planning session
```

## Handoff Notes

After human review:

- Accept or reject each proposed ADR and move to `adr/accepted/` or `adr/superseded/`.
- Review open issues and make decisions where possible.
- Pick the first implementation plan to execute (recommend PLAN-0001 or PLAN-0002).
- Assign implementation workers.
