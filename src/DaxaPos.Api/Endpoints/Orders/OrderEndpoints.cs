using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Application.Orders;
using DaxaPos.Application.Pricing;
using DaxaPos.Application.Tax;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Orders;

public sealed record CreateOrderRequest(Guid TerminalId, string? Notes = null, Guid? TenantId = null);

public sealed record AddOrderLineRequest(Guid ProductId, Guid? ProductVariantId, int Quantity, IReadOnlyList<Guid>? ModifierIds, string? Notes);

public sealed record OrderLineTaxResponse(
    Guid Id,
    Guid TaxDefinitionId,
    string TaxNameSnapshot,
    decimal RatePercentSnapshot,
    decimal TaxableAmount,
    decimal TaxAmount,
    string JurisdictionNameSnapshot,
    TaxJurisdictionType JurisdictionTypeSnapshot,
    string? ReceiptMarkerCodeSnapshot,
    string? ReceiptMarkerLabelSnapshot);

public sealed record OrderLineModifierResponse(Guid Id, Guid ModifierId, string NameSnapshot, decimal PriceDeltaSnapshot);

public sealed record OrderLineResponse(
    Guid Id,
    Guid ProductId,
    Guid? ProductVariantId,
    int Quantity,
    string ProductNameSnapshot,
    decimal UnitPriceSnapshot,
    decimal LineSubtotalAmount,
    decimal LineTotalAmount,
    string TaxCategoryCodeSnapshot,
    string? Notes,
    OrderLineStatus Status,
    DateTimeOffset? VoidedAtUtc,
    string? VoidReason,
    IReadOnlyList<OrderLineModifierResponse> Modifiers,
    IReadOnlyList<OrderLineTaxResponse> Taxes);

public sealed record OrderResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid TerminalId,
    long OrderNumber,
    OrderStatus Status,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string? Notes,
    bool IsTaxInclusivePricing,
    decimal SubtotalAmount,
    decimal TotalTaxAmount,
    decimal GrandTotalAmount,
    IReadOnlyList<OrderLineResponse> Lines);

