namespace DaxaPos.Application.Identity;

/// <summary>
/// Generates and hashes opaque POS/local session bearer tokens (ADR-0015). These are
/// server-generated random values, not JWTs — <see cref="GenerateToken"/> produces the value
/// returned to the caller once at login, and <see cref="Hash"/> produces the value persisted on
/// <c>AuthSession.SessionTokenHash</c> and recomputed from a presented bearer token to look up the
/// session by hash. The raw token itself must never be persisted.
/// </summary>
/// <remarks>
/// Unlike <see cref="IPinHasher"/>, <see cref="Hash"/> is deterministic (no per-call random salt):
/// the token already carries 256 bits of server-generated entropy, and a session lookup needs to
/// recompute the same hash from the presented token to find the matching row.
/// </remarks>
public interface ISessionTokenService
{
    string GenerateToken();

    string Hash(string token);
}
