using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Tax;

public sealed record CreateVenueTaxConfigurationRequest(Guid LocationId, bool TaxInclusivePricing, TaxCalculationScope TaxCalculationMode, Guid? TenantId = null);

public sealed record UpdateVenueTaxConfigurationRequest(bool TaxInclusivePricing, TaxCalculationScope TaxCalculationMode, Guid? TenantId = null);

public sealed record VenueTaxConfigurationResponse(
    Guid Id,
    Guid TenantId,
    Guid LocationId,
    bool TaxInclusivePricing,
    TaxCalculationScope TaxCalculationMode,
    DateTimeOffset CreatedAtUtc)
{
    public static VenueTaxConfigurationResponse FromEntity(VenueTaxConfiguration config) =>
        new(config.Id, config.TenantId, config.LocationId, config.TaxInclusivePricing, config.TaxCalculationMode, config.CreatedAtUtc);
}

/// <summary>
/// Create/read/update endpoints for <see cref="VenueTaxConfiguration"/> (PLAN-0004 Milestone F) —
/// one row per <see cref="Location"/>. Gated <c>pricing.manage</c> + <c>rejectStaffPin: true</c>,
/// per the plan's exact permission table. No <c>DELETE</c>/deactivate/reactivate: the entity has no
/// <c>IsActive</c> lifecycle. Absence for a location is never silently defaulted — <c>GET</c>
/// returns 404 exactly like any other missing row (the plan's approved Human Decision #5), and
/// nothing in this milestone auto-creates a row on a caller's behalf.
/// </summary>
public static class VenueTaxConfigurationEndpoints
{
    public static void MapVenueTaxConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/venue-tax-configurations").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapGet("/{venueTaxConfigurationId:guid}", GetByIdAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapPatch("/{venueTaxConfigurationId:guid}", UpdateAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateVenueTaxConfigurationRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var authContext = authContextAccessor.Current!;

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);
        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        if (await dbContext.VenueTaxConfigurations.AnyAsync(v => v.LocationId == request.LocationId))
        {
            return Results.Conflict("A tax configuration already exists for this location.");
        }

        var now = DateTimeOffset.UtcNow;
        var config = new VenueTaxConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            LocationId = request.LocationId,
            TaxInclusivePricing = request.TaxInclusivePricing,
            TaxCalculationMode = request.TaxCalculationMode,
            CreatedAtUtc = now,
        };

        dbContext.VenueTaxConfigurations.Add(config);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new VenueTaxConfigurationLifecycleDomainEvent(
            config.TenantId, location.OrganisationId, config.Id, config.LocationId, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(ToSnapshot(config)), now));

        return Results.Created($"/api/v1/venue-tax-configurations/{config.Id}", VenueTaxConfigurationResponse.FromEntity(config));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var configs = await (
            from v in dbContext.VenueTaxConfigurations
            join l in dbContext.Locations on v.LocationId equals l.Id
            where l.OrganisationId == authContext.OrganisationId
            select v
        ).ToListAsync();

        return Results.Ok(configs.Select(VenueTaxConfigurationResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid venueTaxConfigurationId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var config = await FindAuthorizedAsync(venueTaxConfigurationId, authContext.OrganisationId, dbContext);

        return config is null ? Results.NotFound() : Results.Ok(VenueTaxConfigurationResponse.FromEntity(config));
    }

    private static async Task<IResult> UpdateAsync(
        Guid venueTaxConfigurationId,
        UpdateVenueTaxConfigurationRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var authContext = authContextAccessor.Current!;
        var config = await FindAuthorizedAsync(venueTaxConfigurationId, authContext.OrganisationId, dbContext);

        if (config is null)
        {
            return Results.NotFound();
        }

        var before = ToSnapshot(config);

        config.TaxInclusivePricing = request.TaxInclusivePricing;
        config.TaxCalculationMode = request.TaxCalculationMode;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(config.LocationId, dbContext);

        await dispatcher.DispatchAsync(new VenueTaxConfigurationLifecycleDomainEvent(
            config.TenantId, organisationId, config.Id, config.LocationId, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(ToSnapshot(config)), DateTimeOffset.UtcNow));

        return Results.Ok(VenueTaxConfigurationResponse.FromEntity(config));
    }

    /// <summary>
    /// Fetches a configuration (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its <see cref="Location"/> (404 either way for
    /// missing/cross-organisation).
    /// </summary>
    private static async Task<VenueTaxConfiguration?> FindAuthorizedAsync(Guid venueTaxConfigurationId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var config = await dbContext.VenueTaxConfigurations.SingleOrDefaultAsync(v => v.Id == venueTaxConfigurationId);

        if (config is null)
        {
            return null;
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == config.LocationId);

        return location is null || location.OrganisationId != callerOrganisationId ? null : config;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid locationId, DaxaDbContext dbContext) =>
        (await dbContext.Locations.SingleAsync(l => l.Id == locationId)).OrganisationId;

    private static object ToSnapshot(VenueTaxConfiguration config) => new { config.TaxInclusivePricing, config.TaxCalculationMode };
}
