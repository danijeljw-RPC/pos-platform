namespace DaxaPos.Web.State;

/// <summary>
/// Locally persisted staff-PIN session established by PLAN-0003's
/// <c>POST /api/v1/auth/staff-pin/login</c>. This is a client-side cache of the server's own
/// expiry decision, not a re-implementation of <c>StaffSessionExpiryPolicy</c> — the server
/// remains authoritative and any request can still come back 401 before <see cref="ExpiresAtUtc"/>
/// (e.g. idle timeout, revocation).
/// </summary>
public sealed record SessionState(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    Guid StaffMemberId,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid? TerminalId = null)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
}
