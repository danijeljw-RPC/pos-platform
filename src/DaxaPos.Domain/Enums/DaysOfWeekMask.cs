namespace DaxaPos.Domain.Enums;

/// <summary>
/// Days a <see cref="Entities.MenuAvailabilityRule"/> applies on (PLAN-0004 Milestone G, approved
/// Human Decision #7's day/time shape) — a bitwise combination, Monday first per ISO 8601 week
/// convention (not <see cref="System.DayOfWeek"/>'s Sunday-first ordering).
/// </summary>
[Flags]
public enum DaysOfWeekMask
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
}
