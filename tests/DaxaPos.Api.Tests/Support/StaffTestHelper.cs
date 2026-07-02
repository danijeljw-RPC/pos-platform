using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;

namespace DaxaPos.Api.Tests.Support;

/// <summary>
/// Shared helpers for the PLAN-0003 Milestone F staff tests: create staff members as an
/// authenticated admin and attempt staff PIN logins from a device-token-authenticated client.
/// Callers seed an <c>OrganisationOwner</c> via <see cref="RbacTestSeeder"/> (holds
/// <c>staff.manage</c>, <c>locations.manage</c>, <c>devices.register</c>) and build the trusted
/// device context via <see cref="DeviceTestHelper"/>.
/// </summary>
public static class StaffTestHelper
{
    public const string DefaultPin = "4321";

    public static async Task<StaffMemberResponse> CreateStaffMemberAsync(
        HttpClient adminClient, Guid locationId, string staffCode, string pin = DefaultPin, string displayName = "Test Staff")
    {
        var response = await adminClient.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest(displayName, staffCode, pin, locationId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StaffMemberResponse>())!;
    }

    public static Task<HttpResponseMessage> StaffLoginRawAsync(
        HttpClient deviceClient, Guid locationId, string staffCode, string pin) =>
        deviceClient.PostAsJsonAsync("/api/v1/auth/staff-pin/login", new StaffPinLoginRequest(locationId, staffCode, pin));

    public static async Task<StaffPinLoginResponse> StaffLoginAsync(
        HttpClient deviceClient, Guid locationId, string staffCode, string pin)
    {
        var response = await StaffLoginRawAsync(deviceClient, locationId, staffCode, pin);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StaffPinLoginResponse>())!;
    }
}
