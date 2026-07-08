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
    public const string ConnectionLost = "Can't reach the server. Check your connection — we'll keep trying.";

    /// <summary>
    /// PLAN-0007 Milestone B: distinct from <see cref="ConnectionLost"/> — that wording implies
    /// automatic retry (true for read/poll paths), but order-open/add-line has no idempotency key,
    /// so Milestone B never auto-replays. This message points the staff member at the explicit
    /// Retry action instead.
    /// </summary>
    public const string AddLineNotConfirmed = "This item wasn't confirmed by the server. Check your connection, then tap Retry.";

    /// <summary>
    /// PLAN-0007 Milestone C: shown when <c>RecordPaymentAsync</c> returns
    /// <see cref="ApiResultKind.NetworkFailure"/>. Distinct from <see cref="ConnectionLost"/> (implies
    /// automatic retry — payments never auto-retry) and from a genuine rejection, since the request may
    /// have already reached the server (ack loss). Points staff at the explicit Retry/Check status
    /// actions instead of assuming the payment failed.
    /// </summary>
    public const string PaymentNotConfirmed = "This payment wasn't confirmed by the server. Check your connection, then tap Retry or Check status.";

    /// <summary>
    /// PLAN-0007 Milestone C: shown when a receipt fetch fails for an order the server has already
    /// confirmed <c>Completed</c> — the payment succeeded; only the receipt view failed to load.
    /// </summary>
    public const string ReceiptUnavailable = "Receipt temporarily unavailable. Your payment was recorded — try again to view or print it.";

    /// <summary>
    /// PLAN-0007 Milestone D: shown on <c>Sales</c> when another browser tab of the same device
    /// moved the shared draft order pointer (via a native <c>storage</c> event) or a pre-action
    /// re-check found it had already diverged from the in-memory order. Points staff at an explicit
    /// Refresh rather than silently switching orders under them.
    /// </summary>
    public const string DraftChangedElsewhere = "This order may have changed in another tab. Refresh to see the latest state before continuing.";

    /// <summary>
    /// PLAN-0007 Milestone D: shown on <c>Pay</c> when a pre-submit revalidation finds the order is
    /// no longer payable (completed, voided, or otherwise moved on) — narrows, but does not
    /// eliminate, the two-tabs-same-order double-payment race using only existing read endpoints.
    /// </summary>
    public const string OrderChangedElsewhere = "This order's status changed elsewhere. Refreshing before you continue.";

    public static string ForLoadFailure(ApiResultKind kind, string genericMessage) => kind switch
    {
        ApiResultKind.Unauthorized => SessionExpired,
        ApiResultKind.Forbidden => Forbidden,
        ApiResultKind.NetworkFailure => ConnectionLost,
        _ => genericMessage,
    };
}
