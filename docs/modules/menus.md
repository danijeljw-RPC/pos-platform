# Module: Menu Service

The menu service builds menus from the product catalogue and manages availability rules.

---

## Responsibilities

- Menus (named collections of products).
- Menu sections.
- Location-specific menus.
- Time and day availability rules.
- Hospitality menus (breakfast, lunch, dinner).
- Retail catalogue views.
- Food truck event menus.
- Sold-out visibility (suppress sold-out items from display).
- POS tile configuration (layout, colours, grouping).

## Core Entities

```text
Menu
MenuSection
MenuAvailabilityRule
MenuItemConfig
```

## Implementation Status (PLAN-0004 Milestone G, 2026-07-05)

`Menu`, `MenuSection`, `MenuSectionItem`, `MenuAvailabilityRule`, and the resolved-menu read endpoint are implemented under `src/DaxaPos.Api/Endpoints/Menus/`.

- **`Menu`** (`OrganisationId`, `LocationId?`) — `LocationId == null` means organisation-wide; set means location-specific. **`MenuSection`**/**`MenuSectionItem`** carry no `OrganisationId` column of their own — scoped through `MenuId`/`MenuSectionId` (a parent walk), matching the `ProductVariant`/`Terminal` precedent. `MenuSectionItem` assignment rejects an inactive or archived `Product` at configuration time (in addition to the resolved-menu endpoint's own defensive re-check at read time, since a product can be archived-and-replaced after assignment).
- **`MenuAvailabilityRule`** (`DaysOfWeekMask` flags enum, Monday-first per ISO 8601, plus `StartTimeLocal`/`EndTimeLocal`) — approved Human Decision #7's day/time shape. A menu with zero active rules is always available; one or more means available only during at least one matching window. No overnight wraparound in this milestone — `StartTimeLocal` must be strictly before `EndTimeLocal` (a venue open past midnight needs two rules). Evaluated against `Location.TimeZoneId` (a new column added to the PLAN-0003 `Location` entity this milestone, defaulting to `"UTC"`, not yet settable via `LocationEndpoints`) — never UTC-naively.
- **`GET /api/v1/menus/resolved?locationId={id}`** — the sales-screen-ready projection. Gated `.RequireAuthorization()` only, **no permission code**, matching `/auth/me`'s precedent (approved Human Decision #1) — the plan's other deliberately staff-accessible endpoint, via a different mechanism than the sold-out toggle's `Operational`-category permission. A location-bound staff-PIN session may only resolve its own location (checked the same way `ProductSoldOutEndpoints` checks it).
  - **Merge precedence** (approved Human Decision #7): for any `Product` appearing in both an organisation-wide and a location-specific menu applicable to the location, the location-specific occurrence wins outright — the organisation-wide occurrence is dropped, not merged section-by-section (two menus' sections are never reconciled by name).
  - **Exclusions:** `Product.IsActive == false`, `Product.IsArchived == true`, `ProductLocationOverride.IsAvailable == false`, `ProductLocationOverride.IsSoldOut == true`.
  - **Pricing:** resolved via Milestone F's `PriceResolver`, called with no variant and no modifiers (`MenuSectionItem` carries only a `ProductId` — variant/modifier selection happens at order time, PLAN-0005, not on the menu display). Fails closed (404) when `VenueTaxConfiguration` is missing for the location, matching `VenueTaxConfigurationEndpoints`' own missing-config behaviour (approved Human Decision #5) — this endpoint never silently defaults.
  - Each resolved item also carries its `TaxCategory.Code`/`TaxTreatment` as marker metadata (not a calculated tax amount — no order exists yet to calculate against).
- Permission: `menus.manage` + `rejectStaffPin: true` on every configuration endpoint (Menu/MenuSection/MenuSectionItem/MenuAvailabilityRule); the resolved-menu endpoint alone has no permission requirement at all.
- See `docs/plans/active/PLAN-0004-worker-notes.md`'s "Milestone G Report" for full detail and deviations.

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
