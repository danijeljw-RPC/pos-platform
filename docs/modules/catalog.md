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

## Related Modules

- Tax (TaxCategory assignment)
- Menus (menu construction from catalogue)
- Pricing (price rules and overrides)
- Inventory (stock levels per product)

## Related Plans

- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md)
- [ADR-0016 — Multi-Language and Localisation Strategy](../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) (proposed) — business data translations (e.g. `ProductTranslation`, `CategoryTranslation`, `ModifierTranslation`) are planned but deferred. PLAN-0004 should read this ADR before finalising the catalogue schema so the design doesn't block adding translations later.
