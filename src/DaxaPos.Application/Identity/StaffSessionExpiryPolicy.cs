namespace DaxaPos.Application.Identity;

/// <summary>
/// Expiry policy for <c>AuthMethod.LocalStaffPin</c> sessions (PLAN-0003 Milestone F,
/// Decision 6): staff PIN sessions are operational POS sessions and are deliberately
/// shorter-lived than back-office/admin sessions (<see cref="SessionExpiryPolicy"/>, 12h/8h) —
/// invalid once either the 8-hour absolute lifetime since issuance, or the 30-minute idle
/// timeout since last activity, has elapsed.
/// </summary>
public static class StaffSessionExpiryPolicy
{
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(8);

    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

    public static bool IsExpired(DateTimeOffset issuedAtUtc, DateTimeOffset lastActivityAtUtc, DateTimeOffset nowUtc) =>
        nowUtc - issuedAtUtc > AbsoluteLifetime || nowUtc - lastActivityAtUtc > IdleTimeout;
}
