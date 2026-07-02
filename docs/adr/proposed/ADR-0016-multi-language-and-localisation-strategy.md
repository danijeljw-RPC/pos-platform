# ADR-0016 — Multi-Language and Localisation Strategy

## Status

Proposed

## Date

2026-07-02

## Context

Daxa POS is planned for AU/NZ launch first, then APAC, North America, and EMEA expansion (`CLAUDE.md`, `docs/03-phase-roadmap.md` Phase 4). The phase roadmap already lists "Multi-language" and "Multi-currency" as Phase 4 items, and `docs/modules/16-internationalisation-industry-templates.md` lists "Language support: Later" without a strategy behind it. No ADR currently decides *how* multi-language support should be architected.

This matters now, before PLAN-0003 (Identity, Tenancy, Locations, Devices) closes out and PLAN-0004 (Catalog, Menu, Tax, Pricing) starts building the product/menu/tax data model, because retrofitting translation support onto an already-shipped schema is expensive: if `Product.Name`, `Category.Name`, tax labels, and receipt text are modelled as single untranslated columns now, adding real translation later means a migration touching every business-data table plus every place that reads `.Name` directly.

Several existing accepted decisions already constrain this ADR and must not be re-litigated:

- **ADR-0006** (tax-line based tax engine): tax is configuration-driven, never hard-coded per country. This ADR must extend that principle to tax *labels*, not just tax *calculation*.
- **ADR-0011** (receipt tax marker strategy): receipt tax markers are already configurable per location/category/item, country-agnostic by design, with historical snapshots preserved. Localisation of receipt text is a natural extension of a decision already made, not a new one.
- **ADR-0003** (multi-location by default) and **PLAN-0003**'s Human Decision to descope `Region`/`Country` as first-class entities: this ADR must not reintroduce country-keyed logic through the back door by making language a property of country. Language/culture is its own axis, independent of tenant/organisation/location/country.
- **CLAUDE.md**'s explicit worked examples (`Flat white`, `F = GST-free`, `Includes GST`) are AU/NZ **illustrations** of the product, not architecture requirements to hard-code English wording — consistent with how ADR-0011 already treats its own AU/NZ example.

The immediate trigger for writing this now rather than later: PLAN-0003 Milestone D just shipped `Organisation`/`Location`/`Terminal` management endpoints with a single `Name` column each, and PLAN-0004 is next in the queue and will add `Product`/`Category`/`Modifier`/`Menu` — all of which are exactly the entities this ADR's translation pattern needs to apply to. Deciding the strategy now, without yet implementing it, lets PLAN-0004 build its schema in a way that doesn't block adding translations later.

## Decision

Daxa POS adopts a **deferred-but-non-blocking** multi-language strategy: plan and document the full approach now; implement only the minimum needed for MVP (a single default culture); ensure no schema or architecture decision made between now and full implementation forecloses adding real translation support later.

### 1. Five separate localisation types

Multi-language is not one problem — it is five, and they must not be conflated into one generic "translation" mechanism:

| Type | What it covers | Who controls the content | Mechanism |
|---|---|---|---|
| **Application UI localisation** | Menus, buttons, screen labels, validation messages, system labels in admin/back-office, POS, KDS, customer display | Daxa (ships with the product) | Standard .NET localisation (`IStringLocalizer`, `.resx`) |
| **Business/customer-configured data translations** | Product names, category names, modifier names, menu section names, surcharge names | Tenant/venue staff | Per-entity translation records keyed by culture code |
| **Receipt/print text localisation** | Receipt headers, "Total", "Refund", tax summary labels, footer text | Daxa (defaults) + tenant override | Configurable label set per location, culture-keyed, extending ADR-0011's existing configurable-marker pattern |
| **Tax/legal label localisation** | "GST", "VAT", "Includes GST", tax-free marker legends | Daxa (defaults) + tenant override, constrained by local legal requirements | Same mechanism as receipt text, scoped under the tax definition (ADR-0006) |
| **Audit/system event text** | Human-readable rendering of audit log entries | Never stored as rendered text | Stable event codes + structured metadata, rendered in the viewer's language at read time (see §6) |

