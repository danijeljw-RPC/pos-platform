using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Orders;
using DaxaPos.Api.Endpoints.Payments;
using DaxaPos.Api.Endpoints.Refunds;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="RefundEndpoints"/> (PLAN-0005 Milestone C).
/// <c>payments.refund</c> is <c>AdminSensitive</c> + <c>rejectStaffPin: true</c> — a different
/// posture from <see cref="PaymentEndpointsTests"/>'s <c>payments.record</c> (<c>Operational</c>,
/// staff-PIN-eligible): refunds are manager/admin-only by default (approved Human Decision #4).
/// Reuses the same venue/product/tax/payment setup helpers as <see cref="PaymentEndpointsTests"/>.
/// </summary>
public class RefundEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public RefundEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task RecordRefund_FullAmount_Succeeds_AndDoesNotMutateOriginalPayment()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 20.30m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 20.30m);

        var response = await RecordRefundAsync(client, payment.Id, 20.30m, "CustomerRequest");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var refund = await response.Content.ReadFromJsonAsync<RefundResponse>();
        Assert.NotNull(refund);
        Assert.Equal(payment.Id, refund!.PaymentId);
        Assert.Equal(order.Id, refund.OrderId);
        Assert.Equal(20.30m, refund.Amount);
        Assert.Equal(RefundStatus.Recorded, refund.Status);

        // ADR-0010: the original payment record is never mutated by a refund.
        var payments = await (await client.GetAsync($"/api/v1/orders/{order.Id}/payments")).Content.ReadFromJsonAsync<List<PaymentResponse>>();
        var unchangedPayment = payments!.Single(p => p.Id == payment.Id);
        Assert.Equal(PaymentStatus.Recorded, unchangedPayment.Status);
        Assert.Equal(20.30m, unchangedPayment.AmountApproved);
    }

    [Fact]
    public async Task RecordRefund_PartialAmount_Succeeds_AndASecondPartialRefundCanCompleteIt()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 20.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 20.00m);

        var first = await RecordRefundAsync(client, payment.Id, 12.00m, "ProductIssue");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RecordRefundAsync(client, payment.Id, 8.00m, "ProductIssue");
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var refunds = await (await client.GetAsync($"/api/v1/payments/{payment.Id}/refunds")).Content.ReadFromJsonAsync<List<RefundResponse>>();
        Assert.Equal(2, refunds!.Count);
        Assert.Equal(20.00m, refunds.Sum(r => r.Amount));
    }

    [Fact]
    public async Task RecordRefund_Rejects_WhenExceedingPaymentApprovedAmount()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m);

        var response = await RecordRefundAsync(client, payment.Id, 15.00m, "CustomerRequest");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_Rejects_WhenSecondPartialRefundWouldExceedRemainingAmount()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 20.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 20.00m);

        var first = await RecordRefundAsync(client, payment.Id, 15.00m, "ProductIssue");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RecordRefundAsync(client, payment.Id, 6.00m, "ProductIssue");

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/payments/{payment.Id}/refunds",
            new RecordRefundRequest(5.00m, "CustomerRequest", null, null, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_Rejects_MissingReasonCode()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m);

        var response = await RecordRefundAsync(client, payment.Id, 5.00m, "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);
        var order = await OpenOrderWithSingleLineAsync(ownerClient, owner.OrganisationId, terminal.Id, 10.00m);
        var payment = await RecordAndParsePaymentAsync(ownerClient, order.Id, PaymentMethod.Cash, 10.00m);

        // "Staff" carries orders.manage/payments.record but not payments.refund (AdminSensitive,
        // manager/admin-only by default).
        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await RecordRefundAsync(staffClient, payment.Id, 5.00m, "CustomerRequest");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_Rejects_ForRealStaffPinSession()
    {
        var adminClient = _factory.CreateClient();
        var admin = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, admin);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, admin.OrganisationId, $"Refund Staff Venue {Guid.NewGuid()}");
        await adminClient.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminal = (await (await adminClient.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Bar 1", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(adminClient, admin.OrganisationId, "REFUND_STAFF_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(adminClient, admin.OrganisationId, taxCategory.Id, 10.00m, "REFUND_STAFF_PRODUCT");

        var order = (await (await adminClient.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminal.Id))).Content.ReadFromJsonAsync<OrderResponse>())!;
        await adminClient.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(product.Id, null, 1, null, null));
        var payment = await RecordAndParsePaymentAsync(adminClient, order.Id, PaymentMethod.Cash, 10.00m);

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var staffMember = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "REF01");

        // "Staff" is the only role a staff-PIN session can ever hold — the login endpoint itself
        // rejects any role granting an AdminSensitive permission (defense-in-depth), so a role
        // carrying payments.refund could never even complete PIN login in the first place. This
        // test therefore proves the realistic end-to-end case Milestone C's plan asks for: a
        // legitimate staff-PIN session (holding orders.manage/payments.record/
        // catalog.sold-out-toggle, none of them payments.refund) is still rejected 403 by
        // RequirePermissionFilter's rejectStaffPin gate when it attempts a refund.
        await using (var dbContext = CreateDbContext())
        {
            var staffRoleId = (await dbContext.Roles.SingleAsync(r => r.Name == "Staff")).Id;
            await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staffMember.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        }

        var staffLogin = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "REF01", StaffTestHelper.DefaultPin);
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffLogin.SessionToken);

        var response = await RecordRefundAsync(staffClient, payment.Id, 5.00m, "CustomerRequest");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_Rejects_WhenPaymentDoesNotExist()
    {
        var client = _factory.CreateClient();
        await SetupVenueAsync(client);

        var response = await RecordRefundAsync(client, Guid.NewGuid(), 5.00m, "CustomerRequest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListRefunds_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var (callerA, _, terminalA) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, callerA.OrganisationId, terminalA.Id, 10.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.GetAsync($"/api/v1/payments/{payment.Id}/refunds");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordRefund_WritesAuditEventRow_WithReasonAndLinkedOrderAndPaymentIds()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = await OpenOrderWithSingleLineAsync(client, caller.OrganisationId, terminal.Id, 10.00m);
        var payment = await RecordAndParsePaymentAsync(client, order.Id, PaymentMethod.Cash, 10.00m);

        var refundResponse = await RecordRefundAsync(client, payment.Id, 10.00m, "CustomerRequest");
        var refund = await refundResponse.Content.ReadFromJsonAsync<RefundResponse>();

        await using var context = CreateDbContext();
        var refundEvent = await context.AuditEvents.IgnoreQueryFilters()
            .SingleAsync(a => a.EntityId == refund!.Id && a.EntityType == "Refund");

        Assert.Equal("RefundRecorded", refundEvent.EventType);
        Assert.NotNull(refundEvent.AfterValue);
        Assert.Contains(payment.Id.ToString(), refundEvent.AfterValue);
        Assert.Contains(order.Id.ToString(), refundEvent.AfterValue);
        Assert.Contains("CustomerRequest", refundEvent.AfterValue);
    }

    private async Task<(SeededCaller Caller, LocationResponse Location, TerminalResponse Terminal)> SetupVenueAsync(HttpClient client)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Refund Venue {Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminalResponse = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Front Counter", location.Id));
        var terminal = (await terminalResponse.Content.ReadFromJsonAsync<TerminalResponse>())!;
        return (caller, location, terminal);
    }

    private static async Task<OrderResponse> OpenOrderWithSingleLineAsync(HttpClient client, Guid organisationId, Guid terminalId, decimal productPrice)
    {
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, organisationId, $"REFUND_{Guid.NewGuid():N}", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, organisationId, taxCategory.Id, productPrice, $"REFUND_PRODUCT_{Guid.NewGuid():N}");

        var order = (await (await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminalId))).Content.ReadFromJsonAsync<OrderResponse>())!;
        var withLine = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(product.Id, null, 1, null, null));
        return (await withLine.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static async Task<PaymentResponse> RecordAndParsePaymentAsync(HttpClient client, Guid orderId, PaymentMethod method, decimal amount)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/payments", new RecordPaymentRequest(method, amount, Guid.NewGuid()));
        return (await response.Content.ReadFromJsonAsync<PaymentResponse>())!;
    }

    private static Task<HttpResponseMessage> RecordRefundAsync(HttpClient client, Guid paymentId, decimal amount, string reasonCode) =>
        client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/refunds", new RecordRefundRequest(amount, reasonCode));

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
