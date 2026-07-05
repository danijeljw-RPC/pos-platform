using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// One append-only row per <see cref="Payment"/> state transition (PLAN-0005 Milestone B, ADR-0010)
/// — proves the ledger is genuinely append-only rather than <see cref="Payment.Status"/> being
/// overwritten with no trail. No <see cref="OrganisationId"/>/<see cref="TenantId"/> column listed
/// in the plan's own field list, but added here for the same reason Milestone A added it to
/// <c>OrderLine</c>/<c>OrderLineModifier</c>/<c>OrderLineTax</c>: every tenant-owned,
/// derived-through-parent entity in this codebase carries its own denormalized <c>TenantId</c> for
/// <see cref="Persistence.DaxaDbContext"/>'s fail-closed query filter, never a join. No
/// <c>UPDATE</c> is ever issued against an existing row — only <c>INSERT</c>.
/// </summary>
public class PaymentLedgerEntry
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid PaymentId { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? Metadata { get; set; }
}
