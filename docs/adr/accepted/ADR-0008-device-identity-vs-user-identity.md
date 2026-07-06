# ADR-0008 — Device Identity vs User Identity

## Status

Accepted

## Context

A POS terminal has a fixed hardware identity (it is registered to a venue, assigned a terminal role, printer mapping, and payment terminal mapping). The staff member using the terminal changes throughout the day — staff clock on and off, cover each other's breaks, and share terminals.

If device and user identity are conflated, the system cannot correctly attribute orders, voids, refunds, and audit events to the right person. Equally, terminal configuration (printer routing, payment terminal mapping, display assignment) would be disrupted by staff login/logout.

## Decision

Device identity and user identity are **separate concerns** in Daxa POS.

- A `Device` is registered to a `Location` and has a `TerminalId`, `DeviceType`, `PrinterMapping`, `PaymentTerminalMapping`, and `DisplayConfig`.
- A `User` logs into the device (via PIN or other credential) to start a session.
- The `Device` retains its configuration regardless of who is logged in.
- Audit events, orders, and payments capture both the `DeviceId`/`TerminalId` and the `UserId`.
- Staff PIN login allows fast user switching without requiring a full app restart or device reconfiguration.

## Consequences

**Positive:**

- Terminal configuration is stable regardless of staff changes.
- Audit trail captures both device and user context.
- Fast staff handover via PIN without disrupting device state.
- Correct attribution of orders, voids, refunds, and overrides to users.

**Negative:**

- Two identity concepts must be managed (device registration + user login).
- Device registration flow adds onboarding steps.

## Alternatives Considered

1. **Combined device+user identity** — Rejected. Cannot correctly attribute actions or maintain stable device configuration across staff sessions.
2. **No device identity, user-only** — Rejected. Device-specific configuration (printers, payment terminals) cannot be maintained per-user.

## Open Questions

- How will device registration tokens be issued and rotated?
- Should device registration require a human admin approval step?

## Related Documents

- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) (supersedes ADR-0009)
- [Architecture: Device Strategy](../../architecture/device-strategy.md)
- [Module: Devices](../../modules/devices.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)

---

## Acceptance Addendum

ADR-0008 is accepted.

Device identity and user identity remain separate concerns.

A device represents the registered terminal, screen, kiosk, tablet, browser, or installed application instance. A user represents the staff member or admin currently using that device.

The device must retain its own identity and configuration independently of staff login/logout.

## Resolution of Open Questions

### How will device registration tokens be issued and rotated?

Device registration is server-issued.

A device must not invent its own trusted identity. The local server PWA application or cloud PWA server application issues the device identity during registration.

There are two supported device registration modes:

1. **Installed/local application device registration**
2. **Browser/PWA device registration**

Both flows use the same core model:

- The device opens the local or cloud Daxa POS URL.
- The device requests registration from the server.
- The server requires a valid registration PIN.
- The device provides a device type and optional friendly name.
- The server creates or approves the device record.
- The server issues a device identifier and registration credential.
- The device stores its issued identity locally.
- Future requests include the device identity so the server can load terminal configuration.

The registered device identity is scoped to:

- Client
- Location
- DeviceId
- TerminalId, where applicable
- DeviceType
- Registration status

The device identity must not replace user login. Staff still log in separately using PIN or other user credentials.

## Installed/Local Application Devices

For installed local applications or service-backed local clients, the issued device registration data may be stored in a local configuration file on the device.

The application reads this configuration file when it starts.

The file should contain only the values needed for the application to identify itself to the server.

Suggested stored values:

```text
ClientId
LocationId
DeviceId
TerminalId
DeviceType
ServerBaseUrl
DeviceCredentialReference
RegisteredAtUtc
LastRotatedAtUtc
```

Secrets should not be stored as plain text where avoidable.

Where the platform supports it, the device credential should be protected using the operating system credential store or an encrypted local secret store. The config file may store a reference to the credential rather than the credential itself.

Examples:

- Windows Credential Manager
- macOS Keychain
- Linux Secret Service or protected file permissions
- Container or service-level secret mount

If a local installation must use a plain config file during early MVP development, that should be treated as a temporary implementation detail and replaced before production hardening.

## Browser/PWA Devices

For iPad, iPhone, Android tablet, desktop browser, and other PWA-based devices, the device identity is created the first time the device opens the local or cloud PWA URL and completes registration.

The PWA registration flow should be:

1. Device opens the local or cloud PWA URL.
2. Server detects that no valid device identity exists.
3. Server displays the device registration screen.
4. Staff/admin enters the current 6-digit registration PIN.
5. Staff/admin selects the device type.
6. Staff/admin enters an optional friendly device name.
7. Server registers the device against the selected client/location.
8. Server issues a device identity and browser-bound credential.
9. PWA stores the device identity using browser storage suitable for the application.
10. Server marks the device as registered and loads its terminal configuration.

The browser/PWA device identity may use a secure, HTTP-only cookie for the server session/device binding and IndexedDB/local storage for non-secret local metadata.

Secret material should not be stored in plain local storage where avoidable.

The preferred model is:

- Store non-secret device metadata in IndexedDB or local storage.
- Store server-issued session/device binding in a secure, HTTP-only, same-site cookie where possible.
- Allow the server to revoke and rotate the device credential.
- Treat browser data loss as requiring re-registration.

