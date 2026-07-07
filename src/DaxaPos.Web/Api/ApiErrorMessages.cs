namespace DaxaPos.Web.Api;

/// <summary>
/// Maps a failed <see cref="ApiResult{T}"/>'s <see cref="ApiResultKind"/> to a distinct,
/// user-facing message for a read/load failure, so an expired session or a missing permission
/// shows a specific, actionable message instead of the same generic wording as a network blip or
/// a 404/500 (PLAN-0006 Milestone G RBAC/UX sweep).
/// </summary>
public static class ApiErrorMessages
{
    public const string SessionExpired = "Your session has expired. Please log in again.";
    public const string Forbidden = "You don't have permission to view this.";

    public static string ForLoadFailure(ApiResultKind kind, string genericMessage) => kind switch
    {
        ApiResultKind.Unauthorized => SessionExpired,
        ApiResultKind.Forbidden => Forbidden,
        _ => genericMessage,
    };
}
