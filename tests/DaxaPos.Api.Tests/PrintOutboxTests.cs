using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Orders;
using DaxaPos.Api.Endpoints.Payments;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Application.Printing;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Infrastructure.Printing;
using DaxaPos.Persistence;
using DaxaPos.Workers.Processing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// PLAN-0005 Milestone E. Proves the concrete shape of ADR-0014's Handler I/O Rule end to end:
/// fully settling a payment enqueues a durable <c>"PrintReceipt"</c> <see cref="OutboxWorkItem"/>
/// row rather than printing inline (<see cref="RecordPayment_FullySettlesOrder_EnqueuesPrintReceiptOutboxRow"/>),
/// and <see cref="PrintReceiptOutboxProcessor"/> (the class <c>DaxaPos.Workers</c>' polling loop
/// delegates to) correctly processes, retries, and eventually permanently fails a work item against
/// a fake <see cref="IPrinterTransport"/> — no physical printer or running worker host required.
/// </summary>
public class PrintOutboxTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public PrintOutboxTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task RecordPayment_FullySettlesOrder_EnqueuesPrintReceiptOutboxRow()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 20.30m);

        var response = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 20.30m, Guid.NewGuid());
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        await using var context = CreateDbContext(caller.TenantId);
        var workItem = await context.OutboxWorkItems
            .SingleAsync(w => w.WorkType == PrintReceiptWorkPayload.WorkType);

        Assert.Equal(OutboxWorkItemStatus.Pending, workItem.Status);
        Assert.Equal(0, workItem.AttemptCount);
        var payload = JsonSerializer.Deserialize<PrintReceiptWorkPayload>(workItem.PayloadJson);
        Assert.Equal(order.Id, payload!.OrderId);
    }

    [Fact]
    public async Task ProcessAsync_Success_MarksCompleted_AndSendsBytesThroughTransport()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m, Guid.NewGuid());

        await using var context = CreateDbContext(caller.TenantId);
        var workItem = await context.OutboxWorkItems.SingleAsync(w => w.WorkType == PrintReceiptWorkPayload.WorkType);

        var transport = new RecordingPrinterTransport();
        var processor = new PrintReceiptOutboxProcessor(transport);

        await processor.ProcessAsync(context, workItem, DateTimeOffset.UtcNow);

        Assert.Equal(OutboxWorkItemStatus.Completed, workItem.Status);
        Assert.Equal(1, workItem.AttemptCount);
        Assert.NotNull(workItem.ProcessedAtUtc);
        Assert.Null(workItem.LastError);
        Assert.Single(transport.SentPayloads);
    }

    [Fact]
    public async Task ProcessAsync_TransientFailureThenSuccess_RetriesAndEventuallyCompletes()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m, Guid.NewGuid());

        await using var context = CreateDbContext(caller.TenantId);
        var workItem = await context.OutboxWorkItems.SingleAsync(w => w.WorkType == PrintReceiptWorkPayload.WorkType);

        var transport = new RecordingPrinterTransport(failuresBeforeSuccess: 1);
        var processor = new PrintReceiptOutboxProcessor(transport);

        var beforeFirstAttempt = DateTimeOffset.UtcNow;
        await processor.ProcessAsync(context, workItem, beforeFirstAttempt);

        Assert.Equal(OutboxWorkItemStatus.Pending, workItem.Status);
        Assert.Equal(1, workItem.AttemptCount);
        Assert.NotNull(workItem.LastError);
        Assert.True(workItem.NextAttemptAtUtc > beforeFirstAttempt);

        await processor.ProcessAsync(context, workItem, DateTimeOffset.UtcNow);

        Assert.Equal(OutboxWorkItemStatus.Completed, workItem.Status);
        Assert.Equal(2, workItem.AttemptCount);
        Assert.Null(workItem.LastError);
    }

    [Fact]
    public async Task ProcessAsync_ExhaustsMaxAttempts_MarksPermanentlyFailed()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m, Guid.NewGuid());

        await using var context = CreateDbContext(caller.TenantId);
        var workItem = await context.OutboxWorkItems.SingleAsync(w => w.WorkType == PrintReceiptWorkPayload.WorkType);
        workItem.MaxAttempts = 1;
        await context.SaveChangesAsync();

        var transport = new RecordingPrinterTransport(failuresBeforeSuccess: int.MaxValue);
        var processor = new PrintReceiptOutboxProcessor(transport);

        await processor.ProcessAsync(context, workItem, DateTimeOffset.UtcNow);

        Assert.Equal(OutboxWorkItemStatus.Failed, workItem.Status);
        Assert.Equal(1, workItem.AttemptCount);
        Assert.NotNull(workItem.LastError);
    }

    private sealed class RecordingPrinterTransport(int failuresBeforeSuccess = 0) : IPrinterTransport
    {
        private int _callCount;
        public List<byte[]> SentPayloads { get; } = [];

        public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            _callCount++;
            if (_callCount <= failuresBeforeSuccess)
            {
                throw new InvalidOperationException("Simulated transient printer failure.");
            }

            SentPayloads.Add(data);
            return Task.CompletedTask;
        }
    }

    private async Task<(SeededCaller Caller, LocationResponse Location, TerminalResponse Terminal)> SetupVenueAsync(HttpClient client)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Print Venue {Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminalResponse = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Front Counter", location.Id));
        var terminal = (await terminalResponse.Content.ReadFromJsonAsync<TerminalResponse>())!;
        return (caller, location, terminal);
    }

    private static async Task<OrderResponse> OpenOrderWithSingleLineAsync(HttpClient client, Guid organisationId, Guid terminalId, decimal productPrice)
    {
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, organisationId, $"PRINT_{Guid.NewGuid():N}", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, organisationId, taxCategory.Id, productPrice, $"PRINT_PRODUCT_{Guid.NewGuid():N}");

        var order = (await (await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminalId))).Content.ReadFromJsonAsync<OrderResponse>())!;
        var withLine = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(product.Id, null, 1, null, null));
        return (await withLine.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static Task<HttpResponseMessage> RecordPaymentAsync(HttpClient client, Guid orderId, PaymentMethod method, decimal amount, Guid idempotencyKey) =>
        client.PostAsJsonAsync($"/api/v1/orders/{orderId}/payments", new RecordPaymentRequest(method, amount, idempotencyKey));

    private static async Task<TaxCategoryResponse> CreateTaxCategoryWithDefinitionAsync(
        HttpClient client, Guid organisationId, string codeSuffix, decimal ratePercent, bool includedInPrice)
    {
        var taxCategoryResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-categories",
            new CreateTaxCategoryRequest($"TAXCAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        var taxCategory = (await taxCategoryResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;

        var taxDefinitionResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-definitions",
            new CreateTaxDefinitionRequest(
                $"TAXDEF_{codeSuffix}", "GST", organisationId, "AU", null, ratePercent, "Australia", TaxJurisdictionType.Country,
                includedInPrice, TaxRoundingMode.NearestCent, 2, TaxCalculationScope.PerLine, null, null, null));
        var taxDefinition = (await taxDefinitionResponse.Content.ReadFromJsonAsync<TaxDefinitionResponse>())!;

        await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(taxCategory.Id, taxDefinition.Id, null, 0));

        return taxCategory;
    }

    private static async Task<ProductResponse> CreateProductAsync(HttpClient client, Guid organisationId, Guid taxCategoryId, decimal basePrice, string codeSuffix)
    {
        var categoryResponse = await client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest($"Category {codeSuffix}", 0, organisationId));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"Product {codeSuffix}", organisationId, category.Id, taxCategoryId, null, null, null, basePrice));
        return (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext(Guid tenantId) =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(tenantId));
}
