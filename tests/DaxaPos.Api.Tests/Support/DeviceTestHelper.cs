using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;

namespace DaxaPos.Api.Tests.Support;

/// <summary>
/// Shared helpers for the PLAN-0003 Milestone E device tests: create a location and registration
/// PIN as an authenticated admin, register a device through the pre-auth endpoint, and switch a
/// client to device-token authentication. Callers seed an <c>OrganisationOwner</c> via
/// <see cref="RbacTestSeeder"/> first — that role holds <c>locations.manage</c>,
/// <c>devices.register</c>, and <c>devices.manage</c>.
/// </summary>
public static class DeviceTestHelper
{
    public static async Task<LocationResponse> CreateLocationAsync(HttpClient client, Guid organisationId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest(name, organisationId));
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    public static async Task<DeviceRegistrationPinCreatedResponse> CreatePinAsync(HttpClient client, Guid locationId, int? maxUses = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(locationId, maxUses));
        return (await response.Content.ReadFromJsonAsync<DeviceRegistrationPinCreatedResponse>())!;
    }

    public static Task<HttpResponseMessage> RegisterDeviceRawAsync(
        HttpClient client, string pin, string deviceType = "WindowsPos", string? name = null) =>
        client.PostAsJsonAsync("/api/v1/device-registration", new RegisterDeviceRequest(pin, deviceType, name));

    public static async Task<RegisterDeviceResponse> RegisterDeviceAsync(
        HttpClient client, string pin, string deviceType = "WindowsPos", string? name = null)
    {
        var response = await RegisterDeviceRawAsync(client, pin, deviceType, name);
        return (await response.Content.ReadFromJsonAsync<RegisterDeviceResponse>())!;
    }

    public static void AuthenticateWithDeviceToken(HttpClient client, string deviceToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Device", deviceToken);
}
