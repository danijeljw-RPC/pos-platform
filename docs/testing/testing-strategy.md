# Testing Strategy — Daxa POS

## Purpose

This document defines the testing strategy for **Daxa POS**.

Daxa POS is a configurable POS platform that supports:

- Daxa Cloud
- Daxa Local
- Daxa Hybrid
- Daxa Terminal
- Daxa Display
- Daxa Back Office
- Daxa Payments
- Daxa Inventory
- Daxa KDS
- Daxa Sync
- Daxa Hospitality
- Daxa Retail

The test strategy must cover cloud, local, and hybrid deployments, plus Windows MAUI, PWA, local server, payment providers, tax, receipts, sync, and audit behaviour.

Financial, tax, payment, refund, sync, and audit behaviour must be tested heavily. These areas must not rely on manual testing only.

---

## Testing Principles

### 1. Tests are required for meaningful changes

Every implementation change should include relevant tests.

If tests are not added, the worker must clearly state why and create an open issue where appropriate.

### 2. Financial correctness is mandatory

The following must never be changed without tests:

- Order totals
- Tax calculation
- GST/GST-free handling
- Discounts
- Surcharges
- Payments
- Split payments
- Refunds
- Voids
- Gift card ledger later
- Store credit ledger later
- Cash drawer audit
- Settlement/reconciliation references

### 3. Server state is authoritative

Realtime events are convenience notifications. Tests must confirm that clients can recover correct state from the API/database after reconnect, refresh, missed messages, or app restart.

### 4. Cloud, local, and hybrid modes must share behaviour

Daxa Cloud, Daxa Local, and Daxa Hybrid must use the same domain rules.

Deployment mode can change where data is hosted and how sync works, but it must not create different business logic.

### 5. Multi-location is always active

Single-location customers are simply tenants with one location.

Tests must not assume that a tenant has only one location unless the test explicitly sets that up.

### 6. Device identity and user identity are separate

Tests must verify that:

- Device registration controls terminal behaviour.
- User/staff login controls permissions.
- One does not replace the other.

---

## Required Test Categories

### Core test categories

- Unit tests
- Integration tests
- API contract tests
- Database migration tests
- Authorization tests
- Tenant isolation tests
- Location isolation tests
- Device registration tests
- Product/catalogue tests
- Menu rendering/config tests
- Pricing tests
- Surcharge tests
- Tax calculation tests
- Receipt rendering tests
- Payment adapter tests
- Refund tests
- Order lifecycle tests
- Order snapshot/version tests
- Customer display state tests
- KDS routing tests
- Reconnect/resync tests
- Print queue tests
- Cash drawer audit tests
- Payment split tests
- Gift card ledger tests later
- Store credit ledger tests later
- Stock movement tests
- Sync conflict tests
- Backup/restore tests
- Docker deployment smoke tests
- MAUI terminal smoke tests where practical
- PWA smoke tests where practical

---

## Critical Behaviour To Test

### AU/NZ tax behaviour

Daxa POS launches with AU/NZ tax support first.

Test cases must include:

- AU GST 10% taxable item.
- AU GST-free item.
- Mixed AU basket with taxable and GST-free items.
- NZ GST 15% taxable item.
- NZ zero-rated item.
- NZ exempt category where modelled.
- Tax-inclusive pricing.
- Tax snapshots stored on order lines.
- Tax summary on order.
- Receipt markers such as `F = GST-free`.

Example mixed basket:

```text
Flat white                    $5.50
Chocolate cake slice          $8.80
Loaf of bread              F  $6.00
-----------------------------------
Total                        $20.30
Includes GST                  $1.30

F = GST-free
```

Expected AU GST calculation:

```text
Flat white GST:           $5.50 / 11 = $0.50
Chocolate cake GST:       $8.80 / 11 = $0.80
Loaf of bread GST:        $0.00
Total GST:                $1.30
```

Tests must confirm product names remain product names. Tax treatment must not replace the product name.

Incorrect receipt output:

```text
GST-free item                 $6.00
```

Correct receipt output:

```text
Loaf of bread              F  $6.00
```

