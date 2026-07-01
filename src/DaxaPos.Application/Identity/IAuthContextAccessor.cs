namespace DaxaPos.Application.Identity;

/// <summary>
/// Exposes the <see cref="AuthContext"/> established for the current request, if any.
/// </summary>
public interface IAuthContextAccessor
{
    AuthContext? Current { get; }
}
