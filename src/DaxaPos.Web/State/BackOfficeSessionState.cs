namespace DaxaPos.Web.State;

/// <summary>
/// Locally persisted Back Office session established by
/// <c>POST /api/v1/auth/local/login</c> (ADR-0013 local admin portal login). Deliberately a
/// separate type from <see cref="SessionState"/> (Milestone A's Staff PIN session) — ADR-0015 §2
/// says the two "are never interchangeable, never share a token format," and every Back Office
/// endpoint (device-registration-pins, devices, locations, catalog, menus) is gated
/// <c>rejectStaffPin: true</c>, so a Staff PIN session could never carry the required permissions
/// anyway. <see cref="Email"/> is stored only for display ("Signed in as…") — it is client-supplied
/// at login, not returned by the server, and is never used for authorization.
/// </summary>
public sealed record BackOfficeSessionState(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
}
