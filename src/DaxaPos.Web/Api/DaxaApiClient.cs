using System.Net;
using System.Net.Http.Json;

namespace DaxaPos.Web.Api;

/// <summary>
/// Typed wrapper over the named "DaxaApi" <see cref="HttpClient"/> (see Program.cs) for the
/// PLAN-0003 identity endpoints Milestone A consumes. Never recalculates or second-guesses server
/// responses (CLAUDE.md: server remains authoritative) — it only maps transport outcomes onto
/// <see cref="ApiResult{T}"/> so pages can branch on success/401/403/failure.
/// </summary>
public sealed class DaxaApiClient(HttpClient httpClient)
{
    public Task<ApiResult<DeviceRegistrationResult>> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken ct = default) =>
        PostAsync<DeviceRegistrationRequest, DeviceRegistrationResult>("api/v1/device-registration", request, ct);

    public Task<ApiResult<StaffPinLoginResult>> StaffPinLoginAsync(StaffPinLoginRequest request, CancellationToken ct = default) =>
        PostAsync<StaffPinLoginRequest, StaffPinLoginResult>("api/v1/auth/staff-pin/login", request, ct);

    public async Task<ApiResult> LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.PostAsync("api/v1/auth/logout", content: null, ct);
            return response.IsSuccessStatusCode
                ? ApiResult.Success(response.StatusCode)
                : ApiResult.FromStatusCode(response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.NetworkFailure(ex.Message);
        }
    }

    public Task<ApiResult<AuthContextResult>> GetMeAsync(CancellationToken ct = default) =>
        GetAsync<AuthContextResult>("api/v1/auth/me", ct);

    private async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest request, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(uri, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<TResponse>.FromStatusCode(response.StatusCode);
            }

            var value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
            return value is null
                ? ApiResult<TResponse>.FromStatusCode(HttpStatusCode.UnprocessableEntity, "Empty response body.")
                : ApiResult<TResponse>.Success(value, response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<TResponse>.NetworkFailure(ex.Message);
        }
    }

    private async Task<ApiResult<TResponse>> GetAsync<TResponse>(string uri, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, ct);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<TResponse>.FromStatusCode(response.StatusCode);
            }

            var value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
            return value is null
                ? ApiResult<TResponse>.FromStatusCode(HttpStatusCode.UnprocessableEntity, "Empty response body.")
                : ApiResult<TResponse>.Success(value, response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<TResponse>.NetworkFailure(ex.Message);
        }
    }
}
