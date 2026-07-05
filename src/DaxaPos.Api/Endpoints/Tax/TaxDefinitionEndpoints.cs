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

public sealed record CreateTaxDefinitionRequest(
    string Code,
    string Name,
    Guid OrganisationId,
    string CountryCode,
    string? RegionCode,
    decimal RatePercent,
    string JurisdictionName,
    TaxJurisdictionType JurisdictionType,
    bool IncludedInPrice,
    TaxRoundingMode RoundingMode,
    int RoundingPrecision,
    TaxCalculationScope CalculationScope,
    string? ReceiptMarkerCode,
    string? ReceiptMarkerLabel,
    string? ReportingCategory,
    Guid? TenantId = null);

public sealed record CreateTaxDefinitionFromTemplateRequest(Guid OrganisationId, string TemplateCode, Guid? TenantId = null);

public sealed record UpdateTaxDefinitionRequest(
    string Name,
    decimal RatePercent,
    string JurisdictionName,
    TaxJurisdictionType JurisdictionType,
    bool IncludedInPrice,
    TaxRoundingMode RoundingMode,
    int RoundingPrecision,
    string? ReceiptMarkerCode,
    string? ReceiptMarkerLabel,
    string? ReportingCategory,
    Guid? TenantId = null);

public sealed record TaxDefinitionResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    string Code,
    string Name,
    string CountryCode,
    string? RegionCode,
    decimal RatePercent,
    string JurisdictionName,
    TaxJurisdictionType JurisdictionType,
    bool IncludedInPrice,
    TaxRoundingMode RoundingMode,
    int RoundingPrecision,
    TaxCalculationScope CalculationScope,
    string? ReceiptMarkerCode,
    string? ReceiptMarkerLabel,
    string? ReportingCategory,
    bool IsActive,
    string? SourceTemplateCode,
    DateTimeOffset CreatedAtUtc)
{
    public static TaxDefinitionResponse FromEntity(TaxDefinition definition) =>
        new(definition.Id, definition.TenantId, definition.OrganisationId, definition.Code, definition.Name,
            definition.CountryCode, definition.RegionCode, definition.RatePercent, definition.JurisdictionName,
            definition.JurisdictionType, definition.IncludedInPrice, definition.RoundingMode, definition.RoundingPrecision,
            definition.CalculationScope, definition.ReceiptMarkerCode, definition.ReceiptMarkerLabel,
            definition.ReportingCategory, definition.IsActive, definition.SourceTemplateCode, definition.CreatedAtUtc);
}

