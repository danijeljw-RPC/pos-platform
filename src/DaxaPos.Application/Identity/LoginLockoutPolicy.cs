namespace DaxaPos.Application.Identity;

/// <summary>
/// Failed-login lockout policy shared by local username/password login and (from Milestone F)
/// staff PIN login, per the accepted PLAN-0003 defaults: lock out after 5 consecutive failures
/// for 15 minutes.
/// </summary>
public static class LoginLockoutPolicy
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static bool ShouldLockOut(int failedAttemptCount) => failedAttemptCount >= MaxFailedAttempts;
}
