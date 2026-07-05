# Module: Product Catalogue

The product catalogue module manages products, categories, variants, and modifiers.

See also existing detail: `docs/modules/01-core-pos-sales-screen.md`, `docs/modules/06-retail.md`.

---

## Responsibilities

- Product categories.
- Products (name, description, SKU, barcode, images, price, tax category).
- Product variants (size, colour, etc.).
- Modifier groups (e.g. "Milk type", "Add-ons").
- Modifiers (e.g. "Oat milk", "Extra shot").
- Tax category assignment per product.
- Print routing per product/category.
- Sold-out state.
- Location-specific product availability.
- Location-specific price overrides.

## Core Entities

```text
Product
ProductCategory
ProductVariant
ModifierGroup
Modifier
ProductTaxCategory
```

## Implementation Status (PLAN-0004 Milestone D, 2026-07-05)

`ProductCategory` and `Product` (foundation only — variants, modifiers, and location-level overrides are Milestones E/F) are implemented with CRUD endpoints under `src/DaxaPos.Api/Endpoints/Catalog/`.

- `ProductCategory`: `Name`, `DisplayOrder`, `IsActive`. No `Code` field, so — matching the `Location`/`Organisation` precedent for name-only entities — duplicate names are not rejected.
- `Product`: `Name`, `Description?`, `Sku?`, `Barcode?`, `ProductCategoryId`, `TaxCategoryId`, `BasePrice`, `IsActive`, `IsArchived`, `ArchivedAtUtc?`, `SupersededByProductId?`. `Sku`/`Barcode` have no uniqueness constraint in this milestone either — the plan's field list treats them as plain optional identifiers, not a deduplicated business key like `TaxDefinition.Code`/`StaffMember.StaffCode`; can be revisited via an explicit ADR/OI if real duplicate-identifier conflicts surface in practice.
- **Archive-and-replace (OI-0007):** a `PATCH` that changes `Product.TaxCategoryId` archives the current row (`IsArchived = true`, `ArchivedAtUtc` set) and creates a brand-new row carrying every requested field value, linked via `SupersededByProductId`. A `PATCH` that leaves `TaxCategoryId` unchanged updates in place. Archived rows are permanent historical records — no further writes (update, deactivate, reactivate) are accepted against them (409 Conflict), though they remain readable via `GET` by id and via any historical reference. List/read endpoints exclude archived products from the default list (same "list hides, single `GET` doesn't" convention as `IsActive`) — `IsArchived` and `IsActive` are independent flags.
- The two-simultaneous-edits concurrency race on archive-and-replace is an accepted MVP risk (matching OI-0013's precedent) — no row-locking was added. Flagged for a future open issue at Milestone H, per the plan's Open Issues Required section, not opened here.
- Permission: `catalog.manage` on every endpoint, `rejectStaffPin: true` throughout — no schema change to the permission catalogue was needed.
- See `docs/plans/active/PLAN-0004-worker-notes.md`'s "Milestone D Report" for full detail and deviations.

## Implementation Status (PLAN-0004 Milestone E, 2026-07-05)

`ProductVariant`, `ModifierGroup`, `Modifier`, and `ProductModifierGroup` (attach/detach only) are implemented with endpoints under `src/DaxaPos.Api/Endpoints/Catalog/`. No pricing calculation — resolving a variant/modifier delta into a final price is Milestone F's `PriceResolver`.

- `ProductVariant`/`Modifier` carry no `OrganisationId` column of their own — scoped entirely through `ProductId`/`ModifierGroupId` respectively (a single-hop parent walk), matching the `Terminal`-derives-through-`Location` precedent from PLAN-0003 Milestone D. `ModifierGroup` is organisation-owned directly, like `ProductCategory`/`TaxCategory`.
- **`PriceDelta` (on both `ProductVariant` and `Modifier`) is a delta on the resolved base price, not an absolute amount — it may be positive, zero, or negative** (e.g. a discount variant, or an "Extra shot" modifier upcharge). Deliberately not validated with `Product.BasePrice`'s `>= 0` rule; proven by dedicated tests accepting all three signs.
- `ProductModifierGroup` has only two operations — assign (`POST`) and unassign (`DELETE`) — no list/read/update, since changing `DisplayOrder` means unassign then reassign. It has no `IsActive`/archive lifecycle either; "unassigned" (hard delete) already fully expresses removal, unlike every other catalogue entity in this plan. No list endpoint exists for this join, so attachment state and `DisplayOrder` are asserted directly against the database in tests.
- Permission: `catalog.manage` on every endpoint, `rejectStaffPin: true` throughout — no schema change to the permission catalogue was needed.
- See `docs/plans/active/PLAN-0004-worker-notes.md`'s "Milestone E Report" for full detail and deviations.

## Related Modules

- Tax (TaxCategory assignment)
- Menus (menu construction from catalogue)
- Pricing (price rules and overrides)
- Inventory (stock levels per product)

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [ADR-0016 — Multi-Language and Localisation Strategy](../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) (proposed) — business data translations (e.g. `ProductTranslation`, `CategoryTranslation`, `ModifierTranslation`) are planned but deferred. PLAN-0004 should read this ADR before finalising the catalogue schema so the design doesn't block adding translations later.
