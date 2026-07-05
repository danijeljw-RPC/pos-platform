using DaxaPos.Application.Pricing;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;

namespace DaxaPos.UnitTests.Pricing;

/// <summary>
/// PLAN-0004 Milestone F. The second genuinely financial logic in the codebase after
/// <c>TaxCalculationEngine</c> — TDD is mandatory per CLAUDE.md's Testing Rules. Reproduces the
/// plan's exact resolution order: base price -> variant delta -> modifier deltas -> location
/// override (replaces the total outright, never adds to it) -> tax-inclusive/exclusive mode from
/// <see cref="VenueTaxConfiguration"/>, which fails closed rather than silently defaulting.
/// </summary>
public class PriceResolverTests
{
    [Fact]
    public void Resolve_BasePriceOnly_WhenNoVariantModifierOrOverride()
    {
        var product = ProductWithBasePrice(10.00m);
        var venueTaxConfig = TaxInclusiveVenueConfig();

        var result = PriceResolver.Resolve(product, variant: null, modifiers: [], locationOverride: null, venueTaxConfig);

        Assert.True(result.IsSuccess);
        Assert.Equal(10.00m, result.ResolvedPrice!.Amount);
        Assert.True(result.ResolvedPrice.IsTaxInclusive);
    }

    [Fact]
    public void Resolve_AddsVariantPriceDelta()
    {
        var product = ProductWithBasePrice(10.00m);
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Large", PriceDelta = 2.00m };

        var result = PriceResolver.Resolve(product, variant, modifiers: [], locationOverride: null, TaxInclusiveVenueConfig());

        Assert.Equal(12.00m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_AddsModifierPriceDeltas()
    {
        var product = ProductWithBasePrice(10.00m);
        var modifiers = new[]
        {
            new Modifier { Id = Guid.NewGuid(), ModifierGroupId = Guid.NewGuid(), Name = "Extra Shot", PriceDelta = 1.00m },
            new Modifier { Id = Guid.NewGuid(), ModifierGroupId = Guid.NewGuid(), Name = "Oat Milk", PriceDelta = 0.50m },
        };

        var result = PriceResolver.Resolve(product, variant: null, modifiers, locationOverride: null, TaxInclusiveVenueConfig());

        Assert.Equal(11.50m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_CombinesBaseVariantAndModifierDeltas()
    {
        var product = ProductWithBasePrice(10.00m);
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Large", PriceDelta = 2.00m };
        var modifiers = new[] { new Modifier { Id = Guid.NewGuid(), ModifierGroupId = Guid.NewGuid(), Name = "Extra Shot", PriceDelta = 1.00m } };

        var result = PriceResolver.Resolve(product, variant, modifiers, locationOverride: null, TaxInclusiveVenueConfig());

        Assert.Equal(13.00m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_SupportsNegativeVariantDelta()
    {
        var product = ProductWithBasePrice(10.00m);
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Small (Discount)", PriceDelta = -3.00m };

        var result = PriceResolver.Resolve(product, variant, modifiers: [], locationOverride: null, TaxInclusiveVenueConfig());

        Assert.Equal(7.00m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_SupportsNegativeModifierDelta()
    {
        var product = ProductWithBasePrice(10.00m);
        var modifiers = new[] { new Modifier { Id = Guid.NewGuid(), ModifierGroupId = Guid.NewGuid(), Name = "Member Discount", PriceDelta = -1.50m } };

        var result = PriceResolver.Resolve(product, variant: null, modifiers, locationOverride: null, TaxInclusiveVenueConfig());

        Assert.Equal(8.50m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_SupportsZeroDeltas_NoChangeFromBasePrice()
    {
        var product = ProductWithBasePrice(10.00m);
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Regular", PriceDelta = 0m };
        var modifiers = new[] { new Modifier { Id = Guid.NewGuid(), ModifierGroupId = Guid.NewGuid(), Name = "No Change", PriceDelta = 0m } };

        var result = PriceResolver.Resolve(product, variant, modifiers, locationOverride: null, TaxInclusiveVenueConfig());

        Assert.Equal(10.00m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_LocationOverridePrice_ReplacesTheTotalOutright_NotAdditive()
    {
        var product = ProductWithBasePrice(10.00m);
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Large", PriceDelta = 2.00m };
        var modifiers = new[] { new Modifier { Id = Guid.NewGuid(), ModifierGroupId = Guid.NewGuid(), Name = "Extra Shot", PriceDelta = 1.00m } };
        var locationOverride = new ProductLocationOverride { Id = Guid.NewGuid(), ProductId = product.Id, LocationId = Guid.NewGuid(), PriceOverride = 7.00m };

        // Base+variant+modifier would be $13.00 — the override must replace that outright, not add to it.
        var result = PriceResolver.Resolve(product, variant, modifiers, locationOverride, TaxInclusiveVenueConfig());

        Assert.Equal(7.00m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_LocationOverrideWithoutPriceOverride_FallsBackToComputedTotal()
    {
        var product = ProductWithBasePrice(10.00m);
        // A location override can exist purely for IsAvailable/IsSoldOut, with PriceOverride left null.
        var locationOverride = new ProductLocationOverride { Id = Guid.NewGuid(), ProductId = product.Id, LocationId = Guid.NewGuid(), PriceOverride = null };

        var result = PriceResolver.Resolve(product, variant: null, modifiers: [], locationOverride, TaxInclusiveVenueConfig());

        Assert.Equal(10.00m, result.ResolvedPrice!.Amount);
    }

    [Fact]
    public void Resolve_TaxExclusiveVenueConfig_SetsIsTaxInclusiveFalse()
    {
        var product = ProductWithBasePrice(10.00m);
        var venueTaxConfig = new VenueTaxConfiguration { Id = Guid.NewGuid(), LocationId = Guid.NewGuid(), TaxInclusivePricing = false, TaxCalculationMode = TaxCalculationScope.PerLine };

        var result = PriceResolver.Resolve(product, variant: null, modifiers: [], locationOverride: null, venueTaxConfig);

        Assert.True(result.IsSuccess);
        Assert.False(result.ResolvedPrice!.IsTaxInclusive);
    }

    [Fact]
    public void Resolve_WithNoVenueTaxConfiguration_FailsClosed_InsteadOfSilentlyDefaulting()
    {
        var product = ProductWithBasePrice(10.00m);

        var result = PriceResolver.Resolve(product, variant: null, modifiers: [], locationOverride: null, venueTaxConfiguration: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(PriceResolutionErrorCode.MissingVenueTaxConfiguration, result.ErrorCode);
        Assert.Null(result.ResolvedPrice);
    }

    [Fact]
    public void Resolve_IsDeterministic_AndTakesNoDependencies()
    {
        // A static class with zero constructor/DI dependencies has nothing to inject a database,
        // HTTP context, or clock into — calling it twice with the same input must produce
        // equal-by-value output every time.
        var product = ProductWithBasePrice(10.00m);
        var venueTaxConfig = TaxInclusiveVenueConfig();

        var first = PriceResolver.Resolve(product, variant: null, modifiers: [], locationOverride: null, venueTaxConfig);
        var second = PriceResolver.Resolve(product, variant: null, modifiers: [], locationOverride: null, venueTaxConfig);

        Assert.Equal(first.ResolvedPrice, second.ResolvedPrice);
    }

    private static Product ProductWithBasePrice(decimal basePrice) => new()
    {
        Id = Guid.NewGuid(),
        ProductCategoryId = Guid.NewGuid(),
        Name = "Test Product",
        TaxCategoryId = Guid.NewGuid(),
        BasePrice = basePrice,
    };

    private static VenueTaxConfiguration TaxInclusiveVenueConfig() => new()
    {
        Id = Guid.NewGuid(),
        LocationId = Guid.NewGuid(),
        TaxInclusivePricing = true,
        TaxCalculationMode = TaxCalculationScope.PerLine,
    };
}
