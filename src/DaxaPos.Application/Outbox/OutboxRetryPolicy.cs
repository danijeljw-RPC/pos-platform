namespace DaxaPos.Application.Outbox;

/// <summary>
/// Pure, dependency-free retry/backoff decision for a durable outbox work item (PLAN-0005
/// Milestone E, ADR-0014's Handler I/O Rule) — mirrors <c>PaymentSettlement</c>/
/// <c>RefundSettlement</c>'s style. Never touches the database or a clock service; the caller
/// supplies <c>now</c> and persists whatever this returns.
/// </summary>
public static class OutboxRetryPolicy
{
    /// <summary>Exponential backoff never waits longer than this between attempts.</summary>
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    /// <summary>Default attempt ceiling for a newly-enqueued work item, absent a work-type-specific override.</summary>
    public const int DefaultMaxAttempts = 5;

    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// <paramref name="attemptsSoFar"/> is the number of attempts already made (before the one that
    /// just failed and is asking whether to retry). Reaching <paramref name="maxAttempts"/> means
    /// the work item is permanently failed, not retried again.
    /// </summary>
    public static OutboxRetryDecision Decide(int attemptsSoFar, int maxAttempts, DateTimeOffset now)
    {
        if (attemptsSoFar >= maxAttempts)
        {
            return new OutboxRetryDecision(false, null);
        }

        var backoffSeconds = Math.Min(
            BaseDelay.TotalSeconds * Math.Pow(2, attemptsSoFar),
            MaxBackoff.TotalSeconds);

        return new OutboxRetryDecision(true, now.AddSeconds(backoffSeconds));
    }
}

/// <summary>Whether an outbox work item should be retried and, if so, when.</summary>
public sealed record OutboxRetryDecision(bool ShouldRetry, DateTimeOffset? NextAttemptAtUtc);
