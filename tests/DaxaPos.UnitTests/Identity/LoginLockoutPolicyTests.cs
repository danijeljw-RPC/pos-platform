using DaxaPos.Application.Identity;

namespace DaxaPos.UnitTests.Identity;

public class LoginLockoutPolicyTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void ShouldLockOut_ReturnsFalse_BelowThreshold(int failedAttemptCount)
    {
        Assert.False(LoginLockoutPolicy.ShouldLockOut(failedAttemptCount));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void ShouldLockOut_ReturnsTrue_AtOrAboveThreshold(int failedAttemptCount)
    {
        Assert.True(LoginLockoutPolicy.ShouldLockOut(failedAttemptCount));
    }

    [Fact]
    public void LockoutDuration_IsFifteenMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(15), LoginLockoutPolicy.LockoutDuration);
    }

    [Fact]
    public void MaxFailedAttempts_IsFive()
    {
        Assert.Equal(5, LoginLockoutPolicy.MaxFailedAttempts);
    }
}
