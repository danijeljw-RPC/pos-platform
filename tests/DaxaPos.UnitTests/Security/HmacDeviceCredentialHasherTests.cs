using DaxaPos.Infrastructure.Security;

namespace DaxaPos.UnitTests.Security;

public class HmacDeviceCredentialHasherTests
{
    private readonly HmacDeviceCredentialHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectSecret()
    {
        var secret = Guid.NewGuid().ToString("N");
        var hash = _hasher.Hash(secret);

        Assert.True(_hasher.Verify(secret, hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectSecret()
    {
        var hash = _hasher.Hash(Guid.NewGuid().ToString("N"));

        Assert.False(_hasher.Verify(Guid.NewGuid().ToString("N"), hash));
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSameSecret_DueToRandomSalt()
    {
        var secret = Guid.NewGuid().ToString("N");

        var hash1 = _hasher.Hash(secret);
        var hash2 = _hasher.Hash(secret);

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.Verify(secret, hash1));
        Assert.True(_hasher.Verify(secret, hash2));
    }

    [Fact]
    public void Hash_DoesNotContainRawSecret()
    {
        var secret = Guid.NewGuid().ToString("N");
        var hash = _hasher.Hash(secret);

        Assert.DoesNotContain(secret, hash);
    }
}
