# PLAN-docs-consolidation — Daxa POS Documentation Consolidation

## Goal

Review the full `./docs` directory and bring it into a consistent, implementation-ready state now that all ADRs and Open Issues have been answered and committed.

## Scope

- Review all ADRs, OIs, architecture docs, module docs, deployment docs, testing docs, integration docs, region docs, and planning docs.
- Apply all accepted ADR and resolved OI decisions consistently.
- Fix broken or stale index files, links, and status entries.
- Create or update `CHANGELOG.md`.

## Non-Goals

- Do not reopen decided ADRs or OIs.
- Do not invent implementation details.
- Do not modify source code.

## Findings

### Critical Index Errors

- [x] `docs/adr/index.md` — All ADRs listed as "Proposed". All are actually in `accepted/`. ADR-0009 is in `superseded/`. ADR-0013 is missing entirely.
- [x] `docs/issues/index.md` — All 10 OIs listed as "Open". All are actually in `closed/`. "Closed Issues" section says "None yet".
- [x] `docs/README.md` — Says "No ADRs have been accepted yet" and "all require human review". Both wrong.

### Link Fixes Required

- [x] `docs/architecture/security.md` — References `adr/accepted/ADR-0008`, `adr/superseded/ADR-0009`, `adr/accepted/ADR-0010`. References OI-0002 and OI-0010 as open. Should reference ADR-0013. Auth model described is outdated.
- [x] `docs/architecture/sync.md` — References `adr/accepted/ADR-0007`. Should be `adr/accepted/ADR-0007`.
- [x] `docs/architecture/tax-engine.md` — References `adr/accepted/ADR-0006`. Should be `adr/accepted/ADR-0006`.
- [x] `docs/modules/tax.md` — References `adr/accepted/ADR-0006`, `adr/accepted/ADR-0011`.
- [x] `docs/deployment/local.md` — Still shows `keycloak` as Docker Compose service (replaced by ADR-0013). References OI-0003 as open. Hardware spec was undecided (now decided).
- [x] All closed OI files — Reference ADRs as `adr/accepted/` and cross-reference OIs as `issues/open/`.
- [x] `docs/adr/accepted/ADR-0013` — References OI-0002, OI-0010, OI-0006 as `issues/open/`.
- [x] `docs/adr/superseded/ADR-0009` — Typo "Superseeded" → "Superseded".
- [x] `docs/issues/closed/OI-0008` — Top status field still says "Open".

### Manifest Incomplete

- [x] `docs/MANIFEST.md` — Only lists ~30 files. Over 80 files now exist in `./docs`.

## Changes Made

### 2026-07-01

- [x] Created this plan file.
- [x] Updated `docs/adr/index.md` — corrected Proposed/Accepted/Superseded sections, added ADR-0013.
- [x] Updated `docs/issues/index.md` — moved all 10 OIs to Closed, updated status.
- [x] Updated `docs/README.md` — removed stale "no accepted ADRs" claim, updated issues section.
- [x] Updated `docs/architecture/security.md` — updated auth model to reflect ADR-0013, fixed broken links.
- [x] Updated `docs/architecture/sync.md` — fixed `adr/accepted/` link.
- [x] Updated `docs/architecture/tax-engine.md` — fixed `adr/accepted/` link.
- [x] Updated `docs/modules/tax.md` — fixed `adr/accepted/` links.
- [x] Updated `docs/deployment/local.md` — removed Keycloak from Docker Compose, updated OI-0003 link and hardware spec.
- [x] Fixed `docs/adr/superseded/ADR-0009` typo.
- [x] Fixed `docs/issues/closed/OI-0008` status field.
- [x] Fixed all closed OI files: `adr/accepted/` → `adr/accepted/`, `issues/open/` → `issues/closed/`.
- [x] Fixed `docs/adr/accepted/ADR-0013` OI links to `issues/closed/`.
- [x] Updated `docs/MANIFEST.md` — expanded to cover all files.
- [x] Created `docs/CHANGELOG.md`.

## Still Pending

- [ ] Deeper review of all module docs (many use `adr/accepted/` — spot-checked, fixing inline).
- [ ] Review `docs/architecture/` numbered files (01-04) for any stale references.
- [ ] Review `docs/planning/` files for references that may now be stale (legacy/reference files).
- [ ] Review `docs/regions/` for AU/NZ-only framing that should be updated to configurable/country-agnostic.
- [ ] Review `docs/testing/` for stale references to proposed ADRs.
- [ ] Review `docs/integrations/` for stale references to Stripe Terminal decision now being confirmed.

## New ADR/OI Candidates

None identified during this pass.

## Unresolved Contradictions

- None identified.

## Documents Needing Human Review

- `docs/regions/` — may need country-agnostic language review.
- `docs/testing/` — may need payment/identity test updates per ADR-0013 and OI-0001.

## Related

- ADR-0013 supersedes ADR-0009
- OI-0001 → Stripe Terminal selected
- OI-0002, OI-0010 → Identity resolved by ADR-0013
- OI-0003 → Hardware spec decided
- OI-0004 → Epson TM-T88VI reference printer
- OI-0005 → Stripe BBPOS WisePOS E reference terminal
- OI-0006 → Category-based sync conflict rules
- OI-0007 → Tax config editable by manager-level + catalogue permission
- OI-0008 → Configurable per-tenant cloud data region
- OI-0009 → Operator-controlled MAUI update via Daxa Local server
