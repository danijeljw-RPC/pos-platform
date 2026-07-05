namespace DaxaPos.Domain.Entities;

/// <summary>
/// <see cref="LocationId"/> null means organisation-wide; set means location-specific (PLAN-0004
/// Milestone G). The resolved-menu projection merges both for a given location, with
/// location-specific winning for any product appearing in both (approved Human Decision #7).
/// </summary>
public class Menu
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid? LocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
