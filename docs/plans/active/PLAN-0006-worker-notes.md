# PLAN-0006 Worker Notes

## Status

Draft planning notes revised after human review.

No implementation has started.

## Purpose Of This Revision

The first PLAN-0006 planning pass was MAUI-first. The current human decision is PWA-first.

This revision records:

- Blazor/PWA first.
- Blazor only for PWA surfaces.
- MAUI deferred.
- OI-0018 production-routing decisions.
- PLAN-0005 historical cleanup.

## Human Decisions Recorded

### 1. PWA First

PLAN-0006 must start with Blazor Server / Blazor PWA.

The PWA can act as a terminal on supported devices. MAUI remains part of the future product
direction, but it is not the starting point for PLAN-0006.

### 2. Blazor Only

Back Office, Terminal, Display, and KDS PWA surfaces use Blazor.

Out of scope:

- React.
- Vue.
- Angular.
- Any non-.NET frontend stack unless a future ADR explicitly changes this.

### 3. MAUI Deferred

MAUI is deferred to a future dedicated Windows terminal plan.

MAUI is still relevant for:

- Dedicated installed Windows terminal software.
- Local Windows hardware integration.
- Local USB/Windows printer support.
- Future terminal-specific installer/update mechanics.

It is not part of PLAN-0006 Milestone A.

### 4. Printer Routing Decisions

OI-0018 stays open.

Recorded decisions:

- Production route codes are strings, not enums.
- Route codes must be slug-like.
- No spaces.
- Lowercase letters/numbers plus hyphen or underscore.
- Recommended pattern: `^[a-z0-9][a-z0-9_-]{0,63}$`.
- For standard products, store production route on the product.
- If no route exists, the item does not print anywhere.
- Combo/grouped products require production components.
- Customer receipts and production dockets are separate document types.

### 5. Printing Ownership

Server owns:

- Server-controlled network receipt printing.
- Worker/outbox print processing.
- Future network production docket printing.

Future MAUI terminal owns:

- Local USB printers physically attached to the terminal.
- Locally installed Windows printers only visible to that terminal.

### 6. PLAN-0005 Historical Cleanup

PLAN-0005 is complete. Its old planning sections must be treated as historical, not current open
work.

Final PLAN-0005 closeout state:

- 1052/1052 tests passing.
- 17 migrations verified clean from empty.
- OI-0017 remains open.
- OI-0018 created during closeout.
- `printing.manage` reserved but unimplemented.

## Revised PLAN-0006 Milestone Shape

| Milestone | Scope |
|-----------|-------|
| A | Blazor/PWA shell, auth/session/device context, Staff PIN login. |
| B | Back Office PWA skeleton and device PIN generation/viewing. |
| C | POS Terminal PWA sales screen. |
| D | Payment and receipt flow in PWA. |
| E | Customer display / display mode in PWA. |
| F | Minimal KDS PWA board. |
| G | Consolidation, RBAC/UX sweep, documentation closeout. |

## Open Items Before Implementation

None block Milestone A if the implementation stays PWA-first.

Still open for later:

- Whether to show a disabled Card button in Milestone D.
- Whether to add refund UI later.
- Whether the minimal KDS board needs a narrow read endpoint.
- When to schedule OI-0018 implementation.
- When to schedule future MAUI terminal work.

## Recommended Next Session

Start PLAN-0006 Milestone A only.

Scope:

- Blazor/PWA shell.
- API client foundation.
- Authentication/session handling.
- Device context handling.
- Staff PIN login.
- Basic 401/403 UX.

Do not:

- Start MAUI.
- Start payments UI.
- Start customer display.
- Start KDS.
- Implement OI-0018.
- Start PLAN-0009.
- Add migrations unless a direct blocker is found.
