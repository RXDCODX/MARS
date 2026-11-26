using MARS.Twitch.Infrastructure.Services.Management;

namespace MARS.Twitch.Tests.Services;

public class TwitchAuthorizationLinkTests
{
    [Fact]
    public void BuildRequestsIrcChatScopes()
    {
        var link = TwitchAuthorizationLink.Build("id", "http://localhost/twitch-auth/callback");

        Assert.Contains("chat:read", link, StringComparison.Ordinal);
        Assert.Contains("chat:edit", link, StringComparison.Ordinal);
        Assert.DoesNotContain("chat:write", link, StringComparison.Ordinal);
    }
}
