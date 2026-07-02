# Module: Device and Terminal Service

The device service manages device registration, terminal assignment, and hardware configuration.

See also: `docs/modules/15-offline-device-admin-product-management.md`.

---

## Responsibilities

- Device registration (first-time setup).
- Terminal type assignment (POS, KDS, display, admin).
- Printer mapping per terminal.
- Payment terminal mapping per terminal.
- Display configuration per terminal.
- Device health status.
- App version tracking.
- Remote configuration delivery.
- Device deregistration.

## Device vs User Identity

Device identity is separate from user identity. A device is registered once and retains its configuration regardless of which staff member is logged in.

See [ADR-0008](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md).

## Related Plans

- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)

## Implementation status (PLAN-0003 Milestone E, 2026-07-02)

Device registration PINs and device credentials are implemented per ADR-0008's accepted flow:

- `POST /api/v1/device-registration-pins` (`devices.register`) issues a 6-digit enrolment PIN scoped to one of the caller's organisation's locations — 15-minute expiry, single-use by default (`MaxUses` 1–10), stored hashed, raw value returned once. `POST /api/v1/device-registration-pins/{pinId}/revoke` kills an accidentally issued PIN before expiry.
- `POST /api/v1/device-registration` (anonymous, rate-limited 10/minute per remote IP) validates the PIN, creates the `Device` + an `Active` `DeviceCredential`, and returns the device identity fields plus the raw device token **once**. Tenant/organisation/location derive exclusively from the matched PIN row.
- The device authenticates with `Authorization: Device {credentialId}.{secret}` — validated by `DeviceTokenAuthenticationHandler`, which builds a partial `AuthContext` with **empty roles/permissions**. A device token is trusted device context only; it never grants user permissions (ADR-0013). Staff PIN login (Milestone F) will require this context plus staff credentials.
- `POST /api/v1/devices/{deviceId}/rotate-credential` retires the old credential immediately and returns a new secret once; `POST /api/v1/devices/{deviceId}/revoke` revokes all of the device's credentials (terminal — a revoked/lost device re-registers as a **new** `Device`; there is no `Device.IsActive`, revocation lives on `DeviceCredential.Status`). `GET /api/v1/devices?locationId=` lists the caller's organisation's devices with a credential-status flag.
- All lifecycle actions are audited (`DeviceRegistrationPinCreated`/`Revoked`, `DeviceRegistered`, `DeviceRegistrationFailed`, `DeviceCredentialRotated`, `DeviceRevoked`). An **unknown**-PIN attempt writes no audit row — there is no tenant for the non-nullable `AuditEvent.TenantId`; rate limiting covers that abuse path, and a tenant-less global security-event store is a flagged future need.

Not yet implemented from the responsibilities above: terminal assignment/pairing, printer and payment-terminal mapping, display configuration, health/version tracking, remote configuration — later plans.

## Implementation status (PLAN-0003 Milestone H closeout, 2026-07-03)

PLAN-0003 is complete for device identity: registration-PIN issuance/revocation, PIN-gated device registration, device-token authentication with a zero-permission partial `AuthContext`, and credential rotation/revocation are implemented, tested (`DeviceRegistrationPinEndpointsTests.cs`, `DeviceRegistrationTests.cs`, `DeviceEndpointsTests.cs`, `DeviceRegistrationRateLimitTests.cs`), and verified with Keycloak stopped. Two known gaps are tracked as open issues rather than silently missing: registration-PIN `MaxUses` is not concurrency-safe under simultaneous registration attempts ([OI-0013](../issues/open/OI-0013-device-registration-pin-maxuses-concurrency.md)), and device authentication does not check whether a parent `Organisation`/`Location`/`Terminal` has been deactivated ([OI-0012](../issues/open/OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md)). Terminal assignment/pairing and the other not-yet-implemented items above remain later-plan work, not PLAN-0003 scope.
