using System.Net;
using System.Net.Http.Headers;
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

    /// <summary>
    /// Terminal sales screen (PLAN-0006 Milestone C). Uses the implicit-auth <see cref="GetAsync{T}"/>
    /// helper, not the Back Office explicit-bearer methods below — this is a Terminal-shell staff/
    /// device session call, resolved the same way Milestone A's other Terminal calls are.
    /// </summary>
    public Task<ApiResult<ResolvedMenuResult>> GetResolvedMenuAsync(Guid locationId, CancellationToken ct = default) =>
        GetAsync<ResolvedMenuResult>($"api/v1/menus/resolved?locationId={locationId}", ct);

    /// <summary>
    /// Milestone C.1: real order wiring, replacing the local-only draft. Implicit-auth, same as
    /// <see cref="GetResolvedMenuAsync"/> — these are Terminal-shell staff-session calls.
    /// </summary>
    public Task<ApiResult<OrderResult>> OpenOrderAsync(CreateOrderRequest request, CancellationToken ct = default) =>
        PostAsync<CreateOrderRequest, OrderResult>("api/v1/orders", request, ct);

    public Task<ApiResult<OrderResult>> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
        GetAsync<OrderResult>($"api/v1/orders/{orderId}", ct);

    public Task<ApiResult<OrderResult>> AddOrderLineAsync(Guid orderId, AddOrderLineRequest request, CancellationToken ct = default) =>
        PostAsync<AddOrderLineRequest, OrderResult>($"api/v1/orders/{orderId}/lines", request, ct);

    /// <summary>Reason is bound from the query string server-side (a plain <c>string?</c> minimal-API parameter, not a body).</summary>
    public Task<ApiResult<OrderResult>> VoidOrderLineAsync(Guid orderId, Guid lineId, string? reason, CancellationToken ct = default) =>
        DeleteAsync<OrderResult>($"api/v1/orders/{orderId}/lines/{lineId}{ReasonQuery(reason)}", ct);

    public Task<ApiResult<OrderResult>> VoidOrderAsync(Guid orderId, string? reason, CancellationToken ct = default) =>
        PostNoBodyAsync<OrderResult>($"api/v1/orders/{orderId}/void{ReasonQuery(reason)}", ct);

    private static string ReasonQuery(string? reason) =>
        reason is null ? string.Empty : $"?reason={Uri.EscapeDataString(reason)}";

    /// <summary>
    /// Back Office admin login (ADR-0013 local admin portal login). Unauthenticated — the server
    /// endpoint requires no prior context, unlike Staff PIN login which requires a device context.
    /// </summary>
    public Task<ApiResult<LocalLoginResult>> LocalLoginAsync(LocalLoginRequest request, CancellationToken ct = default) =>
        PostAsync<LocalLoginRequest, LocalLoginResult>("api/v1/auth/local/login", request, ct);

    /// <summary>
    /// Back Office calls below attach their bearer token explicitly rather than relying on
    /// <see cref="AuthHeaderHandler"/>'s implicit Bearer/Device resolution, which only knows about
    /// the Terminal shell's <c>IAuthSessionStore</c>/<c>IDeviceContextStore</c> — never the separate
    /// Back Office session (see <c>BackOfficeSessionState</c>). <see cref="AuthHeaderHandler"/> is
    /// patched to skip its own resolution whenever a request already carries an explicit
    /// <c>Authorization</c> header, so the two sessions never collide on the shared HttpClient.
    /// </summary>
    public Task<ApiResult<DeviceRegistrationPinCreatedResult>> CreateDeviceRegistrationPinAsync(
        string bearerToken, CreateDeviceRegistrationPinRequest request, CancellationToken ct = default) =>
        PostAuthorizedAsync<CreateDeviceRegistrationPinRequest, DeviceRegistrationPinCreatedResult>(
            "api/v1/device-registration-pins", bearerToken, request, ct);

    public Task<ApiResult<DeviceRegistrationPinResult>> RevokeDeviceRegistrationPinAsync(
        string bearerToken, Guid pinId, CancellationToken ct = default) =>
        PostAuthorizedAsync<object?, DeviceRegistrationPinResult>(
            $"api/v1/device-registration-pins/{pinId}/revoke", bearerToken, request: null, ct);

    public Task<ApiResult<IReadOnlyList<LocationResult>>> ListLocationsAsync(string bearerToken, CancellationToken ct = default) =>
        GetAuthorizedAsync<IReadOnlyList<LocationResult>>("api/v1/locations", bearerToken, ct);

    public Task<ApiResult<IReadOnlyList<DeviceResult>>> ListDevicesAsync(
        string bearerToken, Guid? locationId = null, CancellationToken ct = default)
    {
        var uri = locationId is null ? "api/v1/devices" : $"api/v1/devices?locationId={locationId}";
        return GetAuthorizedAsync<IReadOnlyList<DeviceResult>>(uri, bearerToken, ct);
    }

    public Task<ApiResult<IReadOnlyList<ProductCategoryResult>>> ListProductCategoriesAsync(string bearerToken, CancellationToken ct = default) =>
        GetAuthorizedAsync<IReadOnlyList<ProductCategoryResult>>("api/v1/product-categories", bearerToken, ct);

    public Task<ApiResult<IReadOnlyList<ProductResult>>> ListProductsAsync(string bearerToken, CancellationToken ct = default) =>
        GetAuthorizedAsync<IReadOnlyList<ProductResult>>("api/v1/products", bearerToken, ct);

    public Task<ApiResult<IReadOnlyList<MenuResult>>> ListMenusAsync(string bearerToken, CancellationToken ct = default) =>
        GetAuthorizedAsync<IReadOnlyList<MenuResult>>("api/v1/menus", bearerToken, ct);

    /// <summary>
    /// Terminals page (Milestone C.1) — the smallest Back Office surface that makes a staff
    /// session's TerminalId resolvable at all, since every <c>/api/v1/terminals</c> route is
    /// admin-only.
    /// </summary>
    public Task<ApiResult<IReadOnlyList<TerminalResult>>> ListTerminalsAsync(string bearerToken, CancellationToken ct = default) =>
        GetAuthorizedAsync<IReadOnlyList<TerminalResult>>("api/v1/terminals", bearerToken, ct);

    public Task<ApiResult<TerminalResult>> CreateTerminalAsync(string bearerToken, CreateTerminalRequest request, CancellationToken ct = default) =>
        PostAuthorizedAsync<CreateTerminalRequest, TerminalResult>("api/v1/terminals", bearerToken, request, ct);

    public Task<ApiResult<TerminalResult>> AssignTerminalDeviceAsync(
        string bearerToken, Guid terminalId, AssignTerminalDeviceRequest request, CancellationToken ct = default) =>
        PostAuthorizedAsync<AssignTerminalDeviceRequest, TerminalResult>($"api/v1/terminals/{terminalId}/assign-device", bearerToken, request, ct);

    /// <summary>Explicit-bearer counterpart to <see cref="LogoutAsync"/> for the Back Office session.</summary>
    public async Task<ApiResult> LogoutBackOfficeAsync(string bearerToken, CancellationToken ct = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await httpClient.SendAsync(httpRequest, ct);
            return response.IsSuccessStatusCode
                ? ApiResult.Success(response.StatusCode)
                : ApiResult.FromStatusCode(response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.NetworkFailure(ex.Message);
        }
    }

    private async Task<ApiResult<TResponse>> PostAuthorizedAsync<TRequest, TResponse>(
        string uri, string bearerToken, TRequest? request, CancellationToken ct)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = request is null ? null : JsonContent.Create(request),
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await httpClient.SendAsync(httpRequest, ct);

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

    private async Task<ApiResult<TResponse>> GetAuthorizedAsync<TResponse>(string uri, string bearerToken, CancellationToken ct)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await httpClient.SendAsync(httpRequest, ct);

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

    /// <summary>No-request-body POST (e.g. <c>.../void</c>) — matches <see cref="LogoutAsync"/>'s
    /// null-content style rather than serialising a literal JSON <c>null</c> body.</summary>
    private async Task<ApiResult<TResponse>> PostNoBodyAsync<TResponse>(string uri, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsync(uri, content: null, ct);

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

    private async Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string uri, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.DeleteAsync(uri, ct);

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