/// <summary>
/// Order service foundation (PLAN-0005 Milestone A) — create/read/list orders, add/void lines,
/// hold/resume/void/cancel the whole order. Gated <c>orders.manage</c>, staff-PIN-eligible
/// (<c>rejectStaffPin</c> defaults to <c>false</c>) — order entry is core POS counter work, the
/// first plan whose write endpoints are staff-accessible from day one (unlike PLAN-0004's
/// catalogue writes, which stayed admin-only until Milestone F's sold-out toggle). Reuses
/// PLAN-0004's <see cref="PriceResolver"/> and <see cref="TaxCalculationEngine"/> directly — this
/// module does not re-derive pricing or tax calculation, only order-level aggregation on top of
/// them, and is where ADR-0006's 20-distinct-tax-component-per-order limit
/// (<see cref="OrderTaxAggregation"/>) is finally enforced (PLAN-0004 deliberately left it
/// unenforced since <see cref="Order"/> didn't exist yet).
/// </summary>
/// <remarks>
/// Tax category resolution precedence matches the resolved-menu endpoint's approved merge rule
/// (Human Decision #7 from PLAN-0004): a location-specific <see cref="TaxCategoryDefinition"/> set
/// wins entirely over the organisation-wide set for the same <see cref="TaxCategory"/> — never a
/// partial per-component merge. <see cref="Order.OrderNumber"/> is allocated via a single atomic
/// <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> against <see cref="OrderNumberCounter"/>
/// (approved Human Decision #2) — never a computed <c>MAX + 1</c>, which would race under
/// concurrent order-open calls at the same location the same way OI-0013/OI-0017 do.
/// </remarks>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders").RequireAuthorization();

        group.MapPost("", OpenAsync).RequirePermission(Permissions.OrdersManage);
        group.MapGet("", ListAsync).RequirePermission(Permissions.OrdersManage);
        group.MapGet("/{orderId:guid}", GetByIdAsync).RequirePermission(Permissions.OrdersManage);
        group.MapPost("/{orderId:guid}/lines", AddLineAsync).RequirePermission(Permissions.OrdersManage);
        group.MapDelete("/{orderId:guid}/lines/{lineId:guid}", VoidLineAsync).RequirePermission(Permissions.OrdersManage);
        group.MapPost("/{orderId:guid}/hold", HoldAsync).RequirePermission(Permissions.OrdersManage);
        group.MapPost("/{orderId:guid}/resume", ResumeAsync).RequirePermission(Permissions.OrdersManage);
        group.MapPost("/{orderId:guid}/void", VoidOrderAsync).RequirePermission(Permissions.OrdersManage);
        group.MapPost("/{orderId:guid}/cancel", CancelOrderAsync).RequirePermission(Permissions.OrdersManage);
    }

    private static async Task<IResult> OpenAsync(
        CreateOrderRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var authContext = authContextAccessor.Current!;

        // Context provenance (ADR-0015): Terminal -> Location -> Organisation, matching
        // TerminalEndpoints' own walk of its one parent relationship. A mismatch is 404, never a
        // validation error.
        var terminal = await dbContext.Terminals.SingleOrDefaultAsync(t => t.Id == request.TerminalId);
        if (terminal is null)
        {
            return Results.NotFound();
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == terminal.LocationId);
        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        // A location-bound session (staff PIN, from its device's registered location) may only
        // open orders at its own location — matching ResolvedMenuEndpoints'/ProductSoldOutEndpoints'
        // identical rule.
        if (authContext.LocationId is not null && authContext.LocationId != location.Id)
        {
            return Results.NotFound();
        }

        // Milestone C.2 (ADR-0015 Context Provenance): a location-bound session must open orders
        // only for its own resolved Terminal — never a different terminal at the same location via
        // a client-supplied TerminalId, and never any terminal at all if this device isn't yet
        // assigned to one. Same "mismatch is 404, never a validation error" convention as every
        // other context-provenance check in this file.
        if (authContext.LocationId is not null && (authContext.TerminalId is null || authContext.TerminalId != terminal.Id))
        {
            return Results.NotFound();
        }

        // Fail-closed (approved PLAN-0004 Human Decision #5 precedent, reused here): an order
        // cannot open at a location with no tax configuration, since IsTaxInclusivePricing must be
        // snapshotted from a real configuration, never silently defaulted.
        var venueTaxConfiguration = await dbContext.VenueTaxConfigurations.SingleOrDefaultAsync(v => v.LocationId == location.Id);
        if (venueTaxConfiguration is null)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var orderNumber = await AllocateOrderNumberAsync(dbContext, authContext.TenantId, location.Id);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = location.OrganisationId,
            LocationId = location.Id,
            TerminalId = terminal.Id,
            OrderNumber = orderNumber,
            Status = OrderStatus.Open,
            OpenedAtUtc = now,
            OpenedByUserId = authContext.UserId,
            OpenedByStaffMemberId = authContext.StaffMemberId,
            Notes = request.Notes,
            IsTaxInclusivePricing = venueTaxConfiguration.TaxInclusivePricing,
            SubtotalAmount = 0m,
            TotalTaxAmount = 0m,
            GrandTotalAmount = 0m,
            CreatedAtUtc = now,
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrderLifecycleDomainEvent(
            order.TenantId,
            order.OrganisationId,
            order.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            "Opened",
            null,
            JsonSerializer.Serialize(new { order.OrderNumber, order.LocationId, order.TerminalId }),
            now));

        return Results.Created($"/api/v1/orders/{order.Id}", await BuildOrderResponseAsync(dbContext, order));
    }

    private static async Task<IResult> ListAsync(
        Guid? locationId,
        OrderStatus? status,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        // A location-bound session always sees only its own location — the query-string locationId
        // is only meaningful for an organisation-scoped (LocationId == null) admin session.
        var effectiveLocationId = authContext.LocationId ?? locationId;

        var orders = await dbContext.Orders
            .Where(o => o.OrganisationId == authContext.OrganisationId
                && (effectiveLocationId == null || o.LocationId == effectiveLocationId)
                && (status == null || o.Status == status))
            .OrderByDescending(o => o.OpenedAtUtc)
            .ToListAsync();

        var responses = new List<OrderResponse>();
        foreach (var order in orders)
        {
            responses.Add(await BuildOrderResponseAsync(dbContext, order));
        }

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetByIdAsync(Guid orderId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);

        return order is null ? Results.NotFound() : Results.Ok(await BuildOrderResponseAsync(dbContext, order));
    }

    private static async Task<IResult> AddLineAsync(
        Guid orderId,
        AddOrderLineRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
        {
            return Results.Conflict("Lines can only be added to an order that is open or held.");
        }

        if (request.Quantity < 1)
        {
            return Results.BadRequest("Quantity must be at least 1.");
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        // Reject sold-out/unavailable/inactive/archived at add-line time (plan's explicit bullet) —
        // IsActive/IsArchived first, ProductLocationOverride's IsAvailable/IsSoldOut below once the
        // location's override (if any) is loaded.
        if (!product.IsActive || product.IsArchived)
        {
            return Results.BadRequest("Product is not available for sale.");
        }

        ProductVariant? variant = null;
        if (request.ProductVariantId is not null)
        {
            variant = await dbContext.ProductVariants.SingleOrDefaultAsync(v => v.Id == request.ProductVariantId);
            if (variant is null || variant.ProductId != product.Id || !variant.IsActive)
            {
                return Results.BadRequest("ProductVariantId does not identify an active variant of this product.");
            }
        }

        var linkedModifierGroups = await (
            from pmg in dbContext.ProductModifierGroups
            join g in dbContext.ModifierGroups on pmg.ModifierGroupId equals g.Id
            where pmg.ProductId == product.Id && g.IsActive
            select g).ToListAsync();
        var linkedModifierGroupIds = linkedModifierGroups.Select(g => g.Id).ToHashSet();

        var modifiers = new List<Modifier>();
        if (request.ModifierIds is { Count: > 0 })
        {
            modifiers = await dbContext.Modifiers
                .Where(m => request.ModifierIds.Contains(m.Id))
                .ToListAsync();

            if (modifiers.Count != request.ModifierIds.Count
                || modifiers.Any(m => !m.IsActive || !linkedModifierGroupIds.Contains(m.ModifierGroupId)))
            {
                return Results.BadRequest("One or more ModifierIds are invalid, inactive, or not linked to this product.");
            }
        }

        // Milestone C.1: server-side backstop for modifier group cardinality — the sales-screen
        // modifier modal is the primary UX, this enforces it independently (this codebase's
        // established "server remains authoritative" rule, already applied here to tax/pricing).
        foreach (var group in linkedModifierGroups)
        {
            var selectedCount = modifiers.Count(m => m.ModifierGroupId == group.Id);

            if (group.IsRequired && selectedCount == 0)
            {
                return Results.BadRequest($"'{group.Name}' requires at least one selection.");
            }

            if (selectedCount < group.SelectionMin || selectedCount > group.SelectionMax)
            {
                return Results.BadRequest(
                    $"'{group.Name}' requires between {group.SelectionMin} and {group.SelectionMax} selection(s); {selectedCount} were provided.");
            }
        }

        var locationOverride = await dbContext.ProductLocationOverrides
            .SingleOrDefaultAsync(o => o.LocationId == order.LocationId && o.ProductId == product.Id);
        if (locationOverride is not null && (!locationOverride.IsAvailable || locationOverride.IsSoldOut))
        {
            return Results.BadRequest("Product is not available for sale at this location.");
        }

        // Re-resolved per line-add, not reused from Order.IsTaxInclusivePricing — PriceResolver
        // needs the full VenueTaxConfiguration object, and re-checking here keeps this endpoint
        // fail-closed against a future deletion path even though none exists yet (plan's explicit
        // line-add-flow bullet).
        var venueTaxConfiguration = await dbContext.VenueTaxConfigurations.SingleOrDefaultAsync(v => v.LocationId == order.LocationId);
        if (venueTaxConfiguration is null)
        {
            return Results.NotFound();
        }

        var priceResult = PriceResolver.Resolve(product, variant, modifiers, locationOverride, venueTaxConfiguration);
        if (!priceResult.IsSuccess)
        {
            return Results.NotFound();
        }

        var unitPrice = priceResult.ResolvedPrice!.Amount;
        var lineAmountBeforeTax = unitPrice * request.Quantity;

        var taxCategoryDefinitions = await ResolveTaxCategoryDefinitionsAsync(dbContext, product.TaxCategoryId, order.LocationId);
        var taxDefinitionIds = taxCategoryDefinitions.Select(d => d.TaxDefinitionId).Distinct().ToList();
        var taxDefinitions = await dbContext.TaxDefinitions
            .Where(t => taxDefinitionIds.Contains(t.Id) && t.IsActive)
            .ToListAsync();

        // Fail closed: a mapped TaxDefinition that no longer exists/is inactive, or no mapping at
        // all, must never silently resolve to zero tax components.
        if (taxDefinitions.Count != taxDefinitionIds.Count)
        {
            return Results.NotFound();
        }

        var components = taxDefinitions
            .Select(t => new TaxComponentSnapshot(t.Id, t.Name, t.RatePercent, t.JurisdictionName, t.JurisdictionType, t.IncludedInPrice, t.RoundingMode, t.RoundingPrecision))
            .ToList();

        var taxResult = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(lineAmountBeforeTax, components));
        if (!taxResult.IsSuccess)
        {
            return Results.NotFound();
        }

        var existingActiveLineTaxDefinitionIds = await dbContext.OrderLineTaxes
            .Where(t => dbContext.OrderLines
                .Where(l => l.OrderId == order.Id && l.Status == OrderLineStatus.Active)
                .Select(l => l.Id)
                .Contains(t.OrderLineId))
            .Select(t => t.TaxDefinitionId)
            .ToListAsync();

        var distinctComponentCount = OrderTaxAggregation.CountDistinctTaxComponents(
            [existingActiveLineTaxDefinitionIds, components.Select(c => c.TaxDefinitionId).ToList()]);

        if (OrderTaxAggregation.ExceedsLimit(distinctComponentCount))
        {
            return Results.BadRequest($"Adding this line would exceed ADR-0006's {OrderTaxAggregation.MaxComponentsPerOrder}-distinct-tax-component-per-order limit.");
        }

        // Inclusive-component tax is already embedded in lineAmountBeforeTax (nothing extra to
        // charge); exclusive-component tax adds on top of it — see this file's class remarks.
        var extraExclusiveTax = components.Zip(taxResult.TaxLines)
            .Where(pair => !pair.First.IncludedInPrice)
            .Sum(pair => pair.Second.TaxAmount);

        var lineTotalAmount = lineAmountBeforeTax + extraExclusiveTax;
        var lineSubtotalAmount = lineTotalAmount - taxResult.TotalTaxAmount;

        var taxCategory = await dbContext.TaxCategories.SingleAsync(c => c.Id == product.TaxCategoryId);
        var now = DateTimeOffset.UtcNow;

        var orderLine = new OrderLine
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrderId = order.Id,
            ProductId = product.Id,
            ProductVariantId = variant?.Id,
            Quantity = request.Quantity,
            ProductNameSnapshot = product.Name,
            UnitPriceSnapshot = unitPrice,
            LineSubtotalAmount = lineSubtotalAmount,
            LineTotalAmount = lineTotalAmount,
            TaxCategoryCodeSnapshot = taxCategory.Code,
            Notes = request.Notes,
            Status = OrderLineStatus.Active,
            CreatedAtUtc = now,
        };

        dbContext.OrderLines.Add(orderLine);

        foreach (var modifier in modifiers)
        {
            dbContext.OrderLineModifiers.Add(new OrderLineModifier
            {
                Id = Guid.NewGuid(),
                TenantId = authContext.TenantId,
                OrderLineId = orderLine.Id,
                ModifierId = modifier.Id,
                NameSnapshot = modifier.Name,
                PriceDeltaSnapshot = modifier.PriceDelta,
                CreatedAtUtc = now,
            });
        }

        foreach (var taxLine in taxResult.TaxLines)
        {
            var definition = taxDefinitions.Single(t => t.Id == taxLine.TaxDefinitionId);
            dbContext.OrderLineTaxes.Add(new OrderLineTax
            {
                Id = Guid.NewGuid(),
                TenantId = authContext.TenantId,
                OrderLineId = orderLine.Id,
                TaxDefinitionId = taxLine.TaxDefinitionId,
                TaxNameSnapshot = taxLine.TaxName,
                RatePercentSnapshot = taxLine.RatePercent,
                TaxableAmount = taxLine.TaxableAmount,
                TaxAmount = taxLine.TaxAmount,
                JurisdictionNameSnapshot = taxLine.JurisdictionName,
                JurisdictionTypeSnapshot = taxLine.JurisdictionType,
                ReceiptMarkerCodeSnapshot = definition.ReceiptMarkerCode,
                ReceiptMarkerLabelSnapshot = definition.ReceiptMarkerLabel,
                CreatedAtUtc = now,
            });
        }

        await dbContext.SaveChangesAsync();
        await RecomputeOrderTotalsAsync(dbContext, order);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrderLineChangedDomainEvent(
            order.TenantId,
            order.OrganisationId,
            order.Id,
            orderLine.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            "LineAdded",
            null,
            JsonSerializer.Serialize(new { orderLine.ProductNameSnapshot, orderLine.Quantity, orderLine.LineTotalAmount }),
            now));

        return Results.Created($"/api/v1/orders/{order.Id}", await BuildOrderResponseAsync(dbContext, order));
    }

    private static async Task<IResult> VoidLineAsync(
        Guid orderId,
        Guid lineId,
        string? reason,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
        {
            return Results.Conflict("Lines can only be voided on an order that is open or held.");
        }

        var line = await dbContext.OrderLines.SingleOrDefaultAsync(l => l.Id == lineId && l.OrderId == order.Id);
        if (line is null)
        {
            return Results.NotFound();
        }

        if (line.Status == OrderLineStatus.Voided)
        {
            return Results.Conflict("This line is already voided.");
        }

        var now = DateTimeOffset.UtcNow;
        line.Status = OrderLineStatus.Voided;
        line.VoidedAtUtc = now;
        line.VoidedByUserId = authContext.UserId;
        line.VoidedByStaffMemberId = authContext.StaffMemberId;
        line.VoidReason = reason;

        // Save the void first — RecomputeOrderTotalsAsync issues a fresh DB query (not a local
        // in-memory filter), so it must run after the status change is persisted, otherwise it
        // would still see this line as Active (matching AddLineAsync's save-then-recompute order).
        await dbContext.SaveChangesAsync();
        await RecomputeOrderTotalsAsync(dbContext, order);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrderLineChangedDomainEvent(
            order.TenantId,
            order.OrganisationId,
            order.Id,
            line.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            "LineVoided",
            null,
            JsonSerializer.Serialize(new { reason }),
            now));

        return Results.Ok(await BuildOrderResponseAsync(dbContext, order));
    }

    private static Task<IResult> HoldAsync(Guid orderId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext, IDomainEventDispatcher dispatcher) =>
        TransitionAsync(orderId, OrderStatus.Open, OrderStatus.Held, "Held", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ResumeAsync(Guid orderId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext, IDomainEventDispatcher dispatcher) =>
        TransitionAsync(orderId, OrderStatus.Held, OrderStatus.Open, "Resumed", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> VoidOrderAsync(Guid orderId, string? reason, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext, IDomainEventDispatcher dispatcher) =>
        CloseFromOpenOrHeldAsync(orderId, OrderStatus.Voided, "Voided", reason, authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> CancelOrderAsync(Guid orderId, string? reason, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext, IDomainEventDispatcher dispatcher) =>
        CloseFromOpenOrHeldAsync(orderId, OrderStatus.Cancelled, "Cancelled", reason, authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> TransitionAsync(
        Guid orderId,
        OrderStatus requiredCurrentStatus,
        OrderStatus newStatus,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        if (order.Status != requiredCurrentStatus)
        {
            return Results.Conflict($"Order must be {requiredCurrentStatus} to become {newStatus}.");
        }

        var beforeStatus = order.Status;
        order.Status = newStatus;
        var now = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrderLifecycleDomainEvent(
            order.TenantId,
            order.OrganisationId,
            order.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            action,
            JsonSerializer.Serialize(new { Status = beforeStatus }),
            JsonSerializer.Serialize(new { Status = newStatus }),
            now));

        return Results.Ok(await BuildOrderResponseAsync(dbContext, order));
    }

    private static async Task<IResult> CloseFromOpenOrHeldAsync(
        Guid orderId,
        OrderStatus newStatus,
        string action,
        string? reason,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        if (order.Status is not (OrderStatus.Open or OrderStatus.Held))
        {
            return Results.Conflict($"Order must be Open or Held to become {newStatus}.");
        }

        var beforeStatus = order.Status;
        var now = DateTimeOffset.UtcNow;
        order.Status = newStatus;
        order.ClosedAtUtc = now;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new OrderLifecycleDomainEvent(
            order.TenantId,
            order.OrganisationId,
            order.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            action,
            JsonSerializer.Serialize(new { Status = beforeStatus }),
            JsonSerializer.Serialize(new { Status = newStatus, reason }),
            now));

        return Results.Ok(await BuildOrderResponseAsync(dbContext, order));
    }

    /// <summary>Context provenance (ADR-0015): organisation match, then the same location-bound-session rule every other Milestone A endpoint applies.</summary>
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

        // Milestone C.2 (ADR-0015 Context Provenance): a location-bound session (staff-PIN or
        // device-token) is scoped to its own resolved Terminal, never another terminal's order at
        // the same location — and never any order at all if this device isn't yet assigned to a
        // terminal (matches the C.1 "no fake/null TerminalId" product decision: a null TerminalId
        // here means "not linked", not "unrestricted"). An organisation-scoped admin session
        // (LocationId null) is unrestricted, same as the location check above.
        if (authContext.LocationId is not null && (authContext.TerminalId is null || authContext.TerminalId != order.TerminalId))
        {
            return null;
        }

        return order;
    }

    /// <summary>
    /// Location-specific mappings win entirely over organisation-wide ones for the same
    /// <see cref="TaxCategory"/> — matching the resolved-menu endpoint's approved merge precedence
    /// (PLAN-0004 Human Decision #7), never a partial per-component merge.
    /// </summary>
    private static async Task<List<TaxCategoryDefinition>> ResolveTaxCategoryDefinitionsAsync(DaxaDbContext dbContext, Guid taxCategoryId, Guid locationId)
    {
        var candidates = await dbContext.TaxCategoryDefinitions
            .Where(d => d.TaxCategoryId == taxCategoryId && d.IsActive && (d.LocationId == null || d.LocationId == locationId))
            .OrderBy(d => d.Priority)
            .ToListAsync();

        var locationSpecific = candidates.Where(d => d.LocationId == locationId).ToList();
        return locationSpecific.Count > 0 ? locationSpecific : candidates.Where(d => d.LocationId == null).ToList();
    }

    private static async Task RecomputeOrderTotalsAsync(DaxaDbContext dbContext, Order order)
    {
        var activeLines = await dbContext.OrderLines
            .Where(l => l.OrderId == order.Id && l.Status == OrderLineStatus.Active)
            .ToListAsync();

        order.SubtotalAmount = activeLines.Sum(l => l.LineSubtotalAmount);
        order.TotalTaxAmount = activeLines.Sum(l => l.LineTotalAmount - l.LineSubtotalAmount);
        order.GrandTotalAmount = activeLines.Sum(l => l.LineTotalAmount);
    }

    /// <summary>
    /// Atomic allocate-and-increment against <see cref="OrderNumberCounter"/> — a single
    /// <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> round-trip, safe under concurrent
    /// order-open calls at the same location (approved Human Decision #2). The row is
    /// upserted on first use, never pre-provisioned by a location-create workflow.
    /// </summary>
    private static async Task<long> AllocateOrderNumberAsync(DaxaDbContext dbContext, Guid tenantId, Guid locationId)
    {
        var allocated = await dbContext.Database.SqlQuery<long>(
            $"""
            INSERT INTO order_number_counters ("LocationId", "TenantId", "NextValue")
            VALUES ({locationId}, {tenantId}, 2)
            ON CONFLICT ("LocationId") DO UPDATE SET "NextValue" = order_number_counters."NextValue" + 1
            RETURNING order_number_counters."NextValue" - 1
            """).ToListAsync();

        return allocated[0];
    }

    private static async Task<OrderResponse> BuildOrderResponseAsync(DaxaDbContext dbContext, Order order)
    {
        var lines = await dbContext.OrderLines
            .Where(l => l.OrderId == order.Id)
            .OrderBy(l => l.CreatedAtUtc)
            .ToListAsync();

        var lineIds = lines.Select(l => l.Id).ToList();

        var modifiers = await dbContext.OrderLineModifiers
            .Where(m => lineIds.Contains(m.OrderLineId))
            .ToListAsync();

        var taxes = await dbContext.OrderLineTaxes
            .Where(t => lineIds.Contains(t.OrderLineId))
            .ToListAsync();

        var lineResponses = lines.Select(line => new OrderLineResponse(
            line.Id,
            line.ProductId,
            line.ProductVariantId,
            line.Quantity,
            line.ProductNameSnapshot,
            line.UnitPriceSnapshot,
            line.LineSubtotalAmount,
            line.LineTotalAmount,
            line.TaxCategoryCodeSnapshot,
            line.Notes,
            line.Status,
            line.VoidedAtUtc,
            line.VoidReason,
            modifiers.Where(m => m.OrderLineId == line.Id)
                .Select(m => new OrderLineModifierResponse(m.Id, m.ModifierId, m.NameSnapshot, m.PriceDeltaSnapshot))
                .ToList(),
            taxes.Where(t => t.OrderLineId == line.Id)
                .Select(t => new OrderLineTaxResponse(
                    t.Id, t.TaxDefinitionId, t.TaxNameSnapshot, t.RatePercentSnapshot, t.TaxableAmount, t.TaxAmount,
                    t.JurisdictionNameSnapshot, t.JurisdictionTypeSnapshot, t.ReceiptMarkerCodeSnapshot, t.ReceiptMarkerLabelSnapshot))
                .ToList()))
            .ToList();

        return new OrderResponse(
            order.Id,
            order.TenantId,
            order.OrganisationId,
            order.LocationId,
            order.TerminalId,
            order.OrderNumber,
            order.Status,
            order.OpenedAtUtc,
            order.ClosedAtUtc,
            order.Notes,
            order.IsTaxInclusivePricing,
            order.SubtotalAmount,
            order.TotalTaxAmount,
            order.GrandTotalAmount,
            lineResponses);
    }
}
