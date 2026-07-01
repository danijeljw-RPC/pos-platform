using System.Security.Cryptography;
using DaxaPos.Application.Identity;

namespace DaxaPos.Infrastructure.Security;

/// <summary>
/// Generates opaque, server-side POS/local session bearer tokens and hashes them for storage and
/// lookup (ADR-0015). Not a JWT — see <see cref="ISessionTokenService"/> remarks.
/// </summary>
public sealed class RandomSessionTokenService : ISessionTokenService
{
    private const int TokenSizeBytes = 32;

    public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes));

    public string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
