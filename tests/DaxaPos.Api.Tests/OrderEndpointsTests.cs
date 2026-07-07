using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Orders;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="OrderEndpoints"/> (PLAN-0005 Milestone A).
/// <c>orders.manage</c> is <c>Operational</c> (staff-PIN-eligible), the plan's first
/// staff-accessible write surface from day one — <see cref="Open_Succeeds_ForStaffPinSession"/> is
/// the load-bearing proof, matching PLAN-0004's <c>catalog.sold-out-toggle</c> precedent. Reuses
/// the CLAUDE.md/ADR-0006 AU mixed-basket worked example ($5.50/$8.80/$6.00 -> $20.30 total, $1.30
/// GST) as an end-to-end order rather than re-deriving new fixtures.
/// </summary>
public class OrderEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public OrderEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Open_Succeeds_ForOrganisationOwner_WithSnapshottedTaxInclusivePricing()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);

        var response = await OpenOrderAsync(client, terminal.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Open, order!.Status);
        Assert.True(order.IsTaxInclusivePricing);
        Assert.Equal(1, order.OrderNumber);
        Assert.Empty(order.Lines);
    }

    [Fact]
    public async Task Open_AllocatesSequentialOrderNumbers_PerLocation()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);

        var first = await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>();
        var second = await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(1, first!.OrderNumber);
        Assert.Equal(2, second!.OrderNumber);
    }

    [Fact]
    public async Task Open_Succeeds_ForStaffPinSession()
    {
        var adminClient = _factory.CreateClient();
        var admin = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, admin);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, admin.OrganisationId, $"Staff Venue {Guid.NewGuid()}");
        await adminClient.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));
        var terminal = (await (await adminClient.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Bar 1", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        // Milestone C.2: order actions now require a resolved TerminalId, so the device must be
        // assigned to the terminal it's about to open orders for before the staff session logs in.
        await adminClient.PostAsJsonAsync($"/api/v1/terminals/{terminal.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));

        var staffMember = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "ORD01");
        await using (var dbContext = CreateDbContext())
        {
            var staffRoleId = (await dbContext.Roles.SingleAsync(r => r.Name == "Staff")).Id;
            await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staffMember.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        }

        var staffLogin = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "ORD01", StaffTestHelper.DefaultPin);

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffLogin.SessionToken);

        var response = await OpenOrderAsync(staffClient, terminal.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Open_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminal.Id, null, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Open_Rejects_TerminalFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var (callerA, _, terminalA) = await SetupVenueAsync(client);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await OpenOrderAsync(client, terminalA.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Open_Rejects_MissingVenueTaxConfiguration_InsteadOfSilentlyDefaulting()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Unconfigured Venue {Guid.NewGuid()}");
        var terminal = (await (await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Counter", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;

        // No VenueTaxConfiguration was ever created for this location.
        var response = await OpenOrderAsync(client, terminal.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddLine_AuMixedBasket_MatchesClaudeMdWorkedExample()
    {
        var client = _factory.CreateClient();
        var (caller, location, terminal) = await SetupVenueAsync(client);
        var taxableCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "MIXED_TAXABLE", 10m, includedInPrice: true);
        var gstFreeCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "MIXED_GSTFREE", 0m, includedInPrice: true, receiptMarkerCode: "F");

        var flatWhite = await CreateProductAsync(client, caller.OrganisationId, taxableCategory.Id, 5.50m, "FLAT_WHITE");
        var cakeSlice = await CreateProductAsync(client, caller.OrganisationId, taxableCategory.Id, 8.80m, "CAKE_SLICE");
        var bread = await CreateProductAsync(client, caller.OrganisationId, gstFreeCategory.Id, 6.00m, "BREAD");

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        await AddLineAsync(client, order.Id, flatWhite.Id);
        await AddLineAsync(client, order.Id, cakeSlice.Id);
        var lastResponse = await AddLineAsync(client, order.Id, bread.Id);
        var final = await lastResponse.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.Created, lastResponse.StatusCode);
        Assert.Equal(20.30m, final!.GrandTotalAmount);
        Assert.Equal(1.30m, final.TotalTaxAmount);
        Assert.Equal(19.00m, final.SubtotalAmount);

        var breadLine = Assert.Single(final.Lines, l => l.ProductId == bread.Id);
        var breadTax = Assert.Single(breadLine.Taxes);
        Assert.Equal("F", breadTax.ReceiptMarkerCodeSnapshot);
        Assert.Equal(0m, breadTax.TaxAmount);
    }

    [Fact]
    public async Task AddLine_Rejects_SoldOutProduct()
    {
        var client = _factory.CreateClient();
        var (caller, location, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "SOLDOUT_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 5.00m, "SOLDOUT_PRODUCT");
        await client.PostAsJsonAsync($"/api/v1/products/{product.Id}/locations/{location.Id}/sold-out", new SetSoldOutRequest(true));

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var response = await AddLineAsync(client, order.Id, product.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddLine_Rejects_WhenRequiredModifierGroupHasNoSelection()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "REQMOD_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 20.00m, "STEAK");
        var doneness = await CreateModifierGroupAsync(client, caller.OrganisationId, 1, 1, isRequired: true, "DONENESS");
        await AssignProductModifierGroupAsync(client, product.Id, doneness.Id);

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var response = await AddLineAsync(client, order.Id, product.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddLine_Rejects_WhenSelectionCountBelowGroupMinimum()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "MINMOD_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 12.00m, "BURGER");
        var sauces = await CreateModifierGroupAsync(client, caller.OrganisationId, 2, 3, isRequired: false, "SAUCES");
        var ketchup = await CreateModifierAsync(client, sauces.Id, 0m, "KETCHUP");
        await AssignProductModifierGroupAsync(client, product.Id, sauces.Id);

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var response = await AddLineWithModifiersAsync(client, order.Id, product.Id, [ketchup.Id]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddLine_Rejects_WhenSelectionCountExceedsGroupMaximum()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "MAXMOD_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 12.00m, "PIZZA");
        var toppings = await CreateModifierGroupAsync(client, caller.OrganisationId, 0, 1, isRequired: false, "TOPPINGS");
        var mushroom = await CreateModifierAsync(client, toppings.Id, 1.00m, "MUSHROOM");
        var olives = await CreateModifierAsync(client, toppings.Id, 1.00m, "OLIVES");
        await AssignProductModifierGroupAsync(client, product.Id, toppings.Id);

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var response = await AddLineWithModifiersAsync(client, order.Id, product.Id, [mushroom.Id, olives.Id]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddLine_Succeeds_WithAValidModifierSelection_AndModifiersAppearOnTheLine()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "OKMOD_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 20.00m, "STEAK_OK");
        var doneness = await CreateModifierGroupAsync(client, caller.OrganisationId, 1, 1, isRequired: true, "DONENESS_OK");
        var medium = await CreateModifierAsync(client, doneness.Id, 0m, "MEDIUM");
        await AssignProductModifierGroupAsync(client, product.Id, doneness.Id);

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var response = await AddLineWithModifiersAsync(client, order.Id, product.Id, [medium.Id]);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var withLine = await response.Content.ReadFromJsonAsync<OrderResponse>();
        var line = Assert.Single(withLine!.Lines);
        var modifier = Assert.Single(line.Modifiers);
        Assert.Equal(medium.Id, modifier.ModifierId);
    }

    [Fact]
    public async Task AddLine_Rejects_WhenOrderIsNotOpenOrHeld()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "CLOSED_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 5.00m, "CLOSED_PRODUCT");

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        await client.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null);

        var response = await AddLineAsync(client, order.Id, product.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task VoidLine_RecomputesOrderTotals()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "VOID_LINE_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 11.00m, "VOID_LINE_PRODUCT");

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var addResponse = await AddLineAsync(client, order.Id, product.Id);
        var withLine = (await addResponse.Content.ReadFromJsonAsync<OrderResponse>())!;
        var line = Assert.Single(withLine.Lines);
        Assert.Equal(11.00m, withLine.GrandTotalAmount);

        var voidResponse = await client.DeleteAsync($"/api/v1/orders/{order.Id}/lines/{line.Id}?reason=Customer%20changed%20mind");
        var afterVoid = await voidResponse.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        Assert.Equal(0m, afterVoid!.GrandTotalAmount);
        Assert.Equal(0m, afterVoid.SubtotalAmount);
        Assert.Equal(0m, afterVoid.TotalTaxAmount);
        Assert.Equal(OrderLineStatus.Voided, Assert.Single(afterVoid.Lines).Status);
    }

    [Fact]
    public async Task Hold_Then_Resume_RoundTrips()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);
        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        var holdResponse = await client.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null);
        Assert.Equal(HttpStatusCode.OK, holdResponse.StatusCode);
        Assert.Equal(OrderStatus.Held, (await holdResponse.Content.ReadFromJsonAsync<OrderResponse>())!.Status);

        var resumeResponse = await client.PostAsync($"/api/v1/orders/{order.Id}/resume", content: null);
        Assert.Equal(HttpStatusCode.OK, resumeResponse.StatusCode);
        Assert.Equal(OrderStatus.Open, (await resumeResponse.Content.ReadFromJsonAsync<OrderResponse>())!.Status);
    }

    [Fact]
    public async Task Hold_Fails_WhenOrderIsNotOpen()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);
        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        await client.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null);

        var response = await client.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Void_And_Cancel_TransitionOrder_FromOpenOrHeld()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);

        var voided = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var voidResponse = await client.PostAsync($"/api/v1/orders/{voided.Id}/void?reason=Mistake", content: null);
        Assert.Equal(OrderStatus.Voided, (await voidResponse.Content.ReadFromJsonAsync<OrderResponse>())!.Status);

        var cancelled = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var cancelResponse = await client.PostAsync($"/api/v1/orders/{cancelled.Id}/cancel", content: null);
        Assert.Equal(OrderStatus.Cancelled, (await cancelResponse.Content.ReadFromJsonAsync<OrderResponse>())!.Status);
    }

    [Fact]
    public async Task AddLine_Fails_WhenExceedingTwentyDistinctTaxComponentPerOrderLimit()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        // 20 lines, each with its own distinct single-component tax category — exactly at ADR-0006's
        // per-order limit — must all succeed.
        for (var i = 0; i < 20; i++)
        {
            var category = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, $"LIMIT_{i}", 10m, includedInPrice: true);
            var product = await CreateProductAsync(client, caller.OrganisationId, category.Id, 1.00m, $"LIMIT_PRODUCT_{i}");
            var response = await AddLineAsync(client, order.Id, product.Id);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // The 21st distinct tax component must be rejected.
        var overLimitCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "LIMIT_21", 10m, includedInPrice: true);
        var overLimitProduct = await CreateProductAsync(client, caller.OrganisationId, overLimitCategory.Id, 1.00m, "LIMIT_PRODUCT_21");

        var overLimitResponse = await AddLineAsync(client, order.Id, overLimitProduct.Id);

        Assert.Equal(HttpStatusCode.BadRequest, overLimitResponse.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var (_, _, terminal) = await SetupVenueAsync(client);
        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var (callerA, _, terminal) = await SetupVenueAsync(client);
        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/orders/{order.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetById_Blocked_ForDifferentTerminal_SameLocation()
    {
        var scenario = await SetupTwoTerminalsAsync("Isolation Get Venue");
        var order = (await (await OpenOrderAsync(scenario.StaffClientA, scenario.TerminalA.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        Assert.Equal(HttpStatusCode.NotFound, (await scenario.StaffClientB.GetAsync($"/api/v1/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scenario.StaffClientA.GetAsync($"/api/v1/orders/{order.Id}")).StatusCode);
    }

    [Fact]
    public async Task AddLine_Blocked_ForDifferentTerminal_SameLocation()
    {
        var scenario = await SetupTwoTerminalsAsync("Isolation AddLine Venue");
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(scenario.AdminClient, scenario.OrganisationId, "ISO_ADDLINE_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(scenario.AdminClient, scenario.OrganisationId, taxCategory.Id, 5.00m, "ISO_ADDLINE_PRODUCT");
        var order = (await (await OpenOrderAsync(scenario.StaffClientA, scenario.TerminalA.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        Assert.Equal(HttpStatusCode.NotFound, (await AddLineAsync(scenario.StaffClientB, order.Id, product.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await AddLineAsync(scenario.StaffClientA, order.Id, product.Id)).StatusCode);
    }

    [Fact]
    public async Task VoidLine_Blocked_ForDifferentTerminal_SameLocation()
    {
        var scenario = await SetupTwoTerminalsAsync("Isolation VoidLine Venue");
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(scenario.AdminClient, scenario.OrganisationId, "ISO_VOIDLINE_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(scenario.AdminClient, scenario.OrganisationId, taxCategory.Id, 5.00m, "ISO_VOIDLINE_PRODUCT");
        var order = (await (await OpenOrderAsync(scenario.StaffClientA, scenario.TerminalA.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var withLine = (await (await AddLineAsync(scenario.StaffClientA, order.Id, product.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var lineId = withLine.Lines[0].Id;

        Assert.Equal(HttpStatusCode.NotFound, (await scenario.StaffClientB.DeleteAsync($"/api/v1/orders/{order.Id}/lines/{lineId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scenario.StaffClientA.DeleteAsync($"/api/v1/orders/{order.Id}/lines/{lineId}")).StatusCode);
    }

    [Fact]
    public async Task VoidOrder_Blocked_ForDifferentTerminal_SameLocation()
    {
        var scenario = await SetupTwoTerminalsAsync("Isolation VoidOrder Venue");
        var order = (await (await OpenOrderAsync(scenario.StaffClientA, scenario.TerminalA.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        Assert.Equal(HttpStatusCode.NotFound, (await scenario.StaffClientB.PostAsync($"/api/v1/orders/{order.Id}/void", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scenario.StaffClientA.PostAsync($"/api/v1/orders/{order.Id}/void", content: null)).StatusCode);
    }

    [Fact]
    public async Task HoldAndResume_Blocked_ForDifferentTerminal_SameLocation()
    {
        var scenario = await SetupTwoTerminalsAsync("Isolation Hold Venue");
        var order = (await (await OpenOrderAsync(scenario.StaffClientA, scenario.TerminalA.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        Assert.Equal(HttpStatusCode.NotFound, (await scenario.StaffClientB.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scenario.StaffClientA.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await scenario.StaffClientB.PostAsync($"/api/v1/orders/{order.Id}/resume", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scenario.StaffClientA.PostAsync($"/api/v1/orders/{order.Id}/resume", content: null)).StatusCode);
    }

    [Fact]
    public async Task Open_Rejects_WhenSessionHasNoResolvedTerminalId()
    {
        // A location-bound staff session whose device was never assigned to a terminal must not
        // be able to open an order for any terminal at its own location, even one that exists.
        var client = _factory.CreateClient();
        var (_, location, terminal) = await SetupVenueAsync(client);

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var staffMember = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "NOTERM");
        await using (var dbContext = CreateDbContext())
        {
            var staffRoleId = (await dbContext.Roles.SingleAsync(r => r.Name == "Staff")).Id;
            await client.PostAsJsonAsync($"/api/v1/staff-members/{staffMember.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        }

        var login = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "NOTERM", StaffTestHelper.DefaultPin);
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);

        var response = await OpenOrderAsync(staffClient, terminal.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Open_Rejects_WhenRequestedTerminalDiffersFromSessionsResolvedTerminal()
    {
        var scenario = await SetupTwoTerminalsAsync("Isolation Open Cross Venue");

        // StaffClientA's session is resolved to TerminalA — opening against TerminalB (same
        // location, different terminal) via a client-supplied TerminalId must be rejected, not
        // silently accepted (ADR-0015 Context Provenance).
        var response = await OpenOrderAsync(scenario.StaffClientA, scenario.TerminalB.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);

        // SupportAccess is a seeded role that does not carry orders.manage — an authenticated
        // caller with no relevant permission must still get 403, not 401.
        var supportClient = _factory.CreateClient();
        var supportCaller = await RbacTestSeeder.SeedAsync(supportClient, "SupportAccess", owner.TenantId);
        AuthenticateAs(supportClient, supportCaller);

        var response = await OpenOrderAsync(supportClient, terminal.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemainingEndpoints_Return403_WithoutOrdersManage()
    {
        // Create_Fails_WithoutPermission above already proves POST /orders itself; this sweeps the
        // other 8 orders.manage-gated endpoints (Milestone F RBAC-inventory consolidation).
        var ownerClient = _factory.CreateClient();
        var (owner, _, terminal) = await SetupVenueAsync(ownerClient);
        var withLine = await OpenOrderAsync(ownerClient, terminal.Id);
        var order = (await withLine.Content.ReadFromJsonAsync<OrderResponse>())!;

        var supportClient = _factory.CreateClient();
        var supportCaller = await RbacTestSeeder.SeedAsync(supportClient, "SupportAccess", owner.TenantId);
        AuthenticateAs(supportClient, supportCaller);

        var attempts = new[]
        {
            await supportClient.GetAsync("/api/v1/orders"),
            await supportClient.GetAsync($"/api/v1/orders/{order.Id}"),
            await supportClient.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(Guid.NewGuid(), null, 1, null, null)),
            await supportClient.DeleteAsync($"/api/v1/orders/{order.Id}/lines/{Guid.NewGuid()}"),
            await supportClient.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null),
            await supportClient.PostAsync($"/api/v1/orders/{order.Id}/resume", content: null),
            await supportClient.PostAsync($"/api/v1/orders/{order.Id}/void", content: null),
            await supportClient.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Fact]
    public async Task AllEndpoints_Return403_ForDeviceToken()
    {
        // A device token is trusted device context only (ADR-0008) — empty roles/permissions by
        // design, so it must never satisfy orders.manage on any of the 9 order endpoints.
        var ownerClient = _factory.CreateClient();
        var (owner, location, terminal) = await SetupVenueAsync(ownerClient);
        var order = (await (await OpenOrderAsync(ownerClient, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;

        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(ownerClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var attempts = new[]
        {
            await OpenOrderAsync(deviceClient, terminal.Id),
            await deviceClient.GetAsync("/api/v1/orders"),
            await deviceClient.GetAsync($"/api/v1/orders/{order.Id}"),
            await deviceClient.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(Guid.NewGuid(), null, 1, null, null)),
            await deviceClient.DeleteAsync($"/api/v1/orders/{order.Id}/lines/{Guid.NewGuid()}"),
            await deviceClient.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null),
            await deviceClient.PostAsync($"/api/v1/orders/{order.Id}/resume", content: null),
            await deviceClient.PostAsync($"/api/v1/orders/{order.Id}/void", content: null),
            await deviceClient.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var (caller, _, terminal) = await SetupVenueAsync(client);
        var taxCategory = await CreateTaxCategoryWithDefinitionAsync(client, caller.OrganisationId, "AUDIT_TAX", 10m, includedInPrice: true);
        var product = await CreateProductAsync(client, caller.OrganisationId, taxCategory.Id, 5.00m, "AUDIT_PRODUCT");

        var order = (await (await OpenOrderAsync(client, terminal.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var withLine = (await (await AddLineAsync(client, order.Id, product.Id)).Content.ReadFromJsonAsync<OrderResponse>())!;
        var line = Assert.Single(withLine.Lines);
        await client.DeleteAsync($"/api/v1/orders/{order.Id}/lines/{line.Id}");
        await client.PostAsync($"/api/v1/orders/{order.Id}/hold", content: null);

        await using var context = CreateDbContext();
        var orderEvents = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == order.Id && a.EntityType == "Order")
            .Select(a => a.EventType)
            .ToListAsync();
        var lineEvents = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == line.Id && a.EntityType == "OrderLine")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("OrderOpened", orderEvents);
        Assert.Contains("OrderHeld", orderEvents);
        Assert.Contains("OrderLineLineAdded", lineEvents);
        Assert.Contains("OrderLineLineVoided", lineEvents);
    }

    private async Task<(SeededCaller Caller, LocationResponse Location, TerminalResponse Terminal)> SetupVenueAsync(HttpClient client, bool taxInclusive = true)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Venue {Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, taxInclusive, TaxCalculationScope.PerLine));
        var terminalResponse = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Front Counter", location.Id));
        var terminal = (await terminalResponse.Content.ReadFromJsonAsync<TerminalResponse>())!;
        return (caller, location, terminal);
    }

    private sealed record TwoTerminalScenario(
        HttpClient AdminClient,
        Guid OrganisationId,
        LocationResponse Location,
        TerminalResponse TerminalA,
        TerminalResponse TerminalB,
        HttpClient StaffClientA,
        HttpClient StaffClientB);

    /// <summary>
    /// Milestone C.2: two terminals at the same location, each with its own registered device
    /// assigned to it and its own logged-in staff session — the minimum fixture needed to prove
    /// Terminal A's session cannot touch Terminal B's order.
    /// </summary>
    private async Task<TwoTerminalScenario> SetupTwoTerminalsAsync(string venueName)
    {
        var adminClient = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, venueName);
        await adminClient.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine));

        var terminalA = (await (await adminClient.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Terminal A", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;
        var terminalB = (await (await adminClient.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Terminal B", location.Id))).Content.ReadFromJsonAsync<TerminalResponse>())!;

        var staffClientA = await CreateStaffSessionForTerminalAsync(adminClient, location.Id, terminalA.Id, "TERMA");
        var staffClientB = await CreateStaffSessionForTerminalAsync(adminClient, location.Id, terminalB.Id, "TERMB");

        return new TwoTerminalScenario(adminClient, caller.OrganisationId, location, terminalA, terminalB, staffClientA, staffClientB);
    }

    private async Task<HttpClient> CreateStaffSessionForTerminalAsync(HttpClient adminClient, Guid locationId, Guid terminalId, string staffCode)
    {
        var deviceClient = _factory.CreateClient();
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, locationId);
        var device = await DeviceTestHelper.RegisterDeviceAsync(deviceClient, pin.Pin);
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);
        await adminClient.PostAsJsonAsync($"/api/v1/terminals/{terminalId}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));

        var staffMember = await StaffTestHelper.CreateStaffMemberAsync(adminClient, locationId, staffCode);
        await using (var dbContext = CreateDbContext())
        {
            var staffRoleId = (await dbContext.Roles.SingleAsync(r => r.Name == "Staff")).Id;
            await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staffMember.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        }

        var login = await StaffTestHelper.StaffLoginAsync(deviceClient, locationId, staffCode, StaffTestHelper.DefaultPin);
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);
        return staffClient;
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

    private static Task<HttpResponseMessage> OpenOrderAsync(HttpClient client, Guid terminalId, string? notes = null) =>
        client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(terminalId, notes));

    private static Task<HttpResponseMessage> AddLineAsync(HttpClient client, Guid orderId, Guid productId, int quantity = 1) =>
        client.PostAsJsonAsync($"/api/v1/orders/{orderId}/lines", new AddOrderLineRequest(productId, null, quantity, null, null));

    private static Task<HttpResponseMessage> AddLineWithModifiersAsync(HttpClient client, Guid orderId, Guid productId, IReadOnlyList<Guid> modifierIds) =>
        client.PostAsJsonAsync($"/api/v1/orders/{orderId}/lines", new AddOrderLineRequest(productId, null, 1, modifierIds, null));

    private static async Task<ModifierGroupResponse> CreateModifierGroupAsync(
        HttpClient client, Guid organisationId, int selectionMin, int selectionMax, bool isRequired, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/modifier-groups",
            new CreateModifierGroupRequest($"Group {codeSuffix}", organisationId, selectionMin, selectionMax, isRequired));
        return (await response.Content.ReadFromJsonAsync<ModifierGroupResponse>())!;
    }

    private static async Task<ModifierResponse> CreateModifierAsync(HttpClient client, Guid modifierGroupId, decimal priceDelta, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/modifiers",
            new CreateModifierRequest($"Modifier {codeSuffix}", modifierGroupId, priceDelta));
        return (await response.Content.ReadFromJsonAsync<ModifierResponse>())!;
    }

    private static Task<HttpResponseMessage> AssignProductModifierGroupAsync(HttpClient client, Guid productId, Guid modifierGroupId, int displayOrder = 0) =>
        client.PostAsJsonAsync("/api/v1/product-modifier-groups", new AssignProductModifierGroupRequest(productId, modifierGroupId, displayOrder));

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