/// <summary>
/// Create (from-scratch or from-template)/read/update/deactivate/reactivate endpoints for
/// <see cref="TaxDefinition"/> (PLAN-0004 Milestone C, OI-0007). Tenant-owned and independently
/// editable per tenant — cloning from <see cref="TaxDefinitionTemplate"/> is a convenience, not a
/// live link (editing a tenant's own <see cref="TaxDefinition"/> never touches the template or any
/// other tenant's clone). Every operation is checked against <c>AuthContext.OrganisationId</c>,
/// same pattern as <see cref="Identity.LocationEndpoints"/> — a mismatch is 404, never 403. No hard
/// delete: a <see cref="TaxDefinition"/> is financially meaningful (ADR-0010).
/// </summary>
public static class TaxDefinitionEndpoints
{
    public static void MapTaxDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tax-definitions").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/from-template", CreateFromTemplateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{taxDefinitionId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{taxDefinitionId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{taxDefinitionId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{taxDefinitionId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateTaxDefinitionRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest("Code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CountryCode))
        {
            return Results.BadRequest("CountryCode is required.");
        }

        var validationError = ValidateCommonFields(request.Name, request.JurisdictionName, request.RatePercent, request.RoundingPrecision);
        if (validationError is not null)
        {
            return validationError;
        }

        var authContext = authContextAccessor.Current!;

        if (authContext.OrganisationId != request.OrganisationId)
        {
            return Results.NotFound();
        }

        if (await dbContext.TaxDefinitions.AnyAsync(t => t.TenantId == authContext.TenantId && t.Code == request.Code))
        {
            return Results.Conflict("Code is already in use within this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var definition = new TaxDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            Code = request.Code,
            Name = request.Name,
            CountryCode = request.CountryCode,
            RegionCode = request.RegionCode,
            RatePercent = request.RatePercent,
            JurisdictionName = request.JurisdictionName,
            JurisdictionType = request.JurisdictionType,
            IncludedInPrice = request.IncludedInPrice,
            RoundingMode = request.RoundingMode,
            RoundingPrecision = request.RoundingPrecision,
            CalculationScope = request.CalculationScope,
            ReceiptMarkerCode = request.ReceiptMarkerCode,
            ReceiptMarkerLabel = request.ReceiptMarkerLabel,
            ReportingCategory = request.ReportingCategory,
            IsActive = true,
            SourceTemplateCode = null,
            CreatedAtUtc = now,
        };

        dbContext.TaxDefinitions.Add(definition);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxDefinitionLifecycleDomainEvent(
            definition.TenantId, definition.OrganisationId, definition.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(ToSnapshot(definition)), now));

        return Results.Created($"/api/v1/tax-definitions/{definition.Id}", TaxDefinitionResponse.FromEntity(definition));
    }

    private static async Task<IResult> CreateFromTemplateAsync(
        CreateTaxDefinitionFromTemplateRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            return Results.BadRequest("TemplateCode is required.");
        }

        var authContext = authContextAccessor.Current!;

        if (authContext.OrganisationId != request.OrganisationId)
        {
            return Results.NotFound();
        }

        var template = await dbContext.TaxDefinitionTemplates.SingleOrDefaultAsync(t => t.Code == request.TemplateCode);

        if (template is null)
        {
            return Results.NotFound();
        }

        if (await dbContext.TaxDefinitions.AnyAsync(t => t.TenantId == authContext.TenantId && t.Code == template.Code))
        {
            return Results.Conflict("Code is already in use within this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var definition = new TaxDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            Code = template.Code,
            Name = template.Name,
            CountryCode = template.CountryCode,
            RegionCode = template.RegionCode,
            RatePercent = template.RatePercent,
            JurisdictionName = template.JurisdictionName,
            JurisdictionType = template.JurisdictionType,
            IncludedInPrice = template.IncludedInPrice,
            RoundingMode = template.RoundingMode,
            RoundingPrecision = template.RoundingPrecision,
            CalculationScope = template.CalculationScope,
            ReceiptMarkerCode = template.ReceiptMarkerCode,
            ReceiptMarkerLabel = template.ReceiptMarkerLabel,
            ReportingCategory = template.ReportingCategory,
            IsActive = true,
            SourceTemplateCode = template.Code,
            CreatedAtUtc = now,
        };

        dbContext.TaxDefinitions.Add(definition);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxDefinitionLifecycleDomainEvent(
            definition.TenantId, definition.OrganisationId, definition.Id, authContext.UserId,
            "CreatedFromTemplate", null, JsonSerializer.Serialize(ToSnapshot(definition)), now));

        return Results.Created($"/api/v1/tax-definitions/{definition.Id}", TaxDefinitionResponse.FromEntity(definition));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var definitions = await dbContext.TaxDefinitions
            .Where(t => t.OrganisationId == authContext.OrganisationId && t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync();

        return Results.Ok(definitions.Select(TaxDefinitionResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid taxDefinitionId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var definition = await dbContext.TaxDefinitions.SingleOrDefaultAsync(t => t.Id == taxDefinitionId);

        if (definition is null || definition.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(TaxDefinitionResponse.FromEntity(definition));
    }

    private static async Task<IResult> UpdateAsync(
        Guid taxDefinitionId,
        UpdateTaxDefinitionRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        // Code/CountryCode/RegionCode are the definition's stable identity — not editable here.
        var validationError = ValidateCommonFields(request.Name, request.JurisdictionName, request.RatePercent, request.RoundingPrecision);
        if (validationError is not null)
        {
            return validationError;
        }

        var authContext = authContextAccessor.Current!;
        var definition = await dbContext.TaxDefinitions.SingleOrDefaultAsync(t => t.Id == taxDefinitionId);

        if (definition is null || definition.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var before = ToSnapshot(definition);

        definition.Name = request.Name;
        definition.RatePercent = request.RatePercent;
        definition.JurisdictionName = request.JurisdictionName;
        definition.JurisdictionType = request.JurisdictionType;
        definition.IncludedInPrice = request.IncludedInPrice;
        definition.RoundingMode = request.RoundingMode;
        definition.RoundingPrecision = request.RoundingPrecision;
        definition.ReceiptMarkerCode = request.ReceiptMarkerCode;
        definition.ReceiptMarkerLabel = request.ReceiptMarkerLabel;
        definition.ReportingCategory = request.ReportingCategory;

        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxDefinitionLifecycleDomainEvent(
            definition.TenantId, definition.OrganisationId, definition.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(ToSnapshot(definition)), DateTimeOffset.UtcNow));

        return Results.Ok(TaxDefinitionResponse.FromEntity(definition));
    }

    private static Task<IResult> DeactivateAsync(
        Guid taxDefinitionId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(taxDefinitionId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid taxDefinitionId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(taxDefinitionId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid taxDefinitionId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var definition = await dbContext.TaxDefinitions.SingleOrDefaultAsync(t => t.Id == taxDefinitionId);

        if (definition is null || definition.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeIsActive = definition.IsActive;
        definition.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxDefinitionLifecycleDomainEvent(
            definition.TenantId,
            definition.OrganisationId,
            definition.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { definition.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(TaxDefinitionResponse.FromEntity(definition));
    }

    private static IResult? ValidateCommonFields(string name, string jurisdictionName, decimal ratePercent, int roundingPrecision)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(jurisdictionName))
        {
            return Results.BadRequest("JurisdictionName is required.");
        }

        if (ratePercent < 0)
        {
            return Results.BadRequest("RatePercent must not be negative.");
        }

        if (roundingPrecision < 0)
        {
            return Results.BadRequest("RoundingPrecision must not be negative.");
        }

        return null;
    }

    private static object ToSnapshot(TaxDefinition definition) => new
    {
        definition.Code,
        definition.Name,
        definition.RatePercent,
        definition.JurisdictionName,
        definition.JurisdictionType,
        definition.IncludedInPrice,
        definition.RoundingMode,
        definition.RoundingPrecision,
        definition.ReceiptMarkerCode,
        definition.ReceiptMarkerLabel,
        definition.ReportingCategory,
        definition.SourceTemplateCode,
    };
}
