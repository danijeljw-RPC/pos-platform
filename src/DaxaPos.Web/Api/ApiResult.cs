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
}

public sealed record ApiResult<T>(ApiResultKind Kind, T? Value, HttpStatusCode? StatusCode, string? Error)
{
    public static ApiResult<T> Success(T value, HttpStatusCode statusCode) => new(ApiResultKind.Success, value, statusCode, null);

    public static ApiResult<T> FromStatusCode(HttpStatusCode statusCode, string? error = null) =>
        new(Classify(statusCode), default, statusCode, error);

    public static ApiResult<T> NetworkFailure(string error) => new(ApiResultKind.Failed, default, null, error);

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

    public static ApiResult NetworkFailure(string error) => new(ApiResultKind.Failed, null, error);
}
