using DaxaPos.Api.Authorization;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Tax;

public sealed record TaxDefinitionTemplateResponse(
    Guid Id,
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
    bool IsActive)
{
    public static TaxDefinitionTemplateResponse FromEntity(TaxDefinitionTemplate template) =>
        new(template.Id, template.Code, template.Name, template.CountryCode, template.RegionCode,
            template.RatePercent, template.JurisdictionName, template.JurisdictionType, template.IncludedInPrice,
            template.RoundingMode, template.RoundingPrecision, template.CalculationScope,
            template.ReceiptMarkerCode, template.ReceiptMarkerLabel, template.ReportingCategory, template.IsActive);
}

/// <summary>
/// Read-only listing of the system-wide, unfiltered <see cref="TaxDefinitionTemplate"/> catalogue
/// (PLAN-0004 Milestone C) — the 5 seeded AU/NZ rows today (see <c>TaxDefinitionTemplateConfiguration</c>),
/// more later. No create/update/delete here: templates are never tenant-edited (same status as
/// <see cref="Role"/>/<see cref="Permission"/>) — a tenant clones one into its own
/// <see cref="TaxDefinition"/> via <c>POST /api/v1/tax-definitions/from-template</c>. Gated
/// <c>catalog.manage</c> + <c>rejectStaffPin: true</c> like every other Milestone C endpoint — this
/// is a configuration-lookup surface for building a tenant's own tax setup, not a sales-floor read.
/// </summary>
public static class TaxDefinitionTemplateEndpoints
{
    public static void MapTaxDefinitionTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tax-definition-templates").RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> ListAsync(DaxaDbContext dbContext)
    {
        var templates = await dbContext.TaxDefinitionTemplates
            .OrderBy(t => t.Code)
            .ToListAsync();

        return Results.Ok(templates.Select(TaxDefinitionTemplateResponse.FromEntity));
    }
}
