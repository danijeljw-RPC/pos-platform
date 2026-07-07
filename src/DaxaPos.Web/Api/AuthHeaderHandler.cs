using System.Net.Http.Headers;
using DaxaPos.Web.State;

namespace DaxaPos.Web.Api;

/// <summary>
/// Attaches the Authorization header the DaxaPos API expects for the current client state
/// (ADR-0015/ADR-0008): a live staff session sends <c>Bearer {sessionToken}</c>; otherwise, if a
/// device is registered, requests fall back to <c>Device {deviceToken}</c> so device-scoped,
/// pre-staff-login calls (e.g. staff-pin login itself) still authenticate. Both stores must
/// already be loaded (see Program.cs startup) — this handler only reads their in-memory
/// <c>Current</c>, it never awaits storage I/O per request.
/// </summary>
/// <remarks>
/// PLAN-0006 Milestone B: if the request already carries an explicit <c>Authorization</c> header,
/// this handler leaves it alone. Back Office calls (<c>DaxaApiClient</c>'s
/// <c>PostAuthorizedAsync</c>/<c>GetAuthorizedAsync</c>) set their own Back Office bearer token
/// explicitly, because the Back Office session (<c>BackOfficeSessionState</c>) is a separate
/// concept from the Terminal shell's staff session this handler resolves implicitly (ADR-0015 §2 —
/// the two "are never interchangeable"). Without this check, this handler would overwrite the
/// explicit Back Office token with whatever the Terminal shell's staff/device state happens to be.
/// </remarks>
public sealed class AuthHeaderHandler(IAuthSessionStore sessionStore, IDeviceContextStore deviceContextStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is not null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        var session = sessionStore.Current;

        if (session is not null && !session.IsExpired(DateTimeOffset.UtcNow))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.SessionToken);
        }
        else if (deviceContextStore.Current is { } deviceContext)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Device", deviceContext.DeviceToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
