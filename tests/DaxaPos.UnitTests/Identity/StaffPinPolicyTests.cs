using DaxaPos.Application.Identity;

namespace DaxaPos.UnitTests.Identity;

public class StaffPinPolicyTests
{
    [Theory]
    [InlineData("1234")]
    [InlineData("000000")]
    [InlineData("1234567890")] // exactly 10
    public void IsValid_ReturnsTrue_ForFourToTenDigitPins(string pin)
    {
        Assert.True(StaffPinPolicy.IsValid(pin));
    }

    [Theory]
    [InlineData("123")] // too short
    [InlineData("12345678901")] // 11 digits
    [InlineData("12a4")] // letter
    [InlineData("12 34")] // space
    [InlineData("12-34")] // symbol
    [InlineData("")]
    public void IsValid_ReturnsFalse_ForOutOfRangeOrNonDigitPins(string pin)
    {
        Assert.False(StaffPinPolicy.IsValid(pin));
    }

    [Fact]
    public void GeneratePin_ProducesAValidDigitsOnlyPinOfTheConfiguredLength()
    {
        var pin = StaffPinPolicy.GeneratePin();

        Assert.Equal(StaffPinPolicy.GeneratedPinLength, pin.Length);
        Assert.All(pin, c => Assert.InRange(c, '0', '9'));
        Assert.True(StaffPinPolicy.IsValid(pin));
    }

    [Fact]
    public void GeneratePin_VariesAcrossCalls()
    {
        var pins = Enumerable.Range(0, 10).Select(_ => StaffPinPolicy.GeneratePin()).ToHashSet();

        Assert.True(pins.Count > 1);
    }

    [Fact]
    public void LengthLimits_AreFourToTen()
    {
        Assert.Equal(4, StaffPinPolicy.MinLength);
        Assert.Equal(10, StaffPinPolicy.MaxLength);
    }
}
