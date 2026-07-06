using DaxaPos.Domain.Tenancy;

namespace DaxaPos.Infrastructure.Identity;

/// <summary>
/// <see cref="ICurrentTenantProvider"/> for <c>DaxaPos.Workers</c> (PLAN-0005 Milestone E) — reads
/// <see cref="AmbientTenantContext.Current"/> rather than an HTTP request's <c>AuthContext</c>
/// (there is none in a background host). Returns <c>null</c> — never throws, never guesses — when
/// no ambient tenant is set, so the consuming query filter fails closed to zero rows, mirroring
/// <see cref="CurrentTenantProvider"/>'s identical guarantee for the request-bound case.
/// </summary>
public sealed class AmbientCurrentTenantProvider : ICurrentTenantProvider
{
    public Guid? TenantId => AmbientTenantContext.Current;
}
