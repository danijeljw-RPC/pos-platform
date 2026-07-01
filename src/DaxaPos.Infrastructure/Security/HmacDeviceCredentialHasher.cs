using System.Security.Cryptography;
using DaxaPos.Application.Identity;

namespace DaxaPos.Infrastructure.Security;

/// <summary>
/// Hashes device credential secrets (ADR-0008 / ADR-0015) using salted HMAC-SHA256. Unlike
/// <see cref="Pbkdf2PinHasher"/>, these secrets are high-entropy, server-generated random values
/// rather than human-chosen PINs, so a fast keyed hash is appropriate — there is no realistic
/// offline brute-force target, only the need to avoid ever persisting the raw secret.
/// </summary>
public sealed class HmacDeviceCredentialHasher : IDeviceCredentialHasher
{
    private const int SaltSizeBytes = 16;

    public string Hash(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = HMACSHA256.HashData(salt, System.Text.Encoding.UTF8.GetBytes(secret));

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string secret, string hash)
    {
        var parts = hash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);

        var actualHash = HMACSHA256.HashData(salt, System.Text.Encoding.UTF8.GetBytes(secret));

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
