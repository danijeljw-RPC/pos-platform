using System.Net;
using System.Net.Http.Json;
using DaxaPos.Web.Api;

namespace DaxaPos.Web.Tests.Fakes;

/// <summary>
/// A minimal in-memory stand-in for the real order API, driving a <see cref="StubHttpMessageHandler"/>
/// so <c>SalesTests</c> can exercise Milestone C.1's real open/add-line/void-line/void-order flow
/// without a live backend. Tracks a single <see cref="Order"/> at a time (matching one sales
/// screen's one active order) and recomputes totals the same simplified way every call: sum of
/// active lines' <c>LineTotalAmount</c>, zero tax (the sales screen never re-derives tax, so its
/// tests don't need real tax math to prove the UI wiring is correct).
/// </summary>
public sealed class FakeOrderBackend
{
    private readonly Dictionary<Guid, (string Name, decimal Price)> _products = [];
    private readonly Dictionary<Guid, (string Name, decimal PriceDelta)> _modifiers = [];

    public ResolvedMenuResult? Menu { get; set; }

    public OrderResult? Order { get; set; }

    public Guid? LastOpenedTerminalId { get; private set; }

    public void RegisterProduct(Guid id, string name, decimal price) => _products[id] = (name, price);

    public void RegisterModifier(Guid id, string name, decimal priceDelta) => _modifiers[id] = (name, priceDelta);

    public HttpResponseMessage Respond(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (request.Method == HttpMethod.Get && path.EndsWith("/menus/resolved", StringComparison.Ordinal))
        {
            return Json(HttpStatusCode.OK, Menu);
        }

        if (request.Method == HttpMethod.Post && path == "/api/v1/orders")
        {
            var body = ReadBody<CreateOrderRequest>(request)!;
            LastOpenedTerminalId = body.TerminalId;
            Order = new OrderResult(Guid.NewGuid(), body.TerminalId, OrderStatusResult.Open, 0m, 0m, 0m, []);
            return Json(HttpStatusCode.Created, Order);
        }

        if (request.Method == HttpMethod.Post && path.EndsWith("/lines", StringComparison.Ordinal))
        {
            var orderId = Guid.Parse(segments[3]);
            if (Order is null || Order.Id != orderId)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var body = ReadBody<AddOrderLineRequest>(request)!;
            var (name, price) = _products[body.ProductId];
            var modifiers = (body.ModifierIds ?? [])
                .Select(id => new OrderLineModifierResult(Guid.NewGuid(), id, _modifiers[id].Name, _modifiers[id].PriceDelta))
                .ToList();
            var lineTotal = price + modifiers.Sum(m => m.PriceDeltaSnapshot);
            var line = new OrderLineResult(
                Guid.NewGuid(), body.ProductId, body.Quantity, name, price, lineTotal, body.Notes, OrderLineStatusResult.Active, modifiers);

            Order = RecomputeTotals(Order with { Lines = [.. Order.Lines, line] });
            return Json(HttpStatusCode.Created, Order);
        }

        if (request.Method == HttpMethod.Delete && path.Contains("/lines/", StringComparison.Ordinal))
        {
            var orderId = Guid.Parse(segments[3]);
            var lineId = Guid.Parse(segments[5]);
            if (Order is null || Order.Id != orderId)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var lines = Order.Lines.Select(l => l.Id == lineId ? l with { Status = OrderLineStatusResult.Voided } : l).ToList();
            Order = RecomputeTotals(Order with { Lines = lines });
            return Json(HttpStatusCode.OK, Order);
        }

        if (request.Method == HttpMethod.Post && path.EndsWith("/void", StringComparison.Ordinal))
        {
            var orderId = Guid.Parse(segments[3]);
            if (Order is null || Order.Id != orderId)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            Order = Order with { Status = OrderStatusResult.Voided };
            return Json(HttpStatusCode.OK, Order);
        }

        if (request.Method == HttpMethod.Get && segments is ["api", "v1", "orders", _])
        {
            var orderId = Guid.Parse(segments[3]);
            return Order is not null && Order.Id == orderId
                ? Json(HttpStatusCode.OK, Order)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static OrderResult RecomputeTotals(OrderResult order)
    {
        var subtotal = order.Lines.Where(l => l.Status == OrderLineStatusResult.Active).Sum(l => l.LineTotalAmount);
        return order with { SubtotalAmount = subtotal, TotalTaxAmount = 0m, GrandTotalAmount = subtotal };
    }

    private static T? ReadBody<T>(HttpRequestMessage request) =>
        request.Content!.ReadFromJsonAsync<T>().GetAwaiter().GetResult();

    private static HttpResponseMessage Json<T>(HttpStatusCode statusCode, T? body) =>
        new(statusCode) { Content = JsonContent.Create(body) };
}
