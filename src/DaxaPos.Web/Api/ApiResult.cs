using System.Net;

namespace DaxaPos.Web.Api;

/// <summary>
/// Outcome of a call to the DaxaPos API. Callers branch on <see cref="Kind"/> instead of catching
/// exceptions, so a 401 ("bad PIN"/"expired session") and a 403 ("forbidden") degrade to a UX
/// state rather than an unhandled error, per PLAN-0006's Milestone A basic 401/403 handling goal.
/// </summary>
public enum ApiResultKind
{
    Success,
    Unauthorized,
    Forbidden,
    Failed,

    /// <summary>
    /// The request never reached the server (DNS/connection/timeout failure), as distinct from
    /// <see cref="Failed"/>, which means a response was received but was not a success/401/403
    /// (PLAN-0007 Milestone A). Callers use this to distinguish "you're offline" from "the server
    /// rejected this."
    /// </summary>
    NetworkFailure,
}

public sealed record ApiResult<T>(ApiResultKind Kind, T? Value, HttpStatusCode? StatusCode, string? Error)
{
    public static ApiResult<T> Success(T value, HttpStatusCode statusCode) => new(ApiResultKind.Success, value, statusCode, null);

    public static ApiResult<T> FromStatusCode(HttpStatusCode statusCode, string? error = null) =>
        new(Classify(statusCode), default, statusCode, error);

    public static ApiResult<T> NetworkFailure(string error) => new(ApiResultKind.NetworkFailure, default, null, error);

    internal static ApiResultKind Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => ApiResultKind.Unauthorized,
        HttpStatusCode.Forbidden => ApiResultKind.Forbidden,
        _ => ApiResultKind.Failed,
    };
}

public sealed record ApiResult(ApiResultKind Kind, HttpStatusCode? StatusCode, string? Error)
{
    public static ApiResult Success(HttpStatusCode statusCode) => new(ApiResultKind.Success, statusCode, null);

    public static ApiResult FromStatusCode(HttpStatusCode statusCode, string? error = null) =>
        new(ApiResult<object>.Classify(statusCode), statusCode, error);

    public static ApiResult NetworkFailure(string error) => new(ApiResultKind.NetworkFailure, null, error);
}
