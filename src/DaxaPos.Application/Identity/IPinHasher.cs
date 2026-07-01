namespace DaxaPos.Application.Identity;

/// <summary>
/// Hashes and verifies low-entropy numeric credentials (staff PINs, local passwords) for storage.
/// Raw PINs/passwords must never be persisted — only the value returned from <see cref="Hash"/>.
/// </summary>
public interface IPinHasher
{
    string Hash(string pin);

    bool Verify(string pin, string hash);
}
