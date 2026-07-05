using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class TaxDefinitionTemplateConfiguration : IEntityTypeConfiguration<TaxDefinitionTemplate>
{
    public void Configure(EntityTypeBuilder<TaxDefinitionTemplate> builder)
    {
        builder.ToTable("tax_definition_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CountryCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.RegionCode).HasMaxLength(10);
        builder.Property(t => t.RatePercent).IsRequired().HasPrecision(9, 4);
        builder.Property(t => t.JurisdictionName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.JurisdictionType).IsRequired();
        builder.Property(t => t.IncludedInPrice).IsRequired();
        builder.Property(t => t.RoundingMode).IsRequired();
        builder.Property(t => t.RoundingPrecision).IsRequired();
        builder.Property(t => t.CalculationScope).IsRequired();
        builder.Property(t => t.ReceiptMarkerCode).HasMaxLength(10);
        builder.Property(t => t.ReceiptMarkerLabel).HasMaxLength(100);
        builder.Property(t => t.ReportingCategory).HasMaxLength(100);
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

        // AU/NZ MVP seed data (PLAN-0004 plan doc, "AU/NZ MVP Tax Assumptions"). Tenants clone
        // from these via an explicit endpoint call (Milestone C) — never auto-provisioned at
        // runtime (approved Human Decision #3: no silent auto-creation of tax defaults).
        builder.HasData(
            new TaxDefinitionTemplate
            {
                Id = TaxSeedIds.AuGst10TemplateId,
                Code = "AU_GST_10",
                Name = "GST",
                CountryCode = "AU",
                RatePercent = 10m,
                JurisdictionName = "Australia",
                JurisdictionType = TaxJurisdictionType.Country,
                IncludedInPrice = true,
                RoundingMode = TaxRoundingMode.NearestCent,
                RoundingPrecision = 2,
                CalculationScope = TaxCalculationScope.PerLine,
                ReportingCategory = "GST",
                IsActive = true,
            },
            new TaxDefinitionTemplate
            {
                Id = TaxSeedIds.AuGstFreeTemplateId,
                Code = "AU_GST_FREE",
                Name = "GST",
                CountryCode = "AU",
                RatePercent = 0m,
                JurisdictionName = "Australia",
                JurisdictionType = TaxJurisdictionType.Country,
                IncludedInPrice = true,
                RoundingMode = TaxRoundingMode.NearestCent,
                RoundingPrecision = 2,
                CalculationScope = TaxCalculationScope.PerLine,
                ReceiptMarkerCode = "F",
                ReceiptMarkerLabel = "GST-free",
                ReportingCategory = "GSTFree",
                IsActive = true,
            },
            new TaxDefinitionTemplate
            {
                Id = TaxSeedIds.NzGst15TemplateId,
                Code = "NZ_GST_15",
                Name = "GST",
                CountryCode = "NZ",
                RatePercent = 15m,
                JurisdictionName = "New Zealand",
                JurisdictionType = TaxJurisdictionType.Country,
                IncludedInPrice = true,
                RoundingMode = TaxRoundingMode.NearestCent,
                RoundingPrecision = 2,
                CalculationScope = TaxCalculationScope.PerLine,
                ReportingCategory = "GST",
                IsActive = true,
            },
            new TaxDefinitionTemplate
            {
                Id = TaxSeedIds.NzZeroRatedTemplateId,
                Code = "NZ_ZERO_RATED",
                Name = "GST",
                CountryCode = "NZ",
                RatePercent = 0m,
                JurisdictionName = "New Zealand",
                JurisdictionType = TaxJurisdictionType.Country,
                IncludedInPrice = true,
                RoundingMode = TaxRoundingMode.NearestCent,
                RoundingPrecision = 2,
                CalculationScope = TaxCalculationScope.PerLine,
                ReceiptMarkerCode = "Z",
                ReceiptMarkerLabel = "Zero-rated",
                ReportingCategory = "ZeroRated",
                IsActive = true,
            },
            new TaxDefinitionTemplate
            {
                Id = TaxSeedIds.NzExemptTemplateId,
                Code = "NZ_EXEMPT",
                Name = "GST",
                CountryCode = "NZ",
                RatePercent = 0m,
                JurisdictionName = "New Zealand",
                JurisdictionType = TaxJurisdictionType.Country,
                IncludedInPrice = true,
                RoundingMode = TaxRoundingMode.NearestCent,
                RoundingPrecision = 2,
                CalculationScope = TaxCalculationScope.PerLine,
                ReceiptMarkerCode = "E",
                ReceiptMarkerLabel = "Exempt",
                ReportingCategory = "Exempt",
                IsActive = true,
            });
    }
}
