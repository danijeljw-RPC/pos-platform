namespace DaxaPos.Domain.Entities;

/// <summary>
/// A short-lived 6-digit device enrolment PIN (ADR-0008, PLAN-0003 Milestone E), scoped to a
/// tenant/organisation/location. Stored hashed, never raw (ADR-0015 §3). The PIN proves that the
/// person registering a device holds the admin-issued enrolment code; after registration the
/// device uses its issued <see cref="DeviceCredential"/>, never the PIN.
/// </summary>
public class DeviceRegistrationPin
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid LocationId { get; set; }

    public string PinHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public int MaxUses { get; set; }

    public int UsedCount { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }
}
