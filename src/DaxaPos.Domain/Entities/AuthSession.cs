using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A short-lived POS/local session backing both <see cref="AuthMethod.LocalStaffPin"/> (Milestone
/// F) and <see cref="AuthMethod.LocalUsernamePassword"/> (this milestone). Uses an opaque,
/// server-hashed bearer token — never a JWT — per ADR-0015. <see cref="RoleSnapshot"/>/
/// <see cref="PermissionSnapshot"/> are captured once at creation time and never re-derived, so a
/// later permission change does not rewrite what the session's holder was authorized to do at the
/// time of a past action.
/// </summary>
public class AuthSession
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid? OrganisationId { get; set; }

    public Guid? LocationId { get; set; }

    public Guid? TerminalId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? UserId { get; set; }

    /// <summary>
    /// Set once <c>StaffMember</c> exists (Milestone F). No FK constraint yet — added additively
    /// once the referenced table exists, per ADR-0003's "migrations should add, not repair"
    /// guidance.
    /// </summary>
    public Guid? StaffMemberId { get; set; }

    public AuthMethod AuthMethod { get; set; }

    public List<string> RoleSnapshot { get; set; } = [];

    public List<string> PermissionSnapshot { get; set; } = [];

    public string SessionTokenHash { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset LastActivityAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? RevokedReason { get; set; }
}
