namespace DaxaPos.Web.Api;

// Client-side mirrors of the DaxaPos.Api Identity endpoint contracts (PLAN-0003). Kept local to
// DaxaPos.Web rather than shared via project reference: the PWA only ever talks to the API over
// HTTP/JSON, never in-process, so there is no compile-time coupling to gain from sharing the
// server's record types.

public sealed record DeviceRegistrationRequest(string Pin, string DeviceType, string? Name);

public sealed record DeviceRegistrationResult(
    Guid DeviceId,
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    string DeviceType,
    string Name,
    string DeviceToken);

public sealed record StaffPinLoginRequest(Guid LocationId, string StaffCode, string Pin);

public sealed record StaffPinLoginResult(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    Guid StaffMemberId,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AuthContextResult(
    Guid TenantId,
    Guid? OrganisationId,
    Guid? LocationId,
    Guid? TerminalId,
    Guid? UserId,
    Guid? StaffMemberId,
    Guid? DeviceId,
    string AuthMethod,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
