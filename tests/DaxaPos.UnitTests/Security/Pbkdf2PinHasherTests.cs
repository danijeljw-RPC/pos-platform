using DaxaPos.Infrastructure.Security;

namespace DaxaPos.UnitTests.Security;

public class Pbkdf2PinHasherTests
{
    private readonly Pbkdf2PinHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPin()
    {
        var hash = _hasher.Hash("1234");

        Assert.True(_hasher.Verify("1234", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectPin()
    {
        var hash = _hasher.Hash("1234");

        Assert.False(_hasher.Verify("4321", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSamePin_DueToRandomSalt()
    {
        var hash1 = _hasher.Hash("1234");
        var hash2 = _hasher.Hash("1234");

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.Verify("1234", hash1));
        Assert.True(_hasher.Verify("1234", hash2));
    }

    [Fact]
    public void Hash_DoesNotContainRawPin()
    {
        var hash = _hasher.Hash("1234");

        Assert.DoesNotContain("1234", hash);
    }
}
