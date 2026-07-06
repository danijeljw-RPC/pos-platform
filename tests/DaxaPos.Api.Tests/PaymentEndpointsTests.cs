using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Orders;
using DaxaPos.Api.Endpoints.Payments;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="PaymentEndpoints"/> (PLAN-0005 Milestone B).
/// <c>payments.record</c> is <c>Operational</c> (staff-PIN-eligible), matching
/// <see cref="OrderEndpointsTests"/>'s <c>orders.manage</c> precedent — taking a cash/manual EFTPOS
/// payment is core counter work. Reuses the same venue/product/tax setup helpers as
/// <see cref="OrderEndpointsTests"/> (each test file in this codebase is self-contained; see that
/// class's own remarks for why staff-PIN rejection and the permission filter's unit-level behaviour
/// aren't re-tested per entity here).
/// </summary>
public class PaymentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public PaymentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task RecordCashPayment_ForFullAmount_SettlesAndCompletesTheOrder()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 11.00m);

        var response = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 11.00m, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Recorded, payment!.Status);
        Assert.Equal(11.00m, payment.AmountApproved);

        var updatedOrder = await (await client.GetAsync($"/api/v1/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Completed, updatedOrder!.Status);
        Assert.NotNull(updatedOrder.ClosedAtUtc);
    }

    [Fact]
    public async Task RecordPayment_Succeeds_ForStaffPinSession()
    {
        var adminClient = _factory.CreateClient();
        var admin = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, admin);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, admin.OrganisationId, $"Payment Staff Venue {Guid.NewGuid()}");
        await adminClient.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminal = (await (await adminClient.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Bar 1", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(adminClient, admin.OrganisationId, "STAFF_PAY_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(adminClient, admin.OrganisationId, taxCategory.Id, 5.50m, "STAFF_PAY_PRODUCT");

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var staffMember = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "PAY01");
        await using (var dbContext = CreateDbContext())
        {
            var staffRoleId = (await dbContext.Roles.SingleAsync(r => r.Name == "Staff")).Id;
            await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staffMember.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        }

        var staffLogin = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "PAY01", StaffTestHelper.DefaultPin);
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffLogin.SessionToken);

        var order = (await (await staffClient.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminal.Id))).Content.ReadFromJsonAsync<OrderResponse>())!;
        await staffClient.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(product.Id, null, 1, null, null));

        var response = await RecordPaymentAsync(staffClient, order.Id, PaymentMethod.Cash, 5.50m, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RecordPayment_SplitAcrossTwoPayments_ClosesOrderOnlyOnceFullySettled()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 20.00m);

        var firstResponse = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 12.00m, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var afterFirst = await (await client.GetAsync($"/api/v1/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Open, afterFirst!.Status);

        var secondResponse = await RecordPaymentAsync(client, order.Id, PaymentMethod.ManualEftpos, 8.00m, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var afterSecond = await (await client.GetAsync($"/api/v1/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Completed, afterSecond!.Status);

        var payments = await (await client.GetAsync($"/api/v1/orders/{order.Id}/payments")).Content.ReadFromJsonAsync<List<PaymentResponse>>();
        Assert.Equal(2, payments!.Count);
    }

    [Fact]
    public async Task RecordPayment_RetryWithSameIdempotencyKey_DoesNotCreateDuplicate()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 11.00m);
        var idempotencyKey = Guid.NewGuid();

        var first = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 11.00m, idempotencyKey);
        var firstPayment = await first.Content.ReadFromJsonAsync<PaymentResponse>();

        var retry = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 11.00m, idempotencyKey);
        var retryPayment = await retry.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(firstPayment!.Id, retryPayment!.Id);

        var payments = await (await client.GetAsync($"/api/v1/orders/{order.Id}/payments")).Content.ReadFromJsonAsync<List<PaymentResponse>>();
        Assert.Single(payments!);
    }

    [Fact]
    public async Task RecordPayment_Rejects_WhenExceedingOrderGrandTotal()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);

        var response = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 15.00m, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordPayment_Rejects_IntegratedMethod_NoAdapterExistsYet()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);

        var response = await RecordPaymentAsync(client, order.Id, PaymentMethod.Integrated, 10.00m, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordPayment_Rejects_WhenOrderIsNotOpenOrHeld()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        await client.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null);

        var response = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RecordPayment_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/orders/{order.Id}/payments",
            new RecordPaymentRequest(PaymentMethod.Cash, 10.00m, Guid.NewGuid(), null, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);

        // SupportAccess is a seeded role that does not carry payments.record.
        var supportClient = _factory.CreateClient();
        var supportCaller = await RbacTestSeeder.SeedAsync(supportClient, "SupportAccess", owner.TenantId);
        AuthenticateAs(supportClient, supportCaller);

        var response = await RecordPaymentAsync(supportClient, order.Id, PaymentMethod.Cash, 10.00m, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListPayments_Fails_WithoutPermission()
    {
        // Create_Fails_WithoutPermission above already proves POST .../payments; this covers the
        // list endpoint too (Milestone F RBAC-inventory consolidation).
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);

        var supportClient = _factory.CreateClient();
        var supportCaller = await RbacTestSeeder.SeedAsync(supportClient, "SupportAccess", owner.TenantId);
        AuthenticateAs(supportClient, supportCaller);

        var response = await supportClient.GetAsync($"/api/v1/orders/{order.Id}/payments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AllEndpoints_Return403_ForDeviceToken()
    {
        // A device token is trusted device context only (ADR-0008) — empty roles/permissions by
        // design, so it must never satisfy payments.record on either payment endpoint.
        var ownerClient = _factory.CreateClient();
        var (owner, location, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(ownerClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var attempts = new[]
        {
            await RecordPaymentAsync(deviceClient, order.Id, PaymentMethod.Cash, 10.00m, Guid.NewGuid()),
            await deviceClient.GetAsync($"/api/v1/orders/{order.Id}/payments"),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Fact]
    public async Task ListPayments_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var (callerA, _, terminalA) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, callerA.OrganisationId, terminalA.Id, 10.00m);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.GetAsync($"/api/v1/orders/{order.Id}/payments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows_ForPaymentAndOrderCompletion()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 11.00m);

        var paymentResponse = await RecordPaymentAsync(client, order.Id, PaymentMethod.Cash, 11.00m, Guid.NewGuid());
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        await using var context = CreateDbContext();
        var paymentEvents = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == payment!.Id && a.EntityType == "Payment")
            .Select(a => a.EventType)
            .ToListAsync();
        var orderEvents = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == order.Id && a.EntityType == "Order")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("PaymentRecorded", paymentEvents);
        Assert.Contains("OrderCompleted", orderEvents);
    }

    private async Task<(SeededCaller Caller, LocationResponse Location, TerminalResponse Terminal)> SetupVenueAsync(HttpClient client)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Payment Venue {Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminalResponse = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Front Counter", location.Id));
        var terminal = (await terminalResponse.Content.ReadFromJsonAsync<TerminalResponse>())!;
        return (caller, location, terminal);
    }

    private static async Task<OrderResponse> OpenOrderWithSingleLineAsync(HttpClient client, Guid organisationId, Guid terminalId, decimal productPrice)
    {
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, organisationId, $"PAY_{Guid.NewGuid():N}", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, organisationId, taxCategory.Id, productPrice, $"PAY_PRODUCT_{Guid.NewGuid():N}");

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

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
