namespace DaxaPos.Application.Identity;

/// <summary>
/// Device registration PIN policy (ADR-0008, PLAN-0003 Milestone E approved defaults): a 6-digit
/// enrolment PIN valid for 15 minutes, single-use by default (an admin may allow up to 10 uses),
/// with the pre-auth registration endpoint rate-limited to 10 attempts per minute per remote IP.
/// The PIN is a short-lived enrolment secret, never an ongoing device credential.
/// </summary>
public static class DeviceRegistrationPinPolicy
{
    public const int PinLength = 6;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public const int DefaultMaxUses = 1;

    public const int MinMaxUses = 1;

    public const int MaxMaxUses = 10;

    public const int RegistrationRateLimitPermitLimit = 10;

    public static readonly TimeSpan RegistrationRateLimitWindow = TimeSpan.FromMinutes(1);

    public static bool IsValidMaxUses(int maxUses) => maxUses is >= MinMaxUses and <= MaxMaxUses;

    /// <summary>
    /// A PIN is usable (live) only while it is unrevoked, unexpired, and has uses remaining.
    /// </summary>
    public static bool IsUsable(DateTimeOffset expiresAtUtc, DateTimeOffset? revokedAtUtc, int usedCount, int maxUses, DateTimeOffset nowUtc) =>
        revokedAtUtc is null && expiresAtUtc > nowUtc && usedCount < maxUses;
}
