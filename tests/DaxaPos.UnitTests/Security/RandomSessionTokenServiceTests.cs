using DaxaPos.Infrastructure.Security;

namespace DaxaPos.UnitTests.Security;

public class RandomSessionTokenServiceTests
{
    private readonly RandomSessionTokenService _service = new();

    [Fact]
    public void GenerateToken_ProducesNonEmptyHighEntropyValue()
    {
        var token = _service.GenerateToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(token.Length >= 32);
    }

    [Fact]
    public void GenerateToken_ProducesDifferentValues_OnEachCall()
    {
        var token1 = _service.GenerateToken();
        var token2 = _service.GenerateToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Hash_IsDeterministic_ForTheSameToken()
    {
        var token = _service.GenerateToken();

        var hash1 = _service.Hash(token);
        var hash2 = _service.Hash(token);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_ProducesDifferentValues_ForDifferentTokens()
    {
        var token1 = _service.GenerateToken();
        var token2 = _service.GenerateToken();

        Assert.NotEqual(_service.Hash(token1), _service.Hash(token2));
    }

    [Fact]
    public void Hash_DoesNotReturnTheRawToken()
    {
        var token = _service.GenerateToken();

        Assert.NotEqual(token, _service.Hash(token));
    }
}
