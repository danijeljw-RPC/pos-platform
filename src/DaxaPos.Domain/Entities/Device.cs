namespace DaxaPos.Domain.Entities;

/// <summary>
/// A registered physical/logical device at a location — e.g. a Windows POS machine, iPad KDS
/// screen, customer display, payment terminal, printer, or kiosk/browser device. Distinct from
/// <see cref="Terminal"/>: a device is not always a POS selling endpoint.
/// </summary>
public class Device
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid LocationId { get; set; }

    public DeviceType DeviceType { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
