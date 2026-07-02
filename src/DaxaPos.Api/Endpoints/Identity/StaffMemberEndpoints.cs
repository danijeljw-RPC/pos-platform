using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record CreateStaffMemberRequest(string DisplayName, string StaffCode, string Pin, Guid LocationId, Guid? TenantId = null);

public sealed record AssignStaffRoleRequest(Guid RoleId, Guid? LocationId = null, Guid? TenantId = null);

public sealed record StaffMemberResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    string StaffCode,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static StaffMemberResponse FromEntity(StaffMember staffMember) =>
        new(staffMember.Id, staffMember.TenantId, staffMember.OrganisationId, staffMember.LocationId,
            staffMember.StaffCode, staffMember.DisplayName, staffMember.IsActive, staffMember.CreatedAtUtc);
}

/// <summary>Returned once at PIN reset — the only time the raw generated PIN leaves the server.</summary>
public sealed record ResetStaffPinResponse(Guid StaffMemberId, string Pin);

public sealed record StaffRoleAssignedResponse(Guid StaffMemberId, Guid RoleId, string RoleName, Guid? LocationId);

/// <summary>
/// Staff-member management (PLAN-0003 Milestone F). Every route is gated
/// <c>staff.manage</c> + <c>rejectStaffPin: true</c> — a staff PIN session must never manage
/// staff identities (ADR-0013). Location/organisation scoping follows the Milestone D pattern:
/// tenant-filtered fetch, then an <c>AuthContext.OrganisationId</c> cross-check, 404 on mismatch
/// (existence under another tenant/organisation is never confirmed). PIN resets are
/// server-generated (Decision 10): the raw PIN is returned once and only its hash is stored;
/// reset and disable both revoke the staff member's active sessions.
/// </summary>
public static class StaffMemberEndpoints
{
    public static void MapStaffMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/staff-members").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.StaffManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.StaffManage, rejectStaffPin: true);
        group.MapGet("/{staffMemberId:guid}", GetByIdAsync).RequirePermission(Permissions.StaffManage, rejectStaffPin: true);
        group.MapPost("/{staffMemberId:guid}/reset-pin", ResetPinAsync).RequirePermission(Permissions.StaffManage, rejectStaffPin: true);
        group.MapPost("/{staffMemberId:guid}/roles", AssignRoleAsync).RequirePermission(Permissions.StaffManage, rejectStaffPin: true);
        group.MapPost("/{staffMemberId:guid}/disable", DisableAsync).RequirePermission(Permissions.StaffManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateStaffMemberRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IPinHasher pinHasher,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest("DisplayName is required.");
        }

        if (!StaffCodePolicy.IsValid(request.StaffCode))
        {
            return Results.BadRequest($"StaffCode must be {StaffCodePolicy.MinLength}-{StaffCodePolicy.MaxLength} letters and digits.");
        }

        if (!StaffPinPolicy.IsValid(request.Pin))
        {
            return Results.BadRequest($"Pin must be {StaffPinPolicy.MinLength}-{StaffPinPolicy.MaxLength} digits.");
        }

        var authContext = authContextAccessor.Current!;

        // Context provenance (ADR-0015): the location resolves under the tenant filter, then its
        // organisation is cross-checked against the caller's own — 404 on any mismatch.
        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var staffCode = StaffCodePolicy.Normalize(request.StaffCode);

        if (await dbContext.StaffMembers.AnyAsync(s => s.OrganisationId == location.OrganisationId && s.StaffCode == staffCode))
        {
            return Results.Conflict("StaffCode is already in use within this organisation.");
        }

