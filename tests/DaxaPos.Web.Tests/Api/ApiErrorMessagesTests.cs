using DaxaPos.Web.Api;

namespace DaxaPos.Web.Tests.Api;

public class ApiErrorMessagesTests
{
    [Fact]
    public void ForLoadFailure_Unauthorized_ReturnsSessionExpiredMessage()
    {
        Assert.Equal(ApiErrorMessages.SessionExpired, ApiErrorMessages.ForLoadFailure(ApiResultKind.Unauthorized, "generic"));
    }

    [Fact]
    public void ForLoadFailure_Forbidden_ReturnsForbiddenMessage()
    {
        Assert.Equal(ApiErrorMessages.Forbidden, ApiErrorMessages.ForLoadFailure(ApiResultKind.Forbidden, "generic"));
    }

    [Fact]
    public void ForLoadFailure_Failed_ReturnsGenericMessage()
    {
        Assert.Equal("generic", ApiErrorMessages.ForLoadFailure(ApiResultKind.Failed, "generic"));
    }

    [Fact]
    public void ForLoadFailure_NetworkFailure_ReturnsConnectionLostMessage()
    {
        Assert.Equal(ApiErrorMessages.ConnectionLost, ApiErrorMessages.ForLoadFailure(ApiResultKind.NetworkFailure, "generic"));
    }
}
