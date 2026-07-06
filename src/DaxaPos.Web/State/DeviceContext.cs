namespace DaxaPos.Web.State;

/// <summary>
/// Locally persisted device identity established by PLAN-0003's
/// <c>POST /api/v1/device-registration</c>. Separate from any staff/session identity (ADR-0008) —
/// this survives staff logout/login and only changes when the device itself is re-registered or
/// reset.
/// </summary>
public sealed record DeviceContext(
    Guid DeviceId,
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    string DeviceType,
    string Name,
    string DeviceToken);