        var now = DateTimeOffset.UtcNow;
        var staffMember = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = location.OrganisationId,
            LocationId = location.Id,
            StaffCode = staffCode,
            DisplayName = request.DisplayName,
            PinHash = pinHasher.Hash(request.Pin),
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.StaffMembers.Add(staffMember);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new StaffMemberLifecycleDomainEvent(
            staffMember.TenantId, staffMember.OrganisationId, staffMember.LocationId, staffMember.Id,
            authContext.UserId, "Created", null,
            JsonSerializer.Serialize(new { staffMember.StaffCode, staffMember.DisplayName, staffMember.LocationId }), now));

        return Results.Created($"/api/v1/staff-members/{staffMember.Id}", StaffMemberResponse.FromEntity(staffMember));
    }

    private static async Task<IResult> ListAsync(
        Guid? locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        // List hides inactive staff by default; single GET does not (Milestone D convention).
        var query = dbContext.StaffMembers
            .Where(s => s.OrganisationId == authContext.OrganisationId && s.IsActive);

        if (locationId is not null)
        {
            query = query.Where(s => s.LocationId == locationId);
        }

        var staffMembers = await query.OrderBy(s => s.StaffCode).ToListAsync();

        return Results.Ok(staffMembers.Select(StaffMemberResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid staffMemberId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var staffMember = await ResolveAsync(staffMemberId, authContext, dbContext);

        return staffMember is null ? Results.NotFound() : Results.Ok(StaffMemberResponse.FromEntity(staffMember));
    }

    private static async Task<IResult> ResetPinAsync(
        Guid staffMemberId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IPinHasher pinHasher,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var staffMember = await ResolveAsync(staffMemberId, authContext, dbContext);

        if (staffMember is null)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var newPin = StaffPinPolicy.GeneratePin();

        staffMember.PinHash = pinHasher.Hash(newPin);
        staffMember.FailedPinAttempts = 0;
        staffMember.LockedOutUntilUtc = null;

        var sessionsRevoked = await RevokeActiveSessionsAsync(dbContext, staffMember.Id, "StaffPinReset", now);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new StaffMemberLifecycleDomainEvent(
            staffMember.TenantId, staffMember.OrganisationId, staffMember.LocationId, staffMember.Id,
            authContext.UserId, "PinReset", null,
            JsonSerializer.Serialize(new { SessionsRevoked = sessionsRevoked }), now));

        return Results.Ok(new ResetStaffPinResponse(staffMember.Id, newPin));
    }

    private static async Task<IResult> AssignRoleAsync(
        Guid staffMemberId,
        AssignStaffRoleRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var authContext = authContextAccessor.Current!;
        var staffMember = await ResolveAsync(staffMemberId, authContext, dbContext);

        if (staffMember is null)
        {
            return Results.NotFound();
        }

        // Role is a system-wide catalogue (unfiltered); an unknown id is still a 404.
        var role = await dbContext.Roles.SingleOrDefaultAsync(r => r.Id == request.RoleId);

        if (role is null)
        {
            return Results.NotFound();
        }

        if (request.LocationId is not null)
        {
            var scopeLocation = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);

            if (scopeLocation is null || scopeLocation.OrganisationId != authContext.OrganisationId)
            {
                return Results.NotFound();
            }
        }

        if (await dbContext.StaffMemberRoles.AnyAsync(sr => sr.StaffMemberId == staffMember.Id && sr.RoleId == role.Id))
        {
            return Results.Conflict("The staff member already holds this role.");
        }

        var now = DateTimeOffset.UtcNow;

        dbContext.StaffMemberRoles.Add(new StaffMemberRole
        {
            StaffMemberId = staffMember.Id,
            RoleId = role.Id,
            TenantId = authContext.TenantId,
            LocationId = request.LocationId,
        });
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new StaffMemberLifecycleDomainEvent(
            staffMember.TenantId, staffMember.OrganisationId, staffMember.LocationId, staffMember.Id,
            authContext.UserId, "RoleAssigned", null,
            JsonSerializer.Serialize(new { RoleName = role.Name, request.LocationId }), now));

        return Results.Ok(new StaffRoleAssignedResponse(staffMember.Id, role.Id, role.Name, request.LocationId));
    }

    private static async Task<IResult> DisableAsync(
        Guid staffMemberId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var staffMember = await ResolveAsync(staffMemberId, authContext, dbContext);

        if (staffMember is null)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;

        // Emergency disable (ADR-0013): deactivate and cut off existing sessions in one operation.
        // Idempotent — disabling an already-disabled staff member is not an error.
        staffMember.IsActive = false;
        var sessionsRevoked = await RevokeActiveSessionsAsync(dbContext, staffMember.Id, "StaffMemberDisabled", now);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new StaffMemberDisabledDomainEvent(
            staffMember.TenantId, staffMember.OrganisationId, staffMember.LocationId, staffMember.Id,
            authContext.UserId, sessionsRevoked, now));

        return Results.Ok(StaffMemberResponse.FromEntity(staffMember));
    }

    /// <summary>
    /// Resolves a staff member under the caller's tenant (query filter) and organisation
    /// cross-check; null means 404. No <c>IsActive</c> filter — reset-pin/disable/get must find
    /// their target regardless of state (Milestone D convention).
    /// </summary>
    private static async Task<StaffMember?> ResolveAsync(Guid staffMemberId, AuthContext authContext, DaxaDbContext dbContext)
    {
        var staffMember = await dbContext.StaffMembers.SingleOrDefaultAsync(s => s.Id == staffMemberId);

        return staffMember is null || staffMember.OrganisationId != authContext.OrganisationId ? null : staffMember;
    }

    private static async Task<int> RevokeActiveSessionsAsync(DaxaDbContext dbContext, Guid staffMemberId, string reason, DateTimeOffset now)
    {
        var activeSessions = await dbContext.AuthSessions
            .Where(s => s.StaffMemberId == staffMemberId && s.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedReason = reason;
        }

        return activeSessions.Count;
    }
}
