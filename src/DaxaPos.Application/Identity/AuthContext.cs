using DaxaPos.Domain.Enums;

namespace DaxaPos.Application.Identity;

/// <summary>
/// Normalised authorization context, per ADR-0013. Every authentication method (Keycloak,
/// local username/password, staff PIN, device token, support access) must be mapped into this
/// same shape before any authorization check runs — authorization never inspects
/// <see cref="AuthMethod"/> to decide "what," only to enforce the handful of method-specific
/// restrictions ADR-0013 requires (e.g. <see cref="AuthMethod.LocalStaffPin"/> must never reach
/// identity/tenancy management endpoints, regardless of assigned permissions).
/// </summary>
/// <remarks>
/// <see cref="Roles"/> and <see cref="Permissions"/> are a snapshot taken at session creation
/// time (ADR-0013), not a live lookup — so that later permission changes do not retroactively
/// rewrite what a staff member was allowed to do at the time of a past action.
/// </remarks>
public sealed record AuthContext(
    Guid TenantId,
    Guid? OrganisationId,
    Guid? LocationId,
    Guid? TerminalId,
    Guid? UserId,
    Guid? StaffMemberId,
    Guid? DeviceId,
    AuthMethod AuthMethod,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