## Registration PIN Model

Device registration should use a server-generated or admin-configured 6-digit registration PIN.

The PIN is not the device identity.

The PIN is a short-lived enrolment secret that allows a device to request registration.

The PIN should be configured in the admin portal for a client/location.

Recommended behaviour:

- PINs are scoped to a client and location.
- PINs expire after a short period.
- PINs can be rotated manually by an admin.
- PIN attempts are rate-limited.
- Failed attempts are audit logged.
- Successful registrations are audit logged.
- PINs are not reused as ongoing authentication credentials.

The PIN proves that the person registering the device has access to the admin-controlled enrolment code. After registration, the device uses its issued device credential, not the PIN.

## Device Type Capture

During registration, the server should ask for the device type.

Examples:

- POS Terminal
- KDS Screen
- Customer Display
- Admin Workstation
- Self-Service Kiosk
- Mobile Ordering Device
- Printer Controller
- Local Service Host

The selected device type controls which configuration screens and runtime features apply to the device.

For example:

- A POS Terminal may need printer routing and payment terminal mapping.
- A KDS Screen may need station assignment.
- A Customer Display may need pairing to a POS terminal.
- A Printer Controller may need printer queue configuration.

## Rotation Model

Device credentials must support rotation.

Rotation should be available in the admin portal and may also occur automatically.

Recommended rotation behaviour:

- Admin can rotate a single device credential.
- Admin can revoke a device immediately.
- Admin can force all devices at a location to re-register.
- Server can rotate a credential after suspicious activity.
- Server can rotate credentials during major security events.
- Device credential rotation should not require changing staff user credentials.

When a credential is rotated:

1. Existing device credential is marked retired or revoked.
2. Server issues a replacement credential.
3. Device stores the replacement credential.
4. Audit log records who or what triggered the rotation.
5. Old credential no longer grants device access.

If a device loses its local credential, it should be treated as a new registration and must complete the PIN-based enrolment flow again.

## Should device registration require a human admin approval step?

A separate manual approval queue is not required for the default device registration flow.

The 6-digit registration PIN acts as the human-controlled approval step.

An admin or manager with device-management permission generates or configures the registration PIN in the admin portal. A staff member physically registering the device must enter that PIN to connect the device to the service.

This avoids unnecessary friction during venue setup while still preventing random devices from registering without an enrolment secret.

However, the system should support optional stricter controls later.

Optional future controls may include:

- Require admin approval after PIN entry.
- Restrict registration by local network/IP range.
- Restrict registration to known device serial numbers.
- Require location manager confirmation.
- Require cloud admin approval for sensitive device types.
- Limit how many devices can be registered with a single PIN.

For MVP and normal hospitality deployment, PIN-based registration is accepted as the approval mechanism.

## Accepted Device Registration Flow

The accepted registration flow is:

1. Admin opens the admin portal for the client/location.
2. Admin creates or views a short-lived 6-digit device registration PIN.
3. Device opens the local or cloud Daxa POS PWA URL.
4. Device registration screen appears if the device is unknown.
5. Staff/admin enters the registration PIN.
6. Device type is selected.
7. Optional friendly name is entered.
8. Server validates the PIN against the client/location.
9. Server creates the device record.
10. Server issues the device identity and credential.
11. Device stores its issued identity locally.
12. Server loads the correct device/location configuration.
13. Staff user login remains separate from the device registration flow.

## Multi-Location Rules

Daxa POS is multi-location by default.

Device registration must always be scoped to a client and location.

A device registered to one location must not automatically gain access to another location.

If a device is moved to another location, an admin should either:

- Reassign the device in the admin portal, or
- Revoke the existing registration and register it again at the new location.

The device identity should make it clear which location the device belongs to.

This prevents devices from accidentally loading the wrong menu, printer routing, payment terminal mapping, tax configuration, or reporting context.

## Security Rules

Device registration must follow these rules:

- Device identity is issued by the server.
- Device identity is separate from user identity.
- A registration PIN is required before a new device can connect.
- Registration PINs are scoped to client/location.
- Registration PINs are short-lived or manually rotated.
- Device credentials can be revoked.
- Device credentials can be rotated.
- Staff login is still required for staff actions.
- Audit events capture both `DeviceId` and `UserId` where a user is logged in.
- Anonymous device activity before user login should still capture `DeviceId` where available.

## Audit Requirements

The system must audit device registration lifecycle events.

Audit events should include:

- Device registration requested.
- Device registration succeeded.
- Device registration failed.
- Invalid PIN entered.
- PIN generated.
- PIN rotated.
- Device credential rotated.
- Device revoked.
- Device reassigned to another terminal or location.
- Device type changed.
- Device friendly name changed.

Audit records should capture:

- ClientId
- LocationId
- DeviceId, where known
- TerminalId, where applicable
- UserId, where a user/admin performed the action
- Timestamp
- Source IP or local network identifier where available
- Old value and new value for configuration changes

## Consequence

This gives Daxa POS a practical and secure registration model without overcomplicating venue setup.

The system supports:

- Stable device configuration.
- Separate staff login.
- Fast PWA onboarding.
- Local server and cloud server registration.
- Multi-location scoping from day 0.
- Device revocation and credential rotation.
- Future stricter approval workflows if needed.

## Status Update

Status: **Accepted**
