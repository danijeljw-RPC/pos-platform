using DaxaPos.Application.Identity;

namespace DaxaPos.UnitTests.Identity;

public class StaffCodePolicyTests
{
    [Theory]
    [InlineData("DJ")]
    [InlineData("MGR1")]
    [InlineData("BAR01")]
    [InlineData("1234")]
    [InlineData("AB")]
    [InlineData("A1B2C3D4E5F6G7H8I9J0")] // exactly 20
    public void IsValid_ReturnsTrue_ForUppercaseAlphanumericCodesWithinLength(string code)
    {
        Assert.True(StaffCodePolicy.IsValid(code));
    }

    [Theory]
    [InlineData("dj")]
    [InlineData("bar01")]
    public void IsValid_ReturnsTrue_ForLowercaseInput_BecauseValidationNormalisesFirst(string code)
    {
        Assert.True(StaffCodePolicy.IsValid(code));
    }

    [Theory]
    [InlineData("A")] // too short
    [InlineData("A1B2C3D4E5F6G7H8I9J0X")] // 21 chars
    [InlineData("MGR 1")] // space
    [InlineData("MGR-1")] // symbol
    [InlineData("DJ!")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalse_ForOutOfRangeOrNonAlphanumericCodes(string code)
    {
        Assert.False(StaffCodePolicy.IsValid(code));
    }

    [Fact]
    public void Normalize_UppercasesAndTrims()
    {
        Assert.Equal("BAR01", StaffCodePolicy.Normalize("  bar01 "));
    }

    [Fact]
    public void Normalize_LeavesAnAlreadyNormalisedCodeUnchanged()
    {
        Assert.Equal("MGR1", StaffCodePolicy.Normalize("MGR1"));
    }

    [Fact]
    public void LengthLimits_AreTwoToTwenty()
    {
        Assert.Equal(2, StaffCodePolicy.MinLength);
        Assert.Equal(20, StaffCodePolicy.MaxLength);
    }
}
