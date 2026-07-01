using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record CreateOrganisationRequest(string Name, Guid? TenantId = null);

public sealed record UpdateOrganisationRequest(string Name, Guid? TenantId = null);

public sealed record OrganisationResponse(Guid Id, Guid TenantId, string Name, bool IsActive, DateTimeOffset CreatedAtUtc)
{
    public static OrganisationResponse FromEntity(Organisation organisation) =>
        new(organisation.Id, organisation.TenantId, organisation.Name, organisation.IsActive, organisation.CreatedAtUtc);
}

/// <summary>
/// Create/read/rename/deactivate/reactivate endpoints for <see cref="Organisation"/> (PLAN-0003
/// Milestone D). Scoped to the caller's tenant only — <c>organisations.manage</c> is granted
/// exclusively to <c>SystemAdmin</c> in the Initial Permission Catalogue, and a tenant may own more
/// than one organisation, so no further <c>AuthContext.OrganisationId</c> restriction is applied
/// here (contrast with <see cref="LocationEndpoints"/>/<see cref="TerminalEndpoints"/>, which are
/// also granted to organisation-scoped roles). No hard delete — see <see cref="DeactivateAsync"/>/
/// <see cref="ReactivateAsync"/>.
/// </summary>
public static class OrganisationEndpoints
{
    public static void MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organisations").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.OrganisationsManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.OrganisationsManage, rejectStaffPin: true);
        group.MapGet("/{organisationId:guid}", GetByIdAsync).RequirePermission(Permissions.OrganisationsManage, rejectStaffPin: true);
        group.MapPatch("/{organisationId:guid}", UpdateAsync).RequirePermission(Permissions.OrganisationsManage, rejectStaffPin: true);
        group.MapPost("/{organisationId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.OrganisationsManage, rejectStaffPin: true);
        group.MapPost("/{organisationId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.OrganisationsManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateOrganisationRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("Name is required.");
        }

        var authContext = authContextAccessor.Current!;
        var now = DateTimeOffset.UtcNow;

        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            Name = request.Name,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.Organisations.Add(organisation);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrganisationLifecycleDomainEvent(
            organisation.TenantId, organisation.Id, authContext.UserId, "Created", null, JsonSerializer.Serialize(new { organisation.Name }), now));

        return Results.Created($"/api/v1/organisations/{organisation.Id}", OrganisationResponse.FromEntity(organisation));
    }

    private static async Task<IResult> ListAsync(DaxaDbContext dbContext)
    {
        // Active-only by default — tenant isolation (the query filter) and lifecycle visibility are
        // separate concerns; this is the lifecycle half of that rule.
        var organisations = await dbContext.Organisations
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync();

        return Results.Ok(organisations.Select(OrganisationResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid organisationId, DaxaDbContext dbContext)
    {
        // No IsActive filter — a caller with organisations.manage may look up an inactive
        // organisation directly by id (e.g. to review it before reactivating).
        var organisation = await dbContext.Organisations.SingleOrDefaultAsync(o => o.Id == organisationId);

        return organisation is null ? Results.NotFound() : Results.Ok(OrganisationResponse.FromEntity(organisation));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organisationId,
        UpdateOrganisationRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("Name is required.");
        }

        var organisation = await dbContext.Organisations.SingleOrDefaultAsync(o => o.Id == organisationId);

        if (organisation is null)
        {
            return Results.NotFound();
        }

        var authContext = authContextAccessor.Current!;
        var beforeName = organisation.Name;
        organisation.Name = request.Name;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrganisationLifecycleDomainEvent(
            organisation.TenantId,
            organisation.Id,
            authContext.UserId,
            "Updated",
            JsonSerializer.Serialize(new { Name = beforeName }),
            JsonSerializer.Serialize(new { organisation.Name }),
            DateTimeOffset.UtcNow));

        return Results.Ok(OrganisationResponse.FromEntity(organisation));
    }

    private static Task<IResult> DeactivateAsync(
        Guid organisationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(organisationId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid organisationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(organisationId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid organisationId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        // No IsActive filter on the lookup — deactivate/reactivate must be able to find the target
        // row regardless of its current state (otherwise reactivate could never find an inactive row).
        var organisation = await dbContext.Organisations.SingleOrDefaultAsync(o => o.Id == organisationId);

        if (organisation is null)
        {
            return Results.NotFound();
        }

        var authContext = authContextAccessor.Current!;
        var beforeIsActive = organisation.IsActive;
        organisation.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrganisationLifecycleDomainEvent(
            organisation.TenantId,
            organisation.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { organisation.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(OrganisationResponse.FromEntity(organisation));
    }
}
