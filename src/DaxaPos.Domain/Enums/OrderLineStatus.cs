namespace DaxaPos.Domain.Enums;

/// <summary>A line is voided (reversal), never hard-deleted, per ADR-0010 (PLAN-0005 Milestone A).</summary>
public enum OrderLineStatus
{
    Active = 0,
    Voided = 1,
}
