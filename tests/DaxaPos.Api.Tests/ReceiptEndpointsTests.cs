using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Orders;
using DaxaPos.Api.Endpoints.Payments;
using DaxaPos.Api.Endpoints.Receipts;
using DaxaPos.Api.Endpoints.Refunds;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="ReceiptEndpoints"/> (PLAN-0005 Milestone D).
/// <c>GET .../receipt</c> is gated <c>orders.manage</c> (live-sale viewing, unaudited);
/// <c>POST .../receipt/reprint</c> is gated the new <c>receipts.reprint</c> (Operational,
/// staff-PIN-eligible like <c>orders.manage</c>/<c>payments.record</c>, unlike the AdminSensitive
/// <c>payments.refund</c>) and audited. Reuses the same venue/product/tax/payment/refund setup
/// helpers as <see cref="RefundEndpointsTests"/>.
/// </summary>
public class ReceiptEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ReceiptEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task GetReceipt_AuMixedBasket_MatchesClaudeMdWorkedExample()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxableCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "RCPT_TAXABLE", 10m, includedInPrice: true);
        var gstFreeCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "RCPT_GSTFREE", 0m, includedInPrice: true, receiptMarkerCode: "F");

        var flatWhite = await CreateProductAsync(client, caller.OrganisationId, taxableCategory.Id, 5.50m, "RCPT_FLAT_WHITE");
        var cakeSlice = await CreateProductAsync(client, caller.OrganisationId, taxableCategory.Id, 8.80m, "RCPT_CAKE_SLICE");
        var bread = await CreateProductAsync(client, caller.OrganisationId, gstFreeCategory.Id, 6.00m, "RCPT_BREAD");

        var order = (await (await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminal.Id))).Content.ReadFromJsonAsync<OrderResponse>())!;
        await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(flatWhite.Id, null, 1, null, null));
        await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(cakeSlice.Id, null, 1, null, null));
        await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(bread.Id, null, 1, null, null));

        var response = await client.GetAsync($"/api/v1/orders/{order.Id}/receipt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var receipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);
        Assert.Equal(3, receipt!.Lines.Count);
        Assert.Equal(20.30m, receipt.GrandTotalAmount);
        Assert.Equal(1.30m, receipt.TotalTaxAmount);
        Assert.Equal(19.00m, receipt.SubtotalAmount);
        Assert.Equal("Total", receipt.TotalLabel);
        Assert.Equal("Includes GST", receipt.TaxInclusiveSummaryLabel);

        var breadLine = Assert.Single(receipt.Lines, l => l.ProductName == "Product RCPT_BREAD");
        Assert.Equal("F", breadLine.TaxMarkerCode);
        Assert.Equal(6.00m, breadLine.LineTotalAmount);

        Assert.Equal(["F = GST-free"], receipt.MarkerLegend);
    }

    [Fact]
    public async Task GetReceipt_IncludesPaymentAndLinkedRefundSummary()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 20.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 20.00m);
        var refundResponse = await client.PostAsJsonAsync($"/api/v1/payments/{payment.Id}/refunds", new RecordRefundRequest(5.00m, "CustomerRequest"));
        var refund = await refundResponse.Content.ReadFromJsonAsync<RefundResponse>();

        var response = await client.GetAsync($"/api/v1/orders/{order.Id}/receipt");
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentLine = Assert.Single(receipt!.Payments);
        Assert.Equal(payment.Id, paymentLine.PaymentId);
        Assert.Equal(20.00m, paymentLine.AmountApproved);

        var refundLine = Assert.Single(receipt.Refunds);
        Assert.Equal(refund!.Id, refundLine.RefundId);
        Assert.Equal(payment.Id, refundLine.PaymentId);
        Assert.Equal(5.00m, refundLine.Amount);
        Assert.Equal("CustomerRequest", refundLine.ReasonCode);
    }

    [Fact]
    public async Task GetReceipt_Rejects_WhenOrderDoesNotExist()
    {
        var client = _factory.CreateClient();
        await SetupVenueAsync(client);

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}/receipt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReceipt_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var (callerA, _, terminalA) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, callerA.OrganisationId, terminalA.Id, 10.00m);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.GetAsync($"/api/v1/orders/{order.Id}/receipt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reprint_Succeeds_ForStaffPinSession()
    {
        var adminClient = _factory.CreateClient();
        var admin = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, admin);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, admin.OrganisationId, $"Receipt Staff Venue {Guid.NewGuid()}");
        await adminClient.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminal = (await (await adminClient.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Bar 1", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(adminClient, admin.OrganisationId, "RCPT_STAFF_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(adminClient, admin.OrganisationId, taxCategory.Id, 10.00m, "RCPT_STAFF_PRODUCT");

        var order = (await (await adminClient.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminal.Id))).Content.ReadFromJsonAsync<OrderResponse>())!;
        await adminClient.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(product.Id, null, 1, null, null));

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var staffMember = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "RCP01");
        await using (var dbContext = CreateDbContext())
        {
            var staffRoleId = (await dbContext.Roles.SingleAsync(r => r.Name == "Staff")).Id;
            await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staffMember.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        }

        var staffLogin = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "RCP01", StaffTestHelper.DefaultPin);
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffLogin.SessionToken);

        var response = await staffClient.PostAsync($"/api/v1/orders/{order.Id}/receipt/reprint", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReceipt_Fails_WithoutPermission()
    {
        // GET .../receipt is gated orders.manage, not receipts.reprint — a caller with neither must
        // still get 403 (Milestone F RBAC-inventory consolidation).
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);

        var supportClient = _factory.CreateClient();
        var supportCaller = await RbacTestSeeder.SeedAsync(supportClient, "SupportAccess", owner.TenantId);
        AuthenticateAs(supportClient, supportCaller);

        var response = await supportClient.GetAsync($"/api/v1/orders/{order.Id}/receipt");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AllEndpoints_Return403_ForDeviceToken()
    {
        // A device token is trusted device context only (ADR-0008) — empty roles/permissions by
        // design, so it must never satisfy orders.manage/receipts.reprint on either endpoint.
        var ownerClient = _factory.CreateClient();
        var (owner, location, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(ownerClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var attempts = new[]
        {
            await deviceClient.GetAsync($"/api/v1/orders/{order.Id}/receipt"),
            await deviceClient.PostAsync($"/api/v1/orders/{order.Id}/receipt/reprint", content: null),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Fact]
    public async Task Reprint_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);

        // "SupportAccess" carries devices.manage/sessions.manage only, not receipts.reprint.
        var supportClient = _factory.CreateClient();
        var supportCaller = await RbacTestSeeder.SeedAsync(supportClient, "SupportAccess", owner.TenantId);
        AuthenticateAs(supportClient, supportCaller);

        var response = await supportClient.PostAsync($"/api/v1/orders/{order.Id}/receipt/reprint", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reprint_WritesAuditEventRow_LinkedToOrder()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);

        var response = await client.PostAsync($"/api/v1/orders/{order.Id}/receipt/reprint", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var context = CreateDbContext();
        var reprintEvent = await context.AuditEvents.IgnoreQueryFilters()
            .SingleAsync(a => a.EntityId == order.Id && a.EntityType == "Receipt");

        Assert.Equal("ReceiptReprinted", reprintEvent.EventType);
    }

    private async Task<(SeededCaller Caller, LocationResponse Location, TerminalResponse Terminal)> SetupVenueAsync(HttpClient client)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Receipt Venue {Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminalResponse = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Front Counter", location.Id));
        var terminal = (await terminalResponse.Content.ReadFromJsonAsync<TerminalResponse>())!;
        return (caller, location, terminal);
    }

    private static async Task<OrderResponse> OpenOrderWithSingleLineAsync(HttpClient client, Guid organisationId, Guid terminalId, decimal productPrice)
    {
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, organisationId, $"RCPT_{Guid.NewGuid():N}", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, organisationId, taxCategory.Id, productPrice, $"RCPT_PRODUCT_{Guid.NewGuid():N}");

        var order = (await (await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminalId))).Content.ReadFromJsonAsync<OrderResponse>())!;
        var withLine = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(product.Id, null, 1, null, null));
        return (await withLine.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static async Task<PaymentResponse> RecordAndParsePaymentAsync(HttpClient client, Guid orderId, PaymentMethod method, decimal amount)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/payments", new RecordPaymentRequest(method, amount, Guid.NewGuid()));
        return (await response.Content.ReadFromJsonAsync<PaymentResponse>())!;
    }

    private static async Task<TaxCategoryResponse> CreateTaxCategoryWithDefinitionAsync(
        HttpClient client, Guid organisationId, string codeSuffix, decimal ratePercent, bool includedInPrice, string? receiptMarkerCode = null)
    {
        var taxCategoryResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-categories",
            new CreateTaxCategoryRequest($"TAXCAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        var taxCategory = (await taxCategoryResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;

        var taxDefinitionResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-definitions",
            new CreateTaxDefinitionRequest(
                $"TAXDEF_{codeSuffix}", "GST", organisationId, "AU", null, ratePercent, "Australia", TaxJurisdictionType.Country,
                includedInPrice, TaxRoundingMode.NearestCent, 2, TaxCalculationScope.PerLine,
                receiptMarkerCode, receiptMarkerCode is null ? null : "GST-free", null));
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

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
