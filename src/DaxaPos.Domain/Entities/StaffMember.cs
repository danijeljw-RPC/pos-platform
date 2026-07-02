namespace DaxaPos.Domain.Entities;

/// <summary>
/// A POS operational staff identity (ADR-0013, PLAN-0003 Milestone F) — never a cloud/admin/
/// back-office identity, which is <see cref="User"/>. Authenticates via
/// <c>AuthMethod.LocalStaffPin</c> only: <see cref="StaffCode"/> (human-enterable, uppercase
/// alphanumeric, unique per organisation — never the primary key) plus a PIN stored only as
/// <see cref="PinHash"/>, and only from a trusted registered device context.
/// <see cref="LocationId"/> is the staff member's <b>home</b> location, checked at login; a
/// session's own location comes from the device context. <see cref="LinkedUserId"/> optionally
/// ties a staff member to a <see cref="User"/> for later manager-approval flows — no behaviour
/// yet.
/// </summary>
public class StaffMember
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid LocationId { get; set; }

    public string StaffCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PinHash { get; set; } = string.Empty;

    public int FailedPinAttempts { get; set; }

    public DateTimeOffset? LockedOutUntilUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? LinkedUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