These five types have different content owners, different update cadences, and different mechanisms. A single "translations table for everything" would incorrectly conflate Daxa-shipped UI copy (versioned with the app) with tenant-owned business data (versioned with the tenant's catalogue) with legally-constrained tax wording (versioned with jurisdiction rules).

### 2. UI localisation uses standard .NET patterns

Application UI strings (admin/back-office, POS, KDS, customer display, validation messages, system labels) use framework-idiomatic localisation, not a bespoke in-house string system:

- **ASP.NET Core API** (validation messages, error responses): `IStringLocalizer<T>` / `.resx` resource files, with `RequestLocalizationMiddleware` selecting culture from a request header or the authenticated session's tenant/location default (see §4) — never from a client-supplied free-text locale string trusted without validation against a known-culture allowlist.
- **.NET MAUI** (Daxa Terminal, Daxa Display): `.resx`-based resource localisation, the standard MAUI/`AppResources` pattern.
- **PWA** (Admin, KDS, non-Windows fallback): whichever i18n library is idiomatic for the eventual frontend framework choice (`docs/architecture/overview.md` currently lists "React or Blazor WASM (TBD)"). This ADR fixes the *principle* — no hard-coded UI strings scattered through component code — not the specific library, since that depends on a framework decision this ADR does not make.
- Validation messages returned by the API must be resolvable to a code (e.g. `"validation.name.required"`) with parameters, not a pre-formatted English sentence, so a client can render them in the user's UI language even if the API responds before full server-side localisation exists.

### 3. Business data uses translation records, never per-language duplicate entities

Translatable business entities get a companion `{Entity}Translation` table, not duplicated rows per language. A product does not become two products because it has two languages — it becomes one product with translation rows.

```text
Product                          ProductTranslation
├─ Id                            ├─ Id
├─ TenantId                      ├─ ProductId (FK)
├─ (name fields removed          ├─ CultureCode      (e.g. en-AU, ja-JP)
│   or kept as invariant/         ├─ Name
│   fallback — see §4)           ├─ Description (optional)
├─ ...                           └─ UpdatedAtUtc
└─ ...
```

The same shape applies to `CategoryTranslation`, `ModifierTranslation`, `MenuTranslation`, `SurchargeTranslation`, and `ReceiptLabelTranslation` (for §1's receipt/tax label type — these are Daxa-defaulted but tenant-overridable, so they use the same translation-row mechanism as business data, keyed additionally by location where the override is location-scoped).

Culture codes are standard BCP-47 / .NET `CultureInfo` names — `en-AU`, `en-NZ`, `ja-JP`, `zh-CN`, `zh-HK`, `ko-KR`, `fr-FR`, etc. — never a bespoke enum of supported languages. This keeps the set of supported cultures a data concern (which rows exist), not a code concern (which enum values exist).

This ADR does **not** decide the exact column list, cascade/delete behaviour, or whether the base entity retains an inline `Name` (invariant fallback) column or moves `Name` entirely into the translation table with a distinguished "invariant" culture row — those are implementation details for whichever plan actually builds each entity (PLAN-0004 for catalog/menu, a later plan for surcharges/receipts). This ADR fixes the *pattern* (translation rows, not duplicate entities) so that plan doesn't have to re-derive it.

### 4. Default language and fallback

Each tenant (or a more granular level — organisation or location; the exact level is left to the plan that implements it, consistent with §3's scope) has:

- A **default culture/language** (required).
- An optional **set of supported cultures/languages** (may start empty/single-value for MVP).

Resolution order when rendering any translatable text:

```text
1. Requested culture (from the authenticated session, device locale, or explicit request parameter)
2. Tenant/location default culture
3. Invariant/base entity value (the untranslated fallback, e.g. the name the item was created with)
```

Step 3 is the safety net that makes MVP's single-culture operation (§7) forward-compatible: an entity with zero translation rows still renders correctly today, and adding a translation row later doesn't require a migration or a change to the fallback logic — it just gives step 1/2 something to match before falling through to step 3.

This fallback rule applies uniformly across all five localisation types in §1, with each type resolving "requested culture" from its own appropriate source (an authenticated user's session for UI copy, a receipt's location settings for receipt text, an audit viewer's own locale for rendered audit messages).

### 5. Receipts and tax labels are localisable, never hard-coded

Extending ADR-0011's already-accepted configurable-marker precedent: every piece of receipt and tax-summary wording — "Includes GST", "GST-free", "VAT", "Tax", "Total", "Refund", the tax-free marker legend text — is a configurable, localisable label, not a string literal in receipt-rendering code.

- AU/NZ wording (`GST`, `Includes GST`, `F = GST-free`) remains the **first implementation example**, exactly as ADR-0011 already treats its own AU/NZ example — it is not removed or generalised away, it is simply not hard-coded into the architecture.
- The tax engine (ADR-0006) already forbids `if country == "AU"` branching for tax *calculation*; this ADR extends the same non-negotiable rule to tax *label rendering*. A tax definition's display name, receipt marker code, and marker legend (already configurable per ADR-0011) become translatable per culture using the same `ReceiptLabelTranslation`/tax-definition-scoped mechanism as §3, not a second, separate localisation system.
- Historical receipts continue to use the label text/culture in effect at the time of sale (this is already ADR-0011's rule for marker meaning; it extends unchanged to translated label text).

### 6. Audit logs store codes and structured metadata, never pre-rendered sentences

This is not a new constraint — it is confirmation that the audit design already shipped in PLAN-0003 satisfies it, and a rule that future audit-writing code must not regress from it. `AuditEvent.EventType` is already a stable code (e.g. `"OrganisationCreated"`, `"LocalUserLoginFailed"`, `"LocationDeactivated"`) and `BeforeValue`/`AfterValue` are already structured JSON snapshots (`docs/modules/audit.md`, confirmed in the PLAN-0003 Milestone D worker notes), not rendered English sentences. Human-readable audit messages are rendered from the event code + metadata at read time, in the viewer's language, using the same UI localisation mechanism as §2 (a resource lookup keyed by event code, with metadata values interpolated). No future audit-writing code may start storing a pre-formatted English description string as a shortcut — that would silently make audit log output unlocalisable and is exactly the antipattern this ADR rules out.

### 7. MVP scope: plan now, implement minimally

Multi-language is **planned by this ADR, not implemented by it, and not implemented by PLAN-0003**. For MVP:

- A single default culture is acceptable — likely `en-AU`, matching the AU/NZ launch priority.
- No translation UI, no second language actually shipped, no `{Entity}Translation` tables need to exist yet.
- The only thing MVP-era schema/code must do is **not block** adding translation support later: prefer designs where a base entity's name/label is the "invariant" fallback (§4) rather than a design that makes English the only representable value (e.g. avoid `NVARCHAR` length limits or column semantics that assume Latin-script English text, and avoid business logic that string-matches on rendered label text instead of a stable code/id).

### 8. Non-goals (explicitly deferred to future, separate plans)

This ADR does not decide or schedule:

- Translation management UI (an admin screen for tenants to enter translations).
- Machine translation / auto-translate integration.
- Per-user language preference (as opposed to per-tenant/location default).
- Full multi-language receipt rendering (printing the same receipt in two languages, dual-language layout).
- Right-to-left layout support.
- Language-specific product catalogues (a catalogue structure that differs by language, as opposed to the same catalogue with translated labels).
- Translation import/export (e.g. CSV/XLIFF round-trip for translators).

Each of these is a legitimate future plan once the strategy in this ADR is accepted and at least one real implementation (product/category translations, most likely, per §9) exists to build on.

### 9. Documentation and follow-up

This ADR's acceptance is paired with:

- Doc updates (this session) noting that multi-language is planned-but-deferred, cross-referencing this ADR, in: `docs/architecture/overview.md`, `docs/architecture/tax-engine.md`, `docs/modules/tax.md`, `docs/modules/receipts.md`, `docs/modules/catalog.md`, `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`, `docs/03-phase-roadmap.md`.
- A new placeholder plan, `docs/plans/active/PLAN-localisation-multi-language.md`, marking where actual implementation starts once a worker is assigned to it — not started by this ADR.

## Consequences

### Positive

- PLAN-0004 (catalog/menu/tax/pricing) can design its schema today with translation support in mind, avoiding a costly retrofit migration later.
- Extends two already-accepted decisions (ADR-0006, ADR-0011) instead of inventing a parallel, inconsistent localisation approach for tax/receipts.
- Confirms the existing `AuditEvent` design (stable codes + JSON metadata) already satisfies the audit-localisation requirement — no rework needed there, just a documented rule against regressing it.
- Keeps AU/NZ as the concrete first example everywhere (receipts, tax, UI) without hard-coding it into the architecture, consistent with CLAUDE.md's own principle that worked examples are not requirements to hard-code.
- Defers all genuinely speculative work (translation management UI, machine translation, RTL, etc.) rather than over-building ahead of real need.

### Negative

- Every future plan touching a translatable entity (PLAN-0004 onward) must remember the translation-row pattern from the start, adding a small amount of design overhead even while the feature itself isn't built.
- "Default culture is left to organisation vs. location" is an open granularity question this ADR deliberately does not resolve — the implementing plan must resolve it, which could cause a small amount of rework if resolved inconsistently with how `AuthContext`/tenancy already model organisation/location scope (see PLAN-0003's `AuthContext.OrganisationId` precedent, which suggests organisation-level is the more consistent default).
- The PWA UI localisation library choice is blocked on the still-TBD frontend framework decision (`docs/architecture/overview.md`), so §2's PWA guidance is a principle, not a concrete mechanism, until that framework ADR exists.

### Risks

- If a future worker treats "MVP is single-culture" as license to hard-code English strings directly in receipt/tax rendering code (rather than through the configurable-label mechanism that already exists per ADR-0011), the retrofit cost this ADR is trying to avoid reappears. Mitigated by this ADR being referenced from `docs/modules/receipts.md` and `docs/modules/tax.md` so the constraint is visible at the point of implementation.
- If PLAN-0004 does not read this ADR before designing `Product`/`Category`/`Modifier` schemas, the translation-row pattern could be missed. Mitigated by adding this ADR to PLAN-0004's required-reading context via the doc updates in §9 (`docs/modules/catalog.md` now references it).

## Alternatives Considered

1. **Duplicate entities per language** (e.g. a separate `Product` row per language, or a JSON blob of `{ "en-AU": "...", "ja-JP": "..." }` inline on the entity). Rejected — duplicated entities break every foreign key relationship (tax category, pricing, inventory) per language; an inline JSON blob is not queryable/indexable for reporting and doesn't fit the fail-closed tenant-isolation and audit patterns already established in PLAN-0003.
2. **Full implementation now, inside PLAN-0003 or PLAN-0004.** Rejected per the explicit instruction for this session — multi-language is planned now, implemented later, once MVP's single-culture AU/NZ launch scope is validated. Building translation infrastructure before there's a second language to translate into would be speculative.
3. **Treat language as a property of country/region.** Rejected — PLAN-0003 already descoped `Region`/`Country` as first-class entities (Human Decision #2), and language does not map 1:1 to country in the APAC/EMEA markets this platform is expanding into (e.g. Hong Kong uses both `zh-HK` and `en-HK`; Singapore uses multiple official languages). Culture code is its own independent axis.
4. **A single generic "translation" table for every localisation type, including UI strings and audit text.** Rejected — see §1; conflates content with different owners, update cadences, and mechanisms (Daxa-shipped UI copy vs. tenant-owned business data vs. legally-constrained tax wording vs. never-pre-rendered audit text) into one mechanism that would fit none of them well.

## Follow-Up Work

- `docs/plans/active/PLAN-localisation-multi-language.md` — placeholder for the actual implementation plan; not started.
- PLAN-0004 (Catalog, Menu, Tax, Pricing) should read this ADR before finalising `Product`/`Category`/`Modifier`/`Menu` schema, per §3/§9.
- The organisation-vs-location granularity question for default culture (Negative consequences, above) should be resolved by whichever plan first implements §4, not deferred indefinitely.
- The PWA frontend framework decision (React vs. Blazor WASM, `docs/architecture/overview.md`) will determine the concrete UI localisation library for §2's PWA guidance.

## Related Documents

- [ADR-0003 — Multi-Location by Default](../accepted/ADR-0003-multi-location-by-default.md)
- [ADR-0006 — Tax-Line Based Tax Engine](../accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [ADR-0015 — Tenant Isolation Mechanism and POS Session Token Format](../accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md)
- [Architecture: Overview](../../architecture/overview.md)
- [Architecture: Tax Engine](../../architecture/tax-engine.md)
- [Module: Tax](../../modules/tax.md)
- [Module: Receipts](../../modules/receipts.md)
- [Module: Product Catalogue](../../modules/catalog.md)
- [Module: Audit](../../modules/audit.md)
- [Module: Internationalisation and Industry Templates](../../modules/16-internationalisation-industry-templates.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [PLAN-0006 — Terminal, Display, PWA](../../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
- [PLAN — Multi-Language and Localisation](../../plans/active/PLAN-localisation-multi-language.md)
- [Phase Roadmap](../../03-phase-roadmap.md)
