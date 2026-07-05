using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Application.Payments;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Refunds;

public sealed record RecordRefundRequest(decimal Amount, string ReasonCode, string? ReasonNote = null, string? ProviderReference = null, Guid? TenantId = null);

public sealed record RefundResponse(
    Guid Id,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string ReasonCode,
    string? ReasonNote,
    Guid? RequestedByUserId,
    Guid? RequestedByStaffMemberId,
    RefundStatus Status,
    DateTimeOffset RecordedAtUtc,
    string? ProviderReference)
{
    public static RefundResponse FromEntity(Refund refund) => new(
        refund.Id,
        refund.PaymentId,
        refund.OrderId,
        refund.Amount,
        refund.ReasonCode,
        refund.ReasonNote,
        refund.RequestedByUserId,
        refund.RequestedByStaffMemberId,
        refund.Status,
        refund.RecordedAtUtc,
        refund.ProviderReference);
}

/// <summary>
/// Refund service foundation (PLAN-0005 Milestone C) — record a full/partial refund against a
/// payment, list a payment's refunds. Gated <c>payments.refund</c>, <c>rejectStaffPin: true</c> —
/// a different posture from <see cref="Payments.PaymentEndpoints"/>'s <c>payments.record</c>
/// (<c>Operational</c>, staff-PIN-eligible): refunds are manager/admin-only by default (approved
/// Human Decision #4), matching <c>catalog.manage</c>/<c>pricing.manage</c>/<c>menus.manage</c>'s
/// precedent, not <c>orders.manage</c>/<c>payments.record</c>'s.
/// </summary>
/// <remarks>
/// Per ADR-0010, a refund is a reversal record — the original <see cref="Payment"/>/<see cref="Order"/>
/// rows are never mutated (their financial fields stay exactly as they were at sale time).
/// Settlement (<see cref="RefundSettlement"/>): the running total of refunds already recorded
/// against a payment, plus this refund's amount, may never exceed
/// <see cref="Payment.AmountApproved"/> (full and partial refunds both add up against this same
/// ceiling, never past it, enforced server-side per the plan's explicit requirement).
/// </remarks>
public static class RefundEndpoints
{
    public static void MapRefundEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments/{paymentId:guid}/refunds").RequireAuthorization();

        group.MapPost("", RecordAsync).RequirePermission(Permissions.PaymentsRefund, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.PaymentsRefund, rejectStaffPin: true);
    }

    private static async Task<IResult> RecordAsync(
        Guid paymentId,
        RecordRefundRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return Results.BadRequest("ReasonCode is required.");
        }

        var authContext = authContextAccessor.Current!;
        var loaded = await LoadAuthorizedPaymentAsync(dbContext, paymentId, authContext);
        if (loaded is null)
        {
            return Results.NotFound();
        }

        var (payment, order) = loaded.Value;

        if (payment.Status != PaymentStatus.Recorded)
        {
            return Results.Conflict("Only a recorded payment can be refunded.");
        }

        var existingRefundedTotal = await dbContext.Refunds
            .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Recorded)
            .SumAsync(r => r.Amount);

        if (RefundSettlement.WouldExceedRefundableAmount(existingRefundedTotal, request.Amount, payment.AmountApproved ?? 0m))
        {
            return Results.BadRequest("This refund would exceed the payment's refundable amount.");
        }

        var now = DateTimeOffset.UtcNow;

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            PaymentId = payment.Id,
            OrderId = order.Id,
            Amount = request.Amount,
            ReasonCode = request.ReasonCode,
            ReasonNote = request.ReasonNote,
            RequestedByUserId = authContext.UserId,
            RequestedByStaffMemberId = authContext.StaffMemberId,
            Status = RefundStatus.Recorded,
            RecordedAtUtc = now,
            ProviderReference = request.ProviderReference,
        };

        dbContext.Refunds.Add(refund);

        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new RefundLifecycleDomainEvent(
            refund.TenantId,
            order.OrganisationId,
            order.Id,
            payment.Id,
            refund.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            "Recorded",
            null,
            JsonSerializer.Serialize(new { refund.PaymentId, refund.OrderId, refund.Amount, refund.ReasonCode, refund.ReasonNote }),
            now));

        return Results.Created($"/api/v1/payments/{payment.Id}/refunds/{refund.Id}", RefundResponse.FromEntity(refund));
    }

    private static async Task<IResult> ListAsync(Guid paymentId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var loaded = await LoadAuthorizedPaymentAsync(dbContext, paymentId, authContext);
        if (loaded is null)
        {
            return Results.NotFound();
        }

        var refunds = await dbContext.Refunds
            .Where(r => r.PaymentId == loaded.Value.Payment.Id)
            .OrderBy(r => r.RecordedAtUtc)
            .ToListAsync();

        return Results.Ok(refunds.Select(RefundResponse.FromEntity));
    }

    /// <summary>
    /// Context provenance (ADR-0015): <see cref="Payment"/> carries no <c>OrganisationId</c> of its
    /// own, so authorization walks through its parent <see cref="Order"/> — same organisation/
    /// location-bound-session rule as <see cref="Payments.PaymentEndpoints"/>'s identical helper.
    /// </summary>
    private static async Task<(Payment Payment, Order Order)?> LoadAuthorizedPaymentAsync(DaxaDbContext dbContext, Guid paymentId, AuthContext authContext)
    {
        var payment = await dbContext.Payments.SingleOrDefaultAsync(p => p.Id == paymentId);
        if (payment is null)
        {
            return null;
        }

        var order = await dbContext.Orders.SingleOrDefaultAsync(o => o.Id == payment.OrderId);
        if (order is null || order.OrganisationId != authContext.OrganisationId)
        {
            return null;
        }

        if (authContext.LocationId is not null && authContext.LocationId != order.LocationId)
        {
            return null;
        }

        return (payment, order);
    }
}
