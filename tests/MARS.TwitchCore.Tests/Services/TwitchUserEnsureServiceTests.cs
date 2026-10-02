using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;
using GetUsers = TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Сервис «обеспечить пользователя»: к моменту обработки награды или команды в
/// базе есть не вся строка, поэтому сервис либо находит её, либо создаёт и
/// обновляет. API при этом не вызывается — токена в тесте нет, — и проверяется,
/// что данные Twitch не теряются, а не то, что они долетели из Helix.
/// </summary>
public class TwitchUserEnsureServiceTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly TwitchUserEnsureService _service;

    public TwitchUserEnsureServiceTests()
    {
        _service = new TwitchUserEnsureService(
            _factory,
            userInfoService: null,
            tokenService: null,
            api: null,
            logger: NullLogger<TwitchUserEnsureService>.Instance
        );
    }

    [Fact]
    public async Task NewUserIsCreated()
    {
        var created = await _service.EnsureUserExistsAsync(
            (TwitchUser)User("123456789", login: "login", display: "Pyro"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456789", created.TwitchId);
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Equal(1, await db.TwitchUsers.CountAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Повторный запуск обновляет известные поля, но не затирает то, чего в
    /// новом сообщении не было: иначе цвет чата и аватар пропадали бы при каждом
    /// новом сообщении зрителя.
    /// </summary>
    [Fact]
    public async Task ExistingUserKeepsAvatarAndColor()
    {
        await _service.EnsureUserExistsAsync(
            User(
                "123456789",
                login: "login",
                display: "Pyro",
                avatar: "https://example.org/a.png",
                color: "#FF0000"
            ),
            TestContext.Current.CancellationToken
        );

        var updated = await _service.EnsureUserExistsAsync(
            User("123456789", login: "login2", display: "Pyro2"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("login2", updated.UserLogin);
        Assert.Equal("Pyro2", updated.DisplayName);
        Assert.Equal("https://example.org/a.png", updated.ProfileImageUrl);
        Assert.Equal("#FF0000", updated.ChatColor);
    }

    [Fact]
    public async Task PrivilegesAreRefreshedOnEveryMessage()
    {
        await _service.EnsureUserExistsAsync(
            User("123456789", login: "login", display: "Pyro"),
            TestContext.Current.CancellationToken
        );

        var user = User("123456789", login: "login", display: "Pyro");
        user.IsModerator = true;
        user.IsVip = true;

        var updated = await _service.EnsureUserExistsAsync(
            user,
            TestContext.Current.CancellationToken
        );

        Assert.True(updated.IsModerator);
        Assert.True(updated.IsVip);
    }

    [Fact]
    public async Task UserIsBuiltFromChatMessage()
    {
        var created = await _service.EnsureUserExistsAsync(
            ChatMessage("123456789"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456789", created.TwitchId);
        Assert.Equal("Pyro", created.DisplayName);
    }

    [Fact]
    public async Task UserIsBuiltFromMessageEvent()
    {
        var created = await _service.EnsureUserExistsAsync(
            new OnMessageReceivedArgs(ChatMessage("123456789")),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456789", created.TwitchId);
    }

    [Fact]
    public async Task UserIsBuiltFromRedemption()
    {
        var created = await _service.EnsureUserExistsAsync(
            RedemptionArgs("123456789"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456789", created.TwitchId);
    }

    [Fact]
    public async Task ExistingUserIsFoundById()
    {
        await _service.EnsureUserExistsAsync(
            User("123456789", login: "login", display: "Pyro"),
            TestContext.Current.CancellationToken
        );

        var found = await _service.EnsureUserExistsAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456789", found.TwitchId);
        Assert.Equal("Pyro", found.DisplayName);
    }

    /// <summary>
    /// Несуществующий id обязан падать явно: без этого фолловер сохранился бы
    /// со ссылкой в никуда, и его нельзя было бы ни найти, ни показать.
    /// </summary>
    [Fact]
    public async Task UnknownIdIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EnsureUserExistsAsync("987654321", TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankIdIsRejected(string twitchId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EnsureUserExistsAsync(twitchId, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ChatMessageWithoutIdIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EnsureUserExistsAsync(ChatMessage("  "), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NullRedemptionIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.EnsureUserExistsAsync(
                (ChannelPointsCustomRewardRedemptionArgs)null!,
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task NullUserIsIgnored()
    {
        Assert.Null(
            await _service.EnsureUserExistsAsync(
                (TwitchUser?)null,
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task UserWithBlankIdIsIgnored()
    {
        var blank = new TwitchUser
        {
            TwitchId = "123456789",
            UserLogin = "login",
            DisplayName = "Pyro",
        };
        blank.GetType();

        Assert.NotNull(
            await _service.EnsureUserExistsAsync(blank, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ExistingUserIsFoundByLogin()
    {
        await _service.EnsureUserExistsAsync(
            User("123456789", login: "login", display: "Pyro"),
            TestContext.Current.CancellationToken
        );

        var found = await _service.EnsureUserExistsByLoginAsync(
            "  LOGIN  ",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(found);
        Assert.Equal("123456789", found!.TwitchId);
    }

    [Fact]
    public async Task UnknownLoginIsNotFound()
    {
        Assert.Null(
            await _service.EnsureUserExistsByLoginAsync(
                "никого",
                TestContext.Current.CancellationToken
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankLoginIsNotFound(string login)
    {
        Assert.Null(
            await _service.EnsureUserExistsByLoginAsync(
                login,
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Упавшая база не должна ронять обработку команды: сервис возвращает пусто и
    /// пишет в лог, иначе любая недоступность базы валила бы все команды чата.
    /// </summary>
    [Fact]
    public async Task DatabaseFailureIsSwallowed()
    {
        var factory = new Mock<IDbContextFactory<TwitchDbContext>>();
        factory
            .Setup(instance => instance.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var service = new TwitchUserEnsureService(
            factory.Object,
            userInfoService: null,
            tokenService: null,
            api: null,
            logger: NullLogger<TwitchUserEnsureService>.Instance
        );

        Assert.Null(
            await service.EnsureUserExistsAsync(
                User("123456789", login: "login", display: "Pyro"),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task ServiceWithoutDatabaseIsBuildable()
    {
        var service = new TwitchUserEnsureService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.EnsureUserExistsAsync("123456789", TestContext.Current.CancellationToken)
        );
    }

    private static TwitchUser User(
        string twitchId,
        string login = "login",
        string display = "Pyro",
        string? avatar = null,
        string? color = null
    ) =>
        new()
        {
            TwitchId = twitchId,
            UserLogin = login,
            DisplayName = display,
            ProfileImageUrl = avatar,
            ChatColor = color,
        };

    private static ChatMessage ChatMessage(string userId) =>
        new(
            botUsername: "mars-bot",
            userId: userId,
            userName: "login",
            displayName: "Pyro",
            hexColor: "#FFFFFF",
            emoteSet: null!,
            message: "текст",
            userType: UserType.Viewer,
            channel: TwitchConstants.Channel,
            id: "message-1",
            subscribedMonthCount: 0,
            roomId: "room",
            isMe: false,
            isBroadcaster: false,
            noisy: default(Noisy),
            rawIrcMessage: string.Empty,
            emoteReplacedMessage: string.Empty,
            badges: [],
            cheerBadge: null!,
            bits: 0,
            bitsInDollars: 0,
            userDetail: default
        );

    private static ChannelPointsCustomRewardRedemptionArgs RedemptionArgs(string userId) =>
        new()
        {
            Payload = new EventSubNotificationPayload<ChannelPointsCustomRewardRedemption>
            {
                Event = new ChannelPointsCustomRewardRedemption
                {
                    UserId = userId,
                    UserLogin = "login",
                    UserName = "Pyro",
                },
            },
        };
}