### Global tax model

Even if US/CA tax is not MVP, tests should protect the model from being narrowed to one tax rate.

The tax model must support:

- Multiple tax components per order line.
- Tax-inclusive pricing.
- Tax-exclusive pricing.
- Line-level tax snapshots.
- Order-level tax summaries.
- Surcharge taxability.
- Discount tax behaviour.
- Per-line rounding.
- Per-order rounding.

Recommended design limits:

```text
Maximum tax components per item line: 10
Maximum tax components per order: 20
```

### Payments

Payment tests must cover:

- Cash payment.
- Manual external EFTPOS payment.
- Integrated payment request creation.
- Payment adapter interface behaviour.
- Provider request idempotency.
- Provider approved result.
- Provider declined result.
- Provider cancelled result.
- Provider timeout result.
- Payment terminal mapping.
- Payment status polling.
- Split payment balancing.
- Payment overpay prevention.
- Payment underpay handling.
- Linked refunds.
- Partial refunds.
- Refund audit.
- Payment record immutability.

Payment provider tests should use fake providers first.

Provider-specific integrations must have contract tests or integration tests where possible.

### Square Terminal model

Tests must confirm the Square integration model uses Daxa POS as the POS and Square as the terminal/payment provider.

Correct flow:

```text
Daxa POS creates order
↓
Daxa sends amount to Square Terminal API
↓
Square Terminal displays amount
↓
Customer pays
↓
Square returns result
↓
Daxa records payment result
```

The system must not require staff to manually type the amount into Square Terminal when integrated payments are configured.

### Order lifecycle

Tests must cover:

- New order.
- Add item.
- Add modifiers.
- Remove item.
- Void item.
- Apply line discount.
- Apply order discount.
- Apply surcharge.
- Hold order.
- Resume order.
- Pay order.
- Refund order.
- Reprint receipt.
- Audit events.

### Customer display

Tests must cover the state model used by Daxa Display.

Customer display states:

- Idle
- Order building
- Discount applied
- Surcharge applied
- Payment started
- Payment approved
- Payment declined
- Receipt available
- Loyalty prompt later

Tests should confirm that the customer display is driven by order/payment state, not by a separate inconsistent order model.

### KDS reconnect

KDS screens are separate devices and must rebuild state after reconnect.

Tests must cover:

- Missed realtime messages do not corrupt state.
- KDS reloads full current tickets after reconnect.
- KDS ticket status remains correct after refresh.
- Void/cancel updates are visible after reconnect.
- Bump/complete state survives refresh.
- Routing rules produce correct station tickets.

### POS reconnect

Daxa Terminal must recover order state.

Tests must cover:

- POS refresh during an active order.
- POS reconnect after network interruption.
- POS reconnect after missed SignalR/WebSocket events.
- POS reload from local/cloud server state.
- Duplicate payment prevention after reconnect.
- Idempotency key reuse behaviour.

### Order routing

Routing tests must cover:

- Coffee item routes to coffee station.
- Drinks route to bar station.
- Food routes to kitchen station.
- Burger meal splits to burger, fryer/chips, and drinks stations where configured.
- Modifiers display correctly on station tickets.
- Sent-to-kitchen state is separate from saved draft/order state.
- A routed item can target multiple stations if configured.
- Void/cancel is propagated to routed stations.

### Financial integrity

Tests must cover:

- Split payments balance correctly.
- Refunds create correct reversal/ledger records.
- Voids create audit records.
- Discounts require correct permission where configured.
- Price overrides require correct permission where configured.
- Cash drawer opens are audited.
- Gift card redemptions cannot overdraft unless explicitly allowed later.
- Store credit cannot overdraft unless explicitly allowed later.
- Payment records are not silently edited.

### Stock integrity

Tests must cover:

- Sales create stock consumption movements where configured.
- Adjustments are recorded.
- Waste/spoilage is recorded.
- Stocktake corrections are auditable.
- Sold-out state disables product sale where configured.
- Daily production counts can decrement for bakery/food truck workflows.
- Stock deduction must be idempotent when order submission is retried.

