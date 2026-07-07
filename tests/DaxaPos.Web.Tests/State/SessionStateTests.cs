using DaxaPos.Web.State;

namespace DaxaPos.Web.Tests.State;

public class SessionStateTests
{
    private static SessionState SampleSession(DateTimeOffset expiresAtUtc) => new(
        SessionToken: "token",
        ExpiresAtUtc: expiresAtUtc,
        StaffMemberId: Guid.NewGuid(),
        DisplayName: "Jane Staff",
        Roles: ["StaffPin"],
        Permissions: ["orders.manage"]);

    [Fact]
    public void IsExpired_WhenNowBeforeExpiry_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var session = SampleSession(now.AddHours(1));

        Assert.False(session.IsExpired(now));
    }

    [Fact]
    public void IsExpired_WhenNowAfterExpiry_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var session = SampleSession(now.AddHours(-1));

        Assert.True(session.IsExpired(now));
    }

    [Fact]
    public void IsExpired_WhenNowEqualsExpiry_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var session = SampleSession(now);

        Assert.True(session.IsExpired(now));
    }

    [Fact]
    public void TerminalId_DefaultsToNull_WhenNotSupplied()
    {
        var session = SampleSession(DateTimeOffset.UtcNow.AddHours(1));

        Assert.Null(session.TerminalId);
    }

    [Fact]
    public void TerminalId_RoundTrips_WhenSupplied()
    {
        var terminalId = Guid.NewGuid();
        var session = SampleSession(DateTimeOffset.UtcNow.AddHours(1)) with { TerminalId = terminalId };

        Assert.Equal(terminalId, session.TerminalId);
    }
}
