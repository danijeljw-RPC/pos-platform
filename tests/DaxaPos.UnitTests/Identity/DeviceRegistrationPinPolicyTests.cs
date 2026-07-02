using DaxaPos.Application.Identity;

namespace DaxaPos.UnitTests.Identity;

public class DeviceRegistrationPinPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(-1, false)]
    public void IsValidMaxUses_EnforcesTheApprovedRange(int maxUses, bool expected) =>
        Assert.Equal(expected, DeviceRegistrationPinPolicy.IsValidMaxUses(maxUses));

    [Fact]
    public void IsUsable_ReturnsTrue_ForALivePin() =>
        Assert.True(DeviceRegistrationPinPolicy.IsUsable(
            expiresAtUtc: Now.AddMinutes(5), revokedAtUtc: null, usedCount: 0, maxUses: 1, nowUtc: Now));

    [Fact]
    public void IsUsable_ReturnsFalse_WhenRevoked() =>
        Assert.False(DeviceRegistrationPinPolicy.IsUsable(
            expiresAtUtc: Now.AddMinutes(5), revokedAtUtc: Now.AddMinutes(-1), usedCount: 0, maxUses: 1, nowUtc: Now));

    [Fact]
    public void IsUsable_ReturnsFalse_WhenExpired() =>
        Assert.False(DeviceRegistrationPinPolicy.IsUsable(
            expiresAtUtc: Now.AddMinutes(-1), revokedAtUtc: null, usedCount: 0, maxUses: 1, nowUtc: Now));

    [Fact]
    public void IsUsable_ReturnsFalse_AtTheExactExpiryInstant() =>
        Assert.False(DeviceRegistrationPinPolicy.IsUsable(
            expiresAtUtc: Now, revokedAtUtc: null, usedCount: 0, maxUses: 1, nowUtc: Now));

    [Fact]
    public void IsUsable_ReturnsFalse_WhenExhausted() =>
        Assert.False(DeviceRegistrationPinPolicy.IsUsable(
            expiresAtUtc: Now.AddMinutes(5), revokedAtUtc: null, usedCount: 1, maxUses: 1, nowUtc: Now));

    [Fact]
    public void IsUsable_ReturnsTrue_WithOneUseRemaining() =>
        Assert.True(DeviceRegistrationPinPolicy.IsUsable(
            expiresAtUtc: Now.AddMinutes(5), revokedAtUtc: null, usedCount: 4, maxUses: 5, nowUtc: Now));

    [Fact]
    public void ApprovedDefaults_MatchTheRecordedDecision()
    {
        Assert.Equal(6, DeviceRegistrationPinPolicy.PinLength);
        Assert.Equal(TimeSpan.FromMinutes(15), DeviceRegistrationPinPolicy.Lifetime);
        Assert.Equal(1, DeviceRegistrationPinPolicy.DefaultMaxUses);
        Assert.Equal(10, DeviceRegistrationPinPolicy.RegistrationRateLimitPermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), DeviceRegistrationPinPolicy.RegistrationRateLimitWindow);
    }
}
