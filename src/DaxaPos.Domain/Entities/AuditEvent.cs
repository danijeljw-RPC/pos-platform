namespace DaxaPos.Domain.Entities;

/// <summary>
/// An append-only audit log row (ADR-0010, <c>docs/modules/audit.md</c>). Written by in-process
/// domain-event handlers (ADR-0014) reacting to login/session/device lifecycle events — never
/// updated or deleted after creation.
/// </summary>
public class AuditEvent
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid? OrganisationId { get; set; }

    public Guid? LocationId { get; set; }

    public Guid? TerminalId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? StaffMemberId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public string? BeforeValue { get; set; }

    public string? AfterValue { get; set; }

    public string? Reason { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
