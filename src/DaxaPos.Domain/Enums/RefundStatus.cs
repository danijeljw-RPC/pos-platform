namespace DaxaPos.Domain.Enums;

/// <summary>
/// Mirrors <see cref="PaymentStatus"/>'s forward-looking-but-mostly-unreachable shape (PLAN-0005
/// Milestone C). A manually recorded refund (no provider adapter exists yet) settles immediately as
/// <see cref="Recorded"/>; <see cref="ProviderPending"/>/<see cref="ProviderConfirmed"/> are defined
/// per the plan's own field description ("later `ProviderPending`/`ProviderConfirmed` once
/// PLAN-0009's adapter refund path exists") but are not assigned by any code path in this milestone.
/// </summary>
public enum RefundStatus
{
    Recorded = 0,
    ProviderPending = 1,
    ProviderConfirmed = 2,
}
