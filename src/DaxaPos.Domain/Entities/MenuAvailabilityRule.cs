using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A window during which a <see cref="Menu"/> is available (PLAN-0004 Milestone G, approved Human
/// Decision #7's day/time shape). A menu with zero active rules is always available; one or more
/// means "available only during at least one matching window," evaluated in the menu's location's
/// own local time (<see cref="Location.TimeZoneId"/>) — never UTC-naively. No overnight wraparound
/// in this milestone: <see cref="StartTimeLocal"/> must be strictly before <see cref="EndTimeLocal"/>
/// (a venue open past midnight needs two rules).
/// </summary>
public class MenuAvailabilityRule
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid MenuId { get; set; }

    public DaysOfWeekMask DaysOfWeekMask { get; set; }

    public TimeOnly StartTimeLocal { get; set; }

    public TimeOnly EndTimeLocal { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
