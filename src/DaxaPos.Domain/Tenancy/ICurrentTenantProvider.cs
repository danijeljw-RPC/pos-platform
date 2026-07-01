namespace DaxaPos.Domain.Tenancy;

/// <summary>
/// Supplies the tenant context for the current unit of work, consumed by
/// <c>DaxaDbContext</c>'s global query filters (ADR-0015). Deliberately defined in
/// <see cref="DaxaPos.Domain"/> — not <c>DaxaPos.Application</c> — so that
/// <c>DaxaPos.Persistence</c> (which only references <see cref="DaxaPos.Domain"/>) can consume it
/// without a new project reference.
/// </summary>
/// <remarks>
/// Query filters using this provider must fail closed: a <c>null</c> <see cref="TenantId"/> means
/// "no tenant context is known," and must produce zero rows, never an unfiltered query.
/// </remarks>
public interface ICurrentTenantProvider
{
    Guid? TenantId { get; }
}
