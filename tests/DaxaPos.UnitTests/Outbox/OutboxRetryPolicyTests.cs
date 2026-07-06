using DaxaPos.Application.Outbox;

namespace DaxaPos.UnitTests.Outbox;

/// <summary>
/// PLAN-0005 Milestone E. The genuinely logic-bearing unit this milestone adds — TDD is mandatory
/// per CLAUDE.md's Testing Rules, mirroring <c>PaymentSettlementTests</c>/<c>RefundSettlementTests</c>'
/// pure, dependency-free style for their sibling limits. Proves the outbox retry/backoff decision
/// rule without a database, a clock service, or any I/O: given how many attempts a work item has
/// already made, decide whether it should retry and, if so, when.
/// </summary>
public class OutboxRetryPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Decide_WhenAttemptsBelowMax_ShouldRetry_WithABackoffDelay()
    {
        var decision = OutboxRetryPolicy.Decide(attemptsSoFar: 1, maxAttempts: 5, Now);

        Assert.True(decision.ShouldRetry);
        Assert.NotNull(decision.NextAttemptAtUtc);
        Assert.True(decision.NextAttemptAtUtc > Now);
    }

    [Fact]
    public void Decide_ExactBoundary_OneAttemptBelowMax_StillRetries()
    {
        var decision = OutboxRetryPolicy.Decide(attemptsSoFar: 4, maxAttempts: 5, Now);

        Assert.True(decision.ShouldRetry);
    }

    [Fact]
    public void Decide_ExactBoundary_AttemptsEqualMax_DoesNotRetry()
    {
        var decision = OutboxRetryPolicy.Decide(attemptsSoFar: 5, maxAttempts: 5, Now);

        Assert.False(decision.ShouldRetry);
        Assert.Null(decision.NextAttemptAtUtc);
    }

    [Fact]
    public void Decide_WhenAttemptsExceedMax_DoesNotRetry()
    {
        var decision = OutboxRetryPolicy.Decide(attemptsSoFar: 9, maxAttempts: 5, Now);

        Assert.False(decision.ShouldRetry);
        Assert.Null(decision.NextAttemptAtUtc);
    }

    [Fact]
    public void Decide_BackoffDelay_IncreasesWithEachAttempt()
    {
        var first = OutboxRetryPolicy.Decide(attemptsSoFar: 0, maxAttempts: 10, Now);
        var second = OutboxRetryPolicy.Decide(attemptsSoFar: 1, maxAttempts: 10, Now);
        var third = OutboxRetryPolicy.Decide(attemptsSoFar: 2, maxAttempts: 10, Now);

        Assert.True(first.NextAttemptAtUtc < second.NextAttemptAtUtc);
        Assert.True(second.NextAttemptAtUtc < third.NextAttemptAtUtc);
    }

    [Fact]
    public void Decide_BackoffDelay_NeverExceedsTheConfiguredCap()
    {
        var manyAttemptsIn = OutboxRetryPolicy.Decide(attemptsSoFar: 8, maxAttempts: 20, Now);

        Assert.True(manyAttemptsIn.NextAttemptAtUtc <= Now.Add(OutboxRetryPolicy.MaxBackoff));
    }

    [Fact]
    public void Decide_IsDeterministic_AndTakesNoDependencies()
    {
        // A static class with zero constructor/DI dependencies has nothing to inject a database,
        // HTTP context, or clock into — calling it twice with the same input must produce the same
        // result every time (mirrors RefundSettlementTests' identical proof for its sibling).
        Assert.Equal(
            OutboxRetryPolicy.Decide(2, 5, Now),
            OutboxRetryPolicy.Decide(2, 5, Now));
    }
}
