using DaxaPos.Domain.Tenancy;

namespace DaxaPos.Api.Tests.Support;

/// <summary>
/// Test double for <see cref="ICurrentTenantProvider"/> — lets a test set (or omit) the tenant
/// context directly, without needing a real authenticated request.
/// </summary>
public sealed class FakeCurrentTenantProvider(Guid? tenantId) : ICurrentTenantProvider
{
    public Guid? TenantId { get; } = tenantId;
}
