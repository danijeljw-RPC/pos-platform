using System.Security.Cryptography;
using DaxaPos.Application.Identity;

namespace DaxaPos.Infrastructure.Security;

/// <summary>
/// Hashes staff PINs and local passwords using PBKDF2-SHA256 (ADR-0015 / PLAN-0003).
/// Low-entropy credentials (4+ digit PINs) rely on the iteration count for brute-force
/// resistance, not on secrecy of the algorithm — a random salt per credential still defeats
/// precomputed/rainbow-table attacks and ensures identical PINs never produce identical hashes.
/// </summary>
public sealed class Pbkdf2PinHasher : IPinHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 210_000;

    public string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string pin, string hash)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
