namespace DaxaPos.Domain.Entities;

public enum DeviceCredentialStatus
{
    Active = 0,

    /// <summary>Replaced by rotation — no longer grants access, kept for audit lineage.</summary>
    Retired = 1,

    /// <summary>Terminal. A revoked device must re-register as a new <see cref="Device"/> (ADR-0008).</summary>
    Revoked = 2,
}
