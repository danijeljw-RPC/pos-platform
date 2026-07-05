namespace DaxaPos.Domain.Entities;

/// <summary>
/// Backs <see cref="Order.OrderNumber"/>'s per-location monotonic sequence (PLAN-0005 Milestone A,
/// approved Human Decision #2). One row per <see cref="LocationId"/> (the primary key), allocated
/// via a single atomic <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> statement against
/// <see cref="NextValue"/> — never a read-then-write <c>MAX(OrderNumber) + 1</c>, which would race
/// under concurrent order-open calls the same way OI-0013/OI-0017 do. Not itself a financial
/// record (ADR-0010) — a pure allocation counter, never read back or displayed on its own.
/// </summary>
public class OrderNumberCounter
{
    public Guid LocationId { get; set; }

    public Guid TenantId { get; set; }

    public long NextValue { get; set; }
}
