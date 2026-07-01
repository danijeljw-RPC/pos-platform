using DaxaPos.Application.Identity;
using Microsoft.AspNetCore.Http;

namespace DaxaPos.Infrastructure.Identity;

/// <summary>
/// Reads the <see cref="AuthContext"/> stashed by Api's authentication handlers into
/// <c>HttpContext.Items</c> for the current request.
/// </summary>
public sealed class HttpContextAuthContextAccessor(IHttpContextAccessor httpContextAccessor) : IAuthContextAccessor
{
    public const string AuthContextItemKey = "DaxaPos.AuthContext";

    public AuthContext? Current => httpContextAccessor.HttpContext?.Items[AuthContextItemKey] as AuthContext;
}
