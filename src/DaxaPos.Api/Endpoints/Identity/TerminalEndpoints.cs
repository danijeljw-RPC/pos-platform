using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record CreateTerminalRequest(string Name, Guid LocationId, Guid? TenantId = null);

public sealed record UpdateTerminalRequest(string Name, Guid? TenantId = null);

public sealed record TerminalResponse(Guid Id, Guid TenantId, Guid LocationId, Guid? DeviceId, string Name, bool IsActive, DateTimeOffset CreatedAtUtc)
{
    public static TerminalResponse FromEntity(Terminal terminal) =>
        new(terminal.Id, terminal.TenantId, terminal.LocationId, terminal.DeviceId, terminal.Name, terminal.IsActive, terminal.CreatedAtUtc);
}

/// <summary>
/// Create/read/rename/deactivate/reactivate endpoints for <see cref="Terminal"/> (PLAN-0003
/// Milestone D). <see cref="Terminal"/> has no <c>OrganisationId</c> column and no navigation
/// property to <see cref="Location"/> (see <c>TerminalConfiguration.cs</c>), so the
/// <c>AuthContext.OrganisationId</c> cross-check (same rule as <see cref="LocationEndpoints"/>)
/// walks <c>Terminal -&gt; Location -&gt; OrganisationId</c> via an explicit second lookup or LINQ
/// join, never <c>.Include()</c>. No hard delete.
/// </summary>
public static class TerminalEndpoints
{
    public static void MapTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/terminals").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true);
        group.MapGet("/{terminalId:guid}", GetByIdAsync).RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true);
        group.MapPatch("/{terminalId:guid}", UpdateAsync).RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true);
        group.MapPost("/{terminalId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true);
        group.MapPost("/{terminalId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.TerminalsManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateTerminalRequest request,
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

        // The referenced location must exist (tenant filter) and belong to the caller's
        // organisation — walking Terminal's one and only parent relationship, per ADR-0015 Context
        // Provenance. A mismatch is 404, never a validation error.
        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var terminal = new Terminal
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            LocationId = request.LocationId,
            Name = request.Name,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.Terminals.Add(terminal);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TerminalLifecycleDomainEvent(
            terminal.TenantId, location.OrganisationId, terminal.LocationId, terminal.Id, authContext.UserId, "Created", null, JsonSerializer.Serialize(new { terminal.Name }), now));

        return Results.Created($"/api/v1/terminals/{terminal.Id}", TerminalResponse.FromEntity(terminal));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var terminals = await (
            from t in dbContext.Terminals
            join l in dbContext.Locations on t.LocationId equals l.Id
            where l.OrganisationId == authContext.OrganisationId && t.IsActive
            orderby t.Name
            select t
        ).ToListAsync();

        return Results.Ok(terminals.Select(TerminalResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid terminalId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var terminal = await FindAuthorizedTerminalAsync(terminalId, authContext.OrganisationId, dbContext);

        return terminal is null ? Results.NotFound() : Results.Ok(TerminalResponse.FromEntity(terminal));
    }

    private static async Task<IResult> UpdateAsync(
        Guid terminalId,
        UpdateTerminalRequest request,
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
        var terminal = await FindAuthorizedTerminalAsync(terminalId, authContext.OrganisationId, dbContext);

        if (terminal is null)
        {
            return Results.NotFound();
        }

        var beforeName = terminal.Name;
        terminal.Name = request.Name;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(terminal.LocationId, dbContext);

        await dispatcher.DispatchAsync(new TerminalLifecycleDomainEvent(
            terminal.TenantId,
            organisationId,
            terminal.LocationId,
            terminal.Id,
            authContext.UserId,
            "Updated",
            JsonSerializer.Serialize(new { Name = beforeName }),
            JsonSerializer.Serialize(new { terminal.Name }),
            DateTimeOffset.UtcNow));

        return Results.Ok(TerminalResponse.FromEntity(terminal));
    }

    private static Task<IResult> DeactivateAsync(
        Guid terminalId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(terminalId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid terminalId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(terminalId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid terminalId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var terminal = await FindAuthorizedTerminalAsync(terminalId, authContext.OrganisationId, dbContext);

        if (terminal is null)
        {
            return Results.NotFound();
        }

        var beforeIsActive = terminal.IsActive;
        terminal.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(terminal.LocationId, dbContext);

        await dispatcher.DispatchAsync(new TerminalLifecycleDomainEvent(
            terminal.TenantId,
            organisationId,
            terminal.LocationId,
            terminal.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { terminal.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(TerminalResponse.FromEntity(terminal));
    }

    /// <summary>
    /// Fetches a terminal (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its location, walking
    /// <c>Terminal -&gt; Location -&gt; OrganisationId</c>. Returns <c>null</c> for either a missing
    /// terminal or an organisation mismatch — callers must not distinguish the two in their
    /// response (404 either way).
    /// </summary>
    private static async Task<Terminal?> FindAuthorizedTerminalAsync(Guid terminalId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var terminal = await dbContext.Terminals.SingleOrDefaultAsync(t => t.Id == terminalId);

        if (terminal is null)
        {
            return null;
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == terminal.LocationId);

        return location is null || location.OrganisationId != callerOrganisationId ? null : terminal;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid locationId, DaxaDbContext dbContext) =>
        (await dbContext.Locations.SingleAsync(l => l.Id == locationId)).OrganisationId;
}
