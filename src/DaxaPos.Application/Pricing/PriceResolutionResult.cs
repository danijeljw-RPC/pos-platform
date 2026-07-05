namespace DaxaPos.Application.Pricing;

/// <summary>Why <see cref="PriceResolver.Resolve"/> could not produce a result — a typed failure, never an exception.</summary>
public enum PriceResolutionErrorCode
{
    /// <summary>No <see cref="Domain.Entities.VenueTaxConfiguration"/> was supplied — the caller must not silently
    /// assume a tax-inclusive/exclusive default; per the plan's approved Human Decision #5, this fails closed.</summary>
    MissingVenueTaxConfiguration,
}

/// <summary>
/// Result of resolving a product's price for one location (PLAN-0004 Milestone F). Immutable value
/// object, matching <c>TaxLineCalculationResult</c>'s shape.
/// </summary>
public sealed class PriceResolutionResult
{
    private PriceResolutionResult(bool isSuccess, ResolvedPrice? resolvedPrice, PriceResolutionErrorCode? errorCode)
    {
        IsSuccess = isSuccess;
        ResolvedPrice = resolvedPrice;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public ResolvedPrice? ResolvedPrice { get; }

    public PriceResolutionErrorCode? ErrorCode { get; }

    public static PriceResolutionResult Success(ResolvedPrice resolvedPrice) => new(true, resolvedPrice, null);

    public static PriceResolutionResult Failure(PriceResolutionErrorCode errorCode) => new(false, null, errorCode);
}
