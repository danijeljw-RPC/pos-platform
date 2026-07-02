using System.Security.Cryptography;

namespace DaxaPos.Application.Identity;

/// <summary>
/// Staff PIN format policy (PLAN-0003 Milestone F, Decision 3): digits only, 4–10 characters,
/// hashed via <see cref="IPinHasher"/>, never stored raw. PIN resets are server-generated
/// (Decision 10) — <see cref="GeneratePin"/> uses a cryptographic RNG so admins never supply
/// credential material in a request body.
/// </summary>
public static class StaffPinPolicy
{
    public const int MinLength = 4;

    public const int MaxLength = 10;

    public const int GeneratedPinLength = 6;

    public static bool IsValid(string pin) =>
        pin.Length >= MinLength
        && pin.Length <= MaxLength
        && pin.All(c => c is >= '0' and <= '9');

    public static string GeneratePin() =>
        string.Create(GeneratedPinLength, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }
        });
}
