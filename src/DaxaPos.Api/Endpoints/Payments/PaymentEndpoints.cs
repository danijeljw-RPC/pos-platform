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

namespace DaxaPos.Api.Endpoints.Payments;

public sealed record RecordPaymentRequest(PaymentMethod Method, decimal AmountRequested, Guid IdempotencyKey, string? ProviderReference = null, Guid? TenantId = null);

public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    Guid LocationId,
    PaymentMethod Method,
    PaymentStatus Status,
    decimal AmountRequested,
    decimal? AmountApproved,
    Guid IdempotencyKey,
    Guid? TakenByUserId,
    Guid? TakenByStaffMemberId,
    DateTimeOffset RecordedAtUtc,
    string? ProviderReference)
{
    public static PaymentResponse FromEntity(Payment payment) => new(
        payment.Id,
        payment.OrderId,
        payment.LocationId,
        payment.Method,
        payment.Status,
        payment.AmountRequested,
        payment.AmountApproved,
        payment.IdempotencyKey,
        payment.TakenByUserId,
        payment.TakenByStaffMemberId,
        payment.RecordedAtUtc,
        payment.ProviderReference);
}

/// <summary>
/// Payment foundation (PLAN-0005 Milestone B) — record cash/manual EFTPOS payments against an
/// order, list an order's payments. Gated <c>payments.record</c>, staff-PIN-eligible (matching
/// <see cref="Orders.OrderEndpoints"/>'s <c>orders.manage</c> precedent — taking a payment is core
/// counter work, same as order entry). The integrated-payment route is PLAN-0009's scope, not
/// built here: <see cref="PaymentMethod.Integrated"/> is rejected at this endpoint since no
/// <see cref="IPaymentTerminalProvider"/> adapter exists to actually call a terminal (approved
/// Human Decision #1's no-hardware-coupling boundary).
/// </summary>
/// <remarks>
/// Idempotency (ADR-0010): a retry with the same <see cref="RecordPaymentRequest.IdempotencyKey"/>
/// returns the already-recorded payment (200 OK) rather than creating a duplicate row — checked
/// before the order-state check, so a retry still succeeds even if the first attempt's payment
/// already closed the order. Settlement (<see cref="PaymentSettlement"/>): the running total of
/// <see cref="PaymentStatus.Recorded"/> payments against an order may never exceed
/// <see cref="Order.GrandTotalAmount"/> (split payments must add up to it exactly, never past it);
/// reaching it exactly transitions <see cref="Order.Status"/> to <see cref="OrderStatus.Completed"/>
/// and sets <see cref="Order.ClosedAtUtc"/> — the one place this milestone reaches back into
/// Milestone A's state machine, reusing an <see cref="OrderLifecycleDomainEvent"/> with
/// <c>Action = "Completed"</c> rather than a new event/handler.
/// </remarks>
public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders/{orderId:guid}/payments").RequireAuthorization();

        group.MapPost("", RecordAsync).RequirePermission(Permissions.PaymentsRecord);
        group.MapGet("", ListAsync).RequirePermission(Permissions.PaymentsRecord);
    }

    private static async Task<IResult> RecordAsync(
        Guid orderId,
        RecordPaymentRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.AmountRequested <= 0)
        {
            return Results.BadRequest("AmountRequested must be greater than zero.");
        }

        if (request.Method == PaymentMethod.Integrated)
        {
            return Results.BadRequest("Integrated payment requires a configured payment terminal provider, which is not yet available.");
        }

        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        // Idempotency (ADR-0010): checked before the order-state check below, so a retry of a
        // payment that already settled and closed the order still returns 200, not 409.
        var existingPayment = await dbContext.Payments.SingleOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey);
        if (existingPayment is not null)
        {
            if (existingPayment.OrderId != order.Id)
            {
                return Results.Conflict("This idempotency key was already used for a different order.");
            }

            return Results.Ok(PaymentResponse.FromEntity(existingPayment));
        }

        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
        {
            return Results.Conflict("Payments can only be recorded against an order that is open or held.");
        }

        var existingRecordedTotal = await dbContext.Payments
            .Where(p => p.OrderId == order.Id && p.Status == PaymentStatus.Recorded)
            .SumAsync(p => p.AmountApproved ?? 0m);

        if (PaymentSettlement.WouldExceedOrderTotal(existingRecordedTotal, request.AmountRequested, order.GrandTotalAmount))
        {
            return Results.BadRequest("This payment would exceed the order's grand total.");
        }

        var now = DateTimeOffset.UtcNow;

        // Cash and manual EFTPOS settle immediately — neither has an external system to await a
        // result from, so AmountApproved is set equal to AmountRequested at creation time.
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrderId = order.Id,
            LocationId = order.LocationId,
            Method = request.Method,
            Status = PaymentStatus.Recorded,
            AmountRequested = request.AmountRequested,
            AmountApproved = request.AmountRequested,
            IdempotencyKey = request.IdempotencyKey,
            TakenByUserId = authContext.UserId,
            TakenByStaffMemberId = authContext.StaffMemberId,
            RecordedAtUtc = now,
            ProviderReference = request.ProviderReference,
        };

        dbContext.Payments.Add(payment);

        dbContext.PaymentLedgerEntries.Add(new PaymentLedgerEntry
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            PaymentId = payment.Id,
            Status = PaymentStatus.Recorded,
            Amount = payment.AmountApproved.Value,
            OccurredAtUtc = now,
            Metadata = JsonSerializer.Serialize(new { payment.Method, payment.ProviderReference }),
        });

        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new PaymentLifecycleDomainEvent(
            payment.TenantId,
            order.OrganisationId,
            order.Id,
            payment.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            "Recorded",
            null,
            JsonSerializer.Serialize(new { payment.Method, payment.AmountApproved }),
            now));

        var newRecordedTotal = existingRecordedTotal + payment.AmountApproved.Value;
        if (PaymentSettlement.IsFullySettled(newRecordedTotal, order.GrandTotalAmount))
        {
            var beforeStatus = order.Status;
            order.Status = OrderStatus.Completed;
            order.ClosedAtUtc = now;
            await dbContext.SaveChangesAsync();

            await dispatcher.DispatchAsync(new OrderLifecycleDomainEvent(
                order.TenantId,
                order.OrganisationId,
                order.Id,
                authContext.UserId,
                authContext.StaffMemberId,
                "Completed",
                JsonSerializer.Serialize(new { Status = beforeStatus }),
                JsonSerializer.Serialize(new { Status = order.Status }),
                now));
        }

        return Results.Created($"/api/v1/orders/{order.Id}/payments/{payment.Id}", PaymentResponse.FromEntity(payment));
    }

    private static async Task<IResult> ListAsync(Guid orderId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        var payments = await dbContext.Payments
            .Where(p => p.OrderId == order.Id)
            .OrderBy(p => p.RecordedAtUtc)
            .ToListAsync();

        return Results.Ok(payments.Select(PaymentResponse.FromEntity));
    }

    /// <summary>Context provenance (ADR-0015): same organisation/location-bound-session rule as <see cref="Orders.OrderEndpoints"/>'s identical helper.</summary>
    private static async Task<Order?> LoadAuthorizedOrderAsync(DaxaDbContext dbContext, Guid orderId, AuthContext authContext)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null || order.OrganisationId != authContext.OrganisationId)
        {
            return null;
        }

        if (authContext.LocationId is not null && authContext.LocationId != order.LocationId)
        {
            return null;
        }

        return order;
    }
}
