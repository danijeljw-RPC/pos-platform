namespace DaxaPos.Domain.Entities;

/// <summary>
/// A cloud/admin/back-office/local manager identity (ADR-0013) — never a POS operational staff
/// identity, which is <c>StaffMember</c> (Milestone F). Authenticates via
/// <c>AuthMethod.LocalUsernamePassword</c> for MVP, or (not yet wired — see ADR-0015 Follow-Up
/// Work) <c>AuthMethod.CloudIdentityProvider</c> via <see cref="ExternalIdentityProvider"/>/
/// <see cref="ExternalSubjectId"/>.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string? ExternalIdentityProvider { get; set; }

    public string? ExternalSubjectId { get; set; }

    public bool IsActive { get; set; } = true;

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? LockedOutUntilUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
