namespace DaxaPos.Application.Identity;

/// <summary>
/// POS/local <c>AuthSession</c> expiry policy, per the accepted PLAN-0003 defaults: a session is
/// invalid once either the 12-hour absolute lifetime since issuance, or the 8-hour idle timeout
/// since last activity, has elapsed — whichever comes first.
/// </summary>
public static class SessionExpiryPolicy
{
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(12);

    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(8);

    public static bool IsExpired(DateTimeOffset issuedAtUtc, DateTimeOffset lastActivityAtUtc, DateTimeOffset nowUtc) =>
        nowUtc - issuedAtUtc > AbsoluteLifetime || nowUtc - lastActivityAtUtc > IdleTimeout;
}
