namespace DaxaPos.Domain.Enums;

/// <summary>
/// How an <c>AuthContext</c> was established, per ADR-0013. <see cref="LocalStaffPin"/> and
/// <see cref="LocalUsernamePassword"/> are Daxa WebAPI-native and never involve Keycloak/OIDC.
/// <see cref="CloudIdentityProvider"/> is reserved for cloud/admin/back-office/support/external
/// identity and is not wired up by PLAN-0003 (see ADR-0015).
/// </summary>
public enum AuthMethod
{
    CloudIdentityProvider = 0,
    LocalUsernamePassword = 1,
    LocalStaffPin = 2,
    DeviceToken = 3,
    SupportAccess = 4,
}
