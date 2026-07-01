using DaxaPos.Application.Identity;
using DaxaPos.Domain.Tenancy;

namespace DaxaPos.Infrastructure.Identity;

/// <summary>
/// Derives the current tenant from the request's <see cref="AuthContext"/> for
/// <c>DaxaDbContext</c>'s fail-closed global query filters (ADR-0015). Returns <c>null</c> — never
/// throws, never guesses — when no <see cref="AuthContext"/> is present, so the consuming filter
/// fails closed to zero rows rather than an unfiltered query.
/// </summary>
public sealed class CurrentTenantProvider(IAuthContextAccessor authContextAccessor) : ICurrentTenantProvider
{
    public Guid? TenantId => authContextAccessor.Current?.TenantId;
}
