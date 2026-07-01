namespace DaxaPos.Domain.Entities;

public class Location
{
    public Guid Id { get; set; }

    public Guid OrganisationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
