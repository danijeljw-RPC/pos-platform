namespace DaxaPos.Application.Identity;

/// <summary>
/// Staff login-identifier format policy (PLAN-0003 Milestone F, Decision 3): a human-enterable
/// code — uppercase letters and digits, 2–20 characters — unique per organisation, never the
/// database primary key. Codes are normalised to uppercase before storing and comparing, so
/// venues can use codes like <c>DJ</c>, <c>MGR1</c>, <c>BAR01</c>, or plain numbers.
/// </summary>
public static class StaffCodePolicy
{
    public const int MinLength = 2;

    public const int MaxLength = 20;

    public static string Normalize(string staffCode) => staffCode.Trim().ToUpperInvariant();

    public static bool IsValid(string staffCode)
    {
        var normalized = Normalize(staffCode);

        return normalized.Length >= MinLength
            && normalized.Length <= MaxLength
            && normalized.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9');
    }
}
