namespace DaxaPos.Application.Tax;

/// <summary>Why <see cref="TaxCalculationEngine.CalculateLine"/> could not produce a result — a typed failure, never an exception, per the plan's endpoint-layer <c>Results.BadRequest</c> convention.</summary>
public enum TaxCalculationErrorCode
{
    /// <summary>No tax components were supplied — the caller must not silently treat this as zero tax; tax configuration is genuinely missing and the flow must fail closed.</summary>
    MissingTaxConfiguration,

    /// <summary>More than <see cref="TaxCalculationEngine.MaxComponentsPerLine"/> components were supplied (ADR-0006's per-line design limit).</summary>
    TooManyTaxComponents,
}

/// <summary>
/// Result of calculating tax for one order line. Immutable value object — <see cref="TaxLines"/>
/// is a read-only view and the type exposes no mutation surface, so the engine's output cannot be
/// altered by a caller after the fact.
/// </summary>
public sealed class TaxLineCalculationResult
{
    private TaxLineCalculationResult(bool isSuccess, decimal lineAmount, IReadOnlyList<TaxLineResult> taxLines, TaxCalculationErrorCode? errorCode)
    {
        IsSuccess = isSuccess;
        LineAmount = lineAmount;
        TaxLines = taxLines;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public decimal LineAmount { get; }

    public IReadOnlyList<TaxLineResult> TaxLines { get; }

    public TaxCalculationErrorCode? ErrorCode { get; }

    public decimal TotalTaxAmount => TaxLines.Sum(t => t.TaxAmount);

    public static TaxLineCalculationResult Success(decimal lineAmount, IReadOnlyList<TaxLineResult> taxLines) =>
        new(true, lineAmount, taxLines, null);

    public static TaxLineCalculationResult Failure(TaxCalculationErrorCode errorCode) =>
        new(false, 0m, [], errorCode);
}
