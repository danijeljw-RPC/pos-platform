using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Menus;

public sealed record CreateMenuAvailabilityRuleRequest(Guid MenuId, DaysOfWeekMask DaysOfWeekMask, TimeOnly StartTimeLocal, TimeOnly EndTimeLocal, Guid? TenantId = null);

public sealed record MenuAvailabilityRuleResponse(
    Guid Id,
    Guid TenantId,
    Guid MenuId,
    DaysOfWeekMask DaysOfWeekMask,
    TimeOnly StartTimeLocal,
    TimeOnly EndTimeLocal,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static MenuAvailabilityRuleResponse FromEntity(MenuAvailabilityRule rule) =>
        new(rule.Id, rule.TenantId, rule.MenuId, rule.DaysOfWeekMask, rule.StartTimeLocal, rule.EndTimeLocal, rule.IsActive, rule.CreatedAtUtc);
}

/// <summary>
/// Create/list/delete endpoints for <see cref="MenuAvailabilityRule"/> (PLAN-0004 Milestone G,
/// approved Human Decision #7's day/time shape) — no update; a rule is replaced by
/// delete-then-recreate. No overnight wraparound in this milestone:
/// <see cref="MenuAvailabilityRule.StartTimeLocal"/> must be strictly before
/// <see cref="MenuAvailabilityRule.EndTimeLocal"/>.
/// </summary>
public static class MenuAvailabilityRuleEndpoints
{
    public static void MapMenuAvailabilityRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/menu-availability-rules").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapDelete("/{menuAvailabilityRuleId:guid}", DeleteAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateMenuAvailabilityRuleRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.DaysOfWeekMask == DaysOfWeekMask.None)
        {
            return Results.BadRequest("DaysOfWeekMask must include at least one day.");
        }

        if (request.StartTimeLocal >= request.EndTimeLocal)
        {
            return Results.BadRequest("StartTimeLocal must be strictly before EndTimeLocal; overnight windows are not supported in this milestone — use two rules instead.");
        }

        var authContext = authContextAccessor.Current!;

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == request.MenuId);
        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var rule = new MenuAvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            MenuId = request.MenuId,
            DaysOfWeekMask = request.DaysOfWeekMask,
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.MenuAvailabilityRules.Add(rule);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuAvailabilityRuleChangedDomainEvent(
            rule.TenantId, menu.OrganisationId, rule.Id, rule.MenuId, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { rule.DaysOfWeekMask, rule.StartTimeLocal, rule.EndTimeLocal }), now));

        return Results.Created($"/api/v1/menu-availability-rules/{rule.Id}", MenuAvailabilityRuleResponse.FromEntity(rule));
    }

    private static async Task<IResult> ListAsync(Guid? menuId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query =
            from r in dbContext.MenuAvailabilityRules
            join m in dbContext.Menus on r.MenuId equals m.Id
            where m.OrganisationId == authContext.OrganisationId && r.IsActive
            select r;

        if (menuId is not null)
        {
            query = query.Where(r => r.MenuId == menuId);
        }

        var rules = await query.ToListAsync();

        return Results.Ok(rules.Select(MenuAvailabilityRuleResponse.FromEntity));
    }

    private static async Task<IResult> DeleteAsync(
        Guid menuAvailabilityRuleId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var rule = await dbContext.MenuAvailabilityRules.SingleOrDefaultAsync(r => r.Id == menuAvailabilityRuleId);
        if (rule is null)
        {
            return Results.NotFound();
        }

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == rule.MenuId);
        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var before = new { rule.DaysOfWeekMask, rule.StartTimeLocal, rule.EndTimeLocal };

        dbContext.MenuAvailabilityRules.Remove(rule);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuAvailabilityRuleChangedDomainEvent(
            rule.TenantId, menu.OrganisationId, rule.Id, rule.MenuId, authContext.UserId,
            "Deleted", JsonSerializer.Serialize(before), null, DateTimeOffset.UtcNow));

        return Results.NoContent();
    }
}
