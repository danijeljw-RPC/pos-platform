# PLAN — Multi-Language and Localisation

## Status

Draft — planning-only placeholder. **Not started. Blocked on [ADR-0016](../../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) acceptance.**

This document exists so a future worker has somewhere to start from — it is not an active implementation plan yet, and no code, schema, or migration work should begin from it without first re-reading ADR-0016's current status and refreshing this plan's Context Read / Files Likely To Change against whatever has been built in the meantime (most likely PLAN-0004's catalog/menu/tax schema).

## Goal

Implement real multi-language/localisation support across Daxa POS, per the strategy decided in ADR-0016: application UI localisation, business-data translation records, receipt/tax label localisation, and localised rendering of audit log entries — without breaking single-culture MVP operation for tenants that never configure a second language.

## Scope

Once unblocked, this plan should cover (see ADR-0016 for the authoritative decision on each):

- `{Entity}Translation` tables for the translatable business entities that exist by the time this plan starts (at minimum `Product`, `Category`, `Modifier`; likely also `Menu`, `Surcharge` depending on what PLAN-0004/later plans have shipped).
- Default culture + optional supported-culture-set storage at the tenant/organisation/location level (granularity to be resolved here, per ADR-0016's Negative consequences note).
- The three-step fallback resolution (requested culture → tenant/location default → invariant/base entity name) as a shared, reusable mechanism — not reimplemented per entity.
- `ReceiptLabelTranslation` (or equivalent) for receipt/tax-summary wording, extending ADR-0011's existing configurable-marker mechanism rather than replacing it.
- API-side UI localisation (`IStringLocalizer`/`.resx`, `RequestLocalizationMiddleware`) for validation messages and system labels.
- MAUI (Daxa Terminal/Display) and PWA (Admin/KDS) UI localisation, using whichever mechanism is idiomatic once the PWA frontend framework is chosen.
- Localised rendering of audit log entries from `AuditEvent.EventType` + metadata (read-time rendering, not stored rendered text).

## Non-goals

Unchanged from ADR-0016 §8 — still out of scope when this plan is eventually activated, unless a separate ADR/plan explicitly picks one of these up:

- Translation management UI.
- Machine translation / auto-translate integration.
- Per-user language preference (as opposed to per-tenant/location default).
- Full multi-language receipt rendering (dual-language layout).
- Right-to-left layout support.
- Language-specific product catalogues.
- Translation import/export (CSV/XLIFF).

## Context Read

To be re-read in full before any implementation step is taken, since this plan may sit dormant for multiple other plans' worth of time:

- `CLAUDE.md`
- `docs/README.md`
- `docs/adr/index.md`
- `docs/adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md` (or `docs/adr/accepted/...` if accepted by the time this starts — update this plan's links accordingly)
- `docs/adr/accepted/ADR-0006-tax-line-based-tax-engine.md`
- `docs/adr/accepted/ADR-0011-receipt-tax-marker-strategy.md`
- `docs/adr/accepted/ADR-0003-multi-location-by-default.md`
- `docs/architecture/tenancy.md`, `tax-engine.md`, `overview.md`
- `docs/modules/catalog.md`, `tax.md`, `receipts.md`, `audit.md`
- Whatever `PLAN-0004-catalog-menu-tax-pricing-planning.md` actually shipped as (its current draft state, or the real implemented schema if complete by then)
- Current source for `Product`/`Category`/`Modifier`/`Menu`/tax entities, whatever exists at the time

## Files Likely To Change

Not enumerated yet — deliberately left blank. The actual entity/schema shape depends entirely on what PLAN-0004 (and any later catalog/menu/tax work) has already built by the time this plan is activated. Populating this section now, ahead of that schema existing, would be guesswork this plan's own template (`docs/plans/templates/PLAN-template.md`) doesn't ask for prematurely — a future worker should fill this in as the first concrete step of turning this from a placeholder into an actionable plan.

## Architecture Assumptions

- ADR-0016 is accepted (or its successor/replacement, if amended before acceptance) before this plan starts real work.
- The translation-row pattern (§3 of ADR-0016) applies uniformly — no entity gets a bespoke one-off translation mechanism.
- Tenant isolation (ADR-0015) applies to every new translation table exactly as it applies to the entity being translated — a `ProductTranslation` row is exactly as tenant-scoped as the `Product` row it translates.

## Domain Assumptions

- MVP ships and operates correctly with zero translation rows anywhere (single default culture, per ADR-0016 §7) — this plan adds the second-culture capability on top of that, it does not require MVP to be re-architected to reach it.
- AU/NZ (`en-AU`/`en-NZ`) remains the first concrete culture in any example data or test fixture this plan adds, consistent with ADR-0016 §5's "AU/NZ remains the first implementation example" rule.

## Risks

- **Staleness risk is the primary risk for this specific plan.** Because it's written before its own prerequisites (ADR-0016 acceptance, PLAN-0004 schema) exist, large parts of it will need rewriting once activated rather than merely resumed. This is accepted deliberately — the alternative (not writing a placeholder at all) leaves no trail for the next worker to find ADR-0016's implementation starting point.
- Scope creep into the ADR-0016 §8 non-goals (translation UI, machine translation, etc.) is a standing risk for any localisation work — re-check every task against that list before adding it.

## Implementation / Documentation Steps

Not yet defined — this plan is intentionally a placeholder, not an implementation-ready plan. The first real step, when this plan is activated, is to rewrite this section (and Files Likely To Change, above) against ADR-0016's then-current status and whatever schema actually exists, following `docs/plans/templates/PLAN-template.md`'s full structure with concrete steps, exactly as PLAN-0003 did in its own planning pass before Milestone A began.

## Tests To Run Later

- Fallback resolution tests (requested → tenant/location default → invariant), including the zero-translation-rows MVP case.
- Tenant isolation tests for every new translation table, mirroring `TenantIsolationTests.cs`'s pattern from PLAN-0003.
- Receipt rendering tests confirming AU/NZ wording still renders correctly with zero configured translations (regression protection for the MVP default path).

## Documentation To Update

- Whichever of `docs/architecture/*`, `docs/modules/*` this plan actually touches, at the time it's implemented.
- `docs/adr/proposed/ADR-0016-...md` → move to `docs/adr/accepted/` if not already accepted by the time this plan starts (or update this plan if ADR-0016 was amended/superseded first).

## ADRs Required

- None new anticipated — this plan implements ADR-0016. If implementation reveals a genuine architectural fork ADR-0016 didn't resolve (e.g. the organisation-vs-location default-culture granularity question it explicitly left open), raise a new ADR at that point rather than deciding it silently inside this plan.

## Open Issues Required

- None yet.

## Commit Sequence

Not defined — see Implementation / Documentation Steps above.

## Handoff Notes

This plan was created as a planning-only placeholder alongside ADR-0016, during a PLAN-0003 Milestone D/E boundary session. It intentionally contains no actionable implementation steps yet. **Do not start implementation from this document as-is** — first confirm ADR-0016's status, confirm what PLAN-0004 actually shipped, then rewrite this plan properly before any code is touched.
