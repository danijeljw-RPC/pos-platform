namespace DaxaPos.Domain.Entities;

public class Location
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// IANA time zone id (e.g. <c>"Australia/Sydney"</c>) used to evaluate
    /// <see cref="MenuAvailabilityRule"/> windows in the location's own local time (PLAN-0004
    /// Milestone G, ADR-0003's location-context requirement) — never UTC-naively. Defaults to
    /// <c>"UTC"</c>, a neutral value, not an AU/NZ-specific default. Not yet settable via
    /// <c>LocationEndpoints</c> (PLAN-0003, out of Milestone G's scope) — see the Milestone G
    /// worker notes for the follow-up.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    public DateTimeOffset CreatedAtUtc { get; set; }
}
