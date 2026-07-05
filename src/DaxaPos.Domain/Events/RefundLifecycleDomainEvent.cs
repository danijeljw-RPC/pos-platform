namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a <see cref="Entities.Refund"/> lifecycle action (PLAN-0005 Milestone C). <c>Action</c>
/// is <c>"Recorded"</c> in this milestone (a manually recorded refund settles immediately — there is
/// no intermediate provider state to raise a second event for, matching
/// <see cref="PaymentLifecycleDomainEvent"/>'s identical reasoning). Carries both <see cref="UserId"/>
/// and <see cref="StaffMemberId"/> for shape-consistency with every other lifecycle event in this
/// codebase, though this milestone's <c>payments.refund</c> permission is <c>rejectStaffPin: true</c>
/// — <see cref="StaffMemberId"/> is therefore never populated by any code path reachable through the
/// refund endpoints (a staff-PIN session cannot reach them at all).
/// </summary>
public sealed record RefundLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid OrderId,
    Guid PaymentId,
    Guid RefundId,
    Guid? UserId,
    Guid? StaffMemberId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
