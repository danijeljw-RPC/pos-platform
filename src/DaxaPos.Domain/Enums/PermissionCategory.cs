namespace DaxaPos.Domain.Enums;

/// <summary>
/// Classifies whether a <see cref="Entities.Permission"/> may be held by a
/// <see cref="AuthMethod.LocalStaffPin"/> session (OI-0015). Every permission is classified at
/// creation time via this required column, replacing the former hard-coded
/// <c>Permissions.AdminSensitive</c> list that had to be extended by hand for each new permission.
/// </summary>
public enum PermissionCategory
{
    /// <summary>May be held by a staff PIN session — a normal POS-operator-level action.</summary>
    Operational = 0,

    /// <summary>Must never be held by a staff PIN session (ADR-0013's staff-PIN restrictions).</summary>
    AdminSensitive = 1,
}
