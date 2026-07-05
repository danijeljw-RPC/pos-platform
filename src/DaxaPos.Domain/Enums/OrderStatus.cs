namespace DaxaPos.Domain.Enums;

/// <summary>
/// <see cref="Completed"/> is defined now (PLAN-0005 Milestone A) but unreachable until Milestone B
/// wires payment settlement — an order cannot close without a recorded payment, which doesn't
/// exist yet. Reachable in Milestone A: <see cref="Open"/>, <see cref="Held"/>,
/// <see cref="Voided"/>, <see cref="Cancelled"/>.
/// </summary>
public enum OrderStatus
{
    Open = 0,
    Held = 1,
    Completed = 2,
    Voided = 3,
    Cancelled = 4,
}
