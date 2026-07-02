namespace DaxaPos.Domain.Entities;

/// <summary>
/// A server-issued device credential (ADR-0008, PLAN-0003 Milestone E). The raw secret is returned
/// to the device exactly once at registration/rotation and never persisted — only a salted HMAC
/// hash is stored (ADR-0015 §3; the salt is embedded in the hash string, matching every other
/// credential column). The device presents <c>Authorization: Device {credentialId}.{secret}</c>;
/// the id is the lookup key, the secret is verified against <see cref="CredentialHash"/>.
/// </summary>
public class DeviceCredential
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid DeviceId { get; set; }

    public string CredentialHash { get; set; } = string.Empty;

    public DeviceCredentialStatus Status { get; set; }

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset? RotatedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }
}
