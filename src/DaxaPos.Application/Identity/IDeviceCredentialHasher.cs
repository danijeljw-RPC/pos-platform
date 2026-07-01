namespace DaxaPos.Application.Identity;

/// <summary>
/// Hashes and verifies high-entropy device credential secrets (ADR-0008). Unlike
/// <see cref="IPinHasher"/>, these secrets are server-generated random values, not human-chosen
/// low-entropy PINs, so a fast salted hash is appropriate rather than a slow, brute-force-resistant
/// one. Raw secrets must never be persisted — only the value returned from <see cref="Hash"/>.
/// </summary>
public interface IDeviceCredentialHasher
{
    string Hash(string secret);

    bool Verify(string secret, string hash);
}
