using DaxaPos.Application.Identity;

namespace DaxaPos.UnitTests.Identity;

public class SessionExpiryPolicyTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 7, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsExpired_ReturnsFalse_WhenFreshlyIssuedAndActive()
    {
        var now = IssuedAt.AddMinutes(5);

        Assert.False(SessionExpiryPolicy.IsExpired(IssuedAt, lastActivityAtUtc: IssuedAt, now));
    }

    [Fact]
    public void IsExpired_ReturnsTrue_PastAbsoluteTwelveHourLifetime()
    {
        var now = IssuedAt.AddHours(12).AddMinutes(1);

        Assert.True(SessionExpiryPolicy.IsExpired(IssuedAt, lastActivityAtUtc: now, now));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_JustUnderAbsoluteTwelveHourLifetime()
    {
        var now = IssuedAt.AddHours(11).AddMinutes(59);

        Assert.False(SessionExpiryPolicy.IsExpired(IssuedAt, lastActivityAtUtc: now, now));
    }

    [Fact]
    public void IsExpired_ReturnsTrue_PastEightHourIdleTimeout_EvenWithinAbsoluteLifetime()
    {
        var lastActivity = IssuedAt.AddHours(1);
        var now = lastActivity.AddHours(8).AddMinutes(1);

        Assert.True(SessionExpiryPolicy.IsExpired(IssuedAt, lastActivity, now));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_JustUnderEightHourIdleTimeout()
    {
        var lastActivity = IssuedAt.AddHours(1);
        var now = lastActivity.AddHours(7).AddMinutes(59);

        Assert.False(SessionExpiryPolicy.IsExpired(IssuedAt, lastActivity, now));
    }

    [Fact]
    public void AbsoluteLifetime_IsTwelveHours()
    {
        Assert.Equal(TimeSpan.FromHours(12), SessionExpiryPolicy.AbsoluteLifetime);
    }

    [Fact]
    public void IdleTimeout_IsEightHours()
    {
        Assert.Equal(TimeSpan.FromHours(8), SessionExpiryPolicy.IdleTimeout);
    }
}
