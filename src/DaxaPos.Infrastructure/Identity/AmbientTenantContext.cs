namespace DaxaPos.Infrastructure.Identity;

/// <summary>
/// Ambient (<see cref="AsyncLocal{T}"/>-backed) tenant context for hosts with no
/// <c>HttpContext</c>/<c>AuthContext</c> to derive one from — currently only
/// <c>DaxaPos.Workers</c> (PLAN-0005 Milestone E). The worker's outbox-polling loop sets
/// <see cref="Current"/> to a claimed work item's <c>TenantId</c> before processing it, so
/// <see cref="AmbientCurrentTenantProvider"/> (and therefore <c>DaxaDbContext</c>'s fail-closed
/// query filters) scope every subsequent query in that unit of work to the right tenant, exactly
/// as <see cref="HttpContextAuthContextAccessor"/> does for a real HTTP request. Flows with the
/// async call chain, so it must be set (and reset) around each work item's processing, not once
/// at host startup.
/// </summary>
public static class AmbientTenantContext
{
    private static readonly AsyncLocal<Guid?> Ambient = new();

    public static Guid? Current
    {
        get => Ambient.Value;
        set => Ambient.Value = value;
    }
}
