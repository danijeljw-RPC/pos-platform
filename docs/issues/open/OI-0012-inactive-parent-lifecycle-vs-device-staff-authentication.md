# OI-0012 — Inactive Parent Lifecycle vs Device/Staff Authentication

## Status

Open

## Area

Identity / Security / Devices

## Summary

Deactivating an `Organisation`, `Location`, or `Terminal` (`IsActive = false`) does not affect authentication that hangs off it: a deactivated location's registered devices keep authenticating with their device tokens, and its staff members can still log in with staff code + PIN. What an inactive parent should mean for its children has never been decided.

## Context

Milestone D added `IsActive` with deliberately non-cascading semantics ("do not add complex lifecycle rules yet" — explicit human instruction). Milestone E recorded the consequence as an accepted deferred risk: `DeviceTokenAuthenticationHandler` validates only that the credential is `Active`, not the parent lifecycle state. Milestone F's staff PIN login likewise checks `StaffMember.IsActive` but not the location's or organisation's flag. Immediate cut-off paths exist per-identity (`devices/{id}/revoke`, `staff-members/{id}/disable`) — just not per-parent.

## Impact

- An operator who deactivates a location expecting it to stop trading has not actually cut off its devices or staff logins.
- Whatever rule is chosen affects `DeviceTokenAuthenticationHandler`, `SessionAuthenticationHandler`, staff PIN login, and possibly session revocation semantics (does deactivating a location revoke its live sessions?).
- Later modules (orders, payments) will inherit whatever meaning "inactive location" has by then — deciding late multiplies the touch points.

## Options

1. **Auth-time check** — device-token and staff-PIN authentication also require the resolved `Location`/`Organisation` to be active (fail closed, cheap: rows are already loaded during authentication). Existing sessions die at next validation or via explicit revocation on deactivate.
2. **Cascade on deactivate** — deactivating a parent actively revokes child credentials/sessions (heavier write path, more audit rows, harder to reverse on reactivate).
3. **Status quo** — inactive is reporting/visibility metadata only; operators must revoke devices/disable staff explicitly.

## Recommendation

Option 1, decided in the same future milestone that defines inactive-parent semantics generally (already flagged in the Milestone D planning pass). It matches the codebase's fail-closed posture without the cascade's write complexity.

## Decision Needed

- What an inactive `Organisation`/`Location`/`Terminal` means for device authentication, staff login, and live sessions.
- Whether deactivation revokes existing sessions immediately or lets them lapse at next validation.

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](../../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)
- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](../../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md)
- [PLAN-0003 worker notes — Milestone E report, deferred risks](../../plans/active/PLAN-0003-worker-notes.md)
