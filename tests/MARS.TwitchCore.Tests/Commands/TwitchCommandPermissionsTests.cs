using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Commands;

namespace MARS.TwitchCore.Tests.Commands;

/// <summary>
/// Права на Twitch решаются в сервисе платформы, потому что только он знает
/// личность пишущего, и передаются в MARS.Commands полем <c>is_admin</c>.
/// В монолите то же решение жило в <c>TwitchCommandService.IsAdmin</c> и было
/// <c>userId == TwitchExstension.ChannelId</c> — то есть только стример, модераторы
/// не проходили. Здесь модератор допускается: так же поступали валидаторы наград и
/// <c>TwitchTitleChangeCommand</c>, где прямо написано «только модераторам и
/// стримеру», иначе админ-команда <c>title</c> стала бы недоступна тому, ради кого
/// и писалась.
/// </summary>
public sealed class TwitchCommandPermissionsTests
{
    [Fact]
    public void BroadcasterIsAdmin()
    {
        Assert.True(
            TwitchCommandPermissions.IsAdmin(TwitchConstants.ChannelId, isModerator: false)
        );
    }

    [Fact]
    public void ModeratorIsAdmin()
    {
        Assert.True(TwitchCommandPermissions.IsAdmin("some-mod", isModerator: true));
    }

    [Fact]
    public void RegularViewerIsNotAdmin()
    {
        Assert.False(TwitchCommandPermissions.IsAdmin("some-viewer", isModerator: false));
    }

    /// <summary>
    /// Стример по умолчанию модератором не помечается в сообщениях чата, поэтому
    /// сверка с каналом обязана быть отдельной проверкой, а не частью флага.
    /// </summary>
    [Fact]
    public void BroadcasterCheckIsCaseInsensitive()
    {
        Assert.True(
            TwitchCommandPermissions.IsAdmin(
                TwitchConstants.ChannelId.ToUpperInvariant(),
                isModerator: false
            )
        );
    }

    [Fact]
    public void EmptyUserIdIsNotAdmin()
    {
        Assert.False(TwitchCommandPermissions.IsAdmin(string.Empty, isModerator: true));
    }
}
