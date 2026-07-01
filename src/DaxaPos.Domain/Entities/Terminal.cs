namespace DaxaPos.Domain.Entities;

/// <summary>
/// A POS selling endpoint / operational POS identity at a location — e.g. "Front Counter 1",
/// "Bar POS 2", "Food Truck POS". Distinct from <see cref="Device"/>: a terminal may be
/// associated with a device, but a device is not always a terminal.
/// </summary>
public class Terminal
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid LocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? DeviceId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
