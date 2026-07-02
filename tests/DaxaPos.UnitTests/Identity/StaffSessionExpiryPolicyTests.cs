using DaxaPos.Application.Identity;

namespace DaxaPos.UnitTests.Identity;

public class StaffSessionExpiryPolicyTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 7, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsExpired_ReturnsFalse_WhenFreshlyIssuedAndActive()
    {
        var now = IssuedAt.AddMinutes(5);

        Assert.False(StaffSessionExpiryPolicy.IsExpired(IssuedAt, lastActivityAtUtc: IssuedAt, now));
    }

    [Fact]
    public void IsExpired_ReturnsTrue_PastAbsoluteEightHourLifetime()
    {
        var now = IssuedAt.AddHours(8).AddMinutes(1);

        Assert.True(StaffSessionExpiryPolicy.IsExpired(IssuedAt, lastActivityAtUtc: now, now));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_JustUnderAbsoluteEightHourLifetime()
    {
        var now = IssuedAt.AddHours(7).AddMinutes(59);

        Assert.False(StaffSessionExpiryPolicy.IsExpired(IssuedAt, lastActivityAtUtc: now, now));
    }

    [Fact]
    public void IsExpired_ReturnsTrue_PastThirtyMinuteIdleTimeout_EvenWithinAbsoluteLifetime()
    {
        var lastActivity = IssuedAt.AddHours(1);
        var now = lastActivity.AddMinutes(31);

        Assert.True(StaffSessionExpiryPolicy.IsExpired(IssuedAt, lastActivity, now));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_JustUnderThirtyMinuteIdleTimeout()
    {
        var lastActivity = IssuedAt.AddHours(1);
        var now = lastActivity.AddMinutes(29);

        Assert.False(StaffSessionExpiryPolicy.IsExpired(IssuedAt, lastActivity, now));
    }

    [Fact]
    public void AbsoluteLifetime_IsEightHours()
    {
        Assert.Equal(TimeSpan.FromHours(8), StaffSessionExpiryPolicy.AbsoluteLifetime);
    }

    [Fact]
    public void IdleTimeout_IsThirtyMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), StaffSessionExpiryPolicy.IdleTimeout);
    }
}