### Sync and offline

Daxa Local and Daxa Hybrid need sync tests.

Tests must cover:

- Local order created while cloud is unavailable.
- Local payment record queued for sync.
- Local audit log queued for sync.
- Local stock movement queued for sync.
- Cloud configuration pushed to local.
- Local-to-cloud retry.
- Cloud-to-local retry.
- Idempotency prevents duplicates.
- Conflict detection is explicit.
- Sync status is visible.
- Failed sync is auditable.
- Local trading continues when cloud is unavailable, where configured.

### Backup/restore

Tests must cover:

- Local database backup can be created.
- Backup metadata is recorded.
- Restore procedure is documented and tested.
- Restore to replacement local server is possible.
- Cloud backup export is authenticated.
- Backup failure is visible and auditable.

### Docker and deployment smoke tests

Docker smoke tests should cover:

- API container starts.
- Database container starts.
- Worker container starts where applicable.
- Reverse proxy starts where applicable.
- Health endpoint responds.
- Database migration applies.
- Basic auth/login works in dev/test mode.
- API can connect to database.
- Logs are accessible.
- Volumes persist data across container restart.

### MAUI terminal tests

Where practical, test:

- Application startup.
- Full-screen/borderless mode configuration.
- Terminal registration.
- Current venue/location/terminal resolution.
- Customer display window state publishing.
- Barcode scanner input as keyboard wedge.
- Payment terminal assignment.
- Printer assignment.

Manual test notes are acceptable for UI/device-specific behaviours that are not easily automated, but they must be documented.

---

## Test Data Requirements

Use seeded test data for:

- Tenant
- Organisation
- Region
- Country
- Venue/location
- Terminal
- User roles
- Staff PIN
- Products
- Tax categories
- Modifiers
- Prices
- Surcharges
- Payment methods
- Printers
- Payment terminals
- Customers later
- Stock items later

Minimum AU test dataset:

```text
Country: AU
Currency: AUD
Tax mode: GST-inclusive
Tax categories:
- AU_GST_10
- AU_GST_FREE

Products:
- Flat white, $5.50, AU_GST_10
- Chocolate cake slice, $8.80, AU_GST_10
- Loaf of bread, $6.00, AU_GST_FREE
```

Minimum NZ test dataset:

```text
Country: NZ
Currency: NZD
Tax mode: GST-inclusive
Tax categories:
- NZ_GST_15
- NZ_ZERO_RATED
- NZ_EXEMPT
```

---

## Test Naming

Use descriptive test names.

Good:

```text
Calculates_AuGstInclusiveMixedBasket_WithGstFreeReceiptMarker
Rejects_PaymentCompletion_When_IdempotencyKeyAlreadyUsed
Rebuilds_KdsTickets_After_Reconnect
Prevents_CrossLocationOrderAccess
Stores_TaxSnapshot_On_OrderLine
```

Bad:

```text
Test1
PaymentWorks
TaxTest
```

---

## Claude Code Test Rule

Every implementation change must include relevant tests or clearly state why tests were not added.

If tests are missing because test infrastructure does not exist yet, create or update an open issue and document the gap.

Financial, tax, payment, refund, and audit changes must not be merged without tests.

---

## Implementation Status (PLAN-0003, as of Milestone G, 2026-07-03)

Two test projects exist: `tests/DaxaPos.UnitTests/` (pure logic — hashers, policies, plus the `IgnoreQueryFilters()` source-scan guard) and `tests/DaxaPos.Api.Tests/` (HTTP-level tests against a real Postgres container; no mocks). Offline verification is structural: CI runs with a Postgres service only (no Keycloak container exists in the workflow), local runs keep the `keycloak` compose service stopped, and `HybridOfflineLoginTests.cs` exercises both auth chains end-to-end under those conditions. Cross-cutting authorization coverage is consolidated in `RbacTests.cs`, driven by a single protected-endpoint inventory — add new protected endpoints to that inventory as they are built. See [Security Tests](security-tests.md) for the detailed mapping and the [Local smoke test](local-smoke-test.md) for the manual walkthrough.
