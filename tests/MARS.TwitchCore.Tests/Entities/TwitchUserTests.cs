using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;
using GetChannelVIPs = TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using GetModerators = TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using GetUsers = TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace MARS.TwitchCore.Tests.Entities;

/// <summary>
/// Пользователь Twitch: проверяется, что из события TwitchLib собирается именно
/// тот пользователь, которого дальше видно в правах команд.
///
/// Идентификатор проверяется в сеттере, поэтому нечисловой id обязан ронять
/// создание объекта: иначе в базу попал бы мусор, по которому не найти ни
/// награду, ни права.
/// </summary>
public class TwitchUserTests
{
    [Fact]
    public void ValidTwitchIdIsAccepted()
    {
        Assert.Equal("123456789", User("123456789").TwitchId);
    }

    [Theory]
    [InlineData("не число")]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidTwitchIdIsRejected(string twitchId)
    {
        Assert.Throws<ArgumentException>(() => User(twitchId));
    }

    [Fact]
    public void BroadcasterIsRecognizedByChannelId()
    {
        Assert.True(User(TwitchConstants.ChannelId).IsBroadcaster);
        Assert.False(User("123456789").IsBroadcaster);
    }

    /// <summary>
    /// Обычный зритель — тот, у кого нет ни модераторки, ни VIP и кто не
    /// стример: именно такие получают обычные права команд.
    /// </summary>
    [Fact]
    public void SimpleUserHasNoPrivileges()
    {
        var user = User("123456789");

        Assert.True(user.IsSimpleUser);
        Assert.False(user.IsInBlockList);
    }

    [Fact]
    public void ModeratorIsNotSimpleUser()
    {
        var user = User("123456789");
        user.IsModerator = true;

        Assert.False(user.IsSimpleUser);
    }

    [Fact]
    public void VipIsNotSimpleUser()
    {
        var user = User("123456789");
        user.IsVip = true;

        Assert.False(user.IsSimpleUser);
    }

    [Fact]
    public void EqualityIsByTwitchId()
    {
        var first = User("123456789", login: "login-1", display: "Первый");
        var second = User("123456789", login: "login-2", display: "Второй");
        var other = User("987654321");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void EqualityWithForeignTypeIsFalse()
    {
        Assert.False(User("123456789").Equals(null));
        Assert.False(User("123456789").Equals("строка"));
    }

    [Fact]
    public void ToStringKeepsNameLoginAndId()
    {
        Assert.Equal("Pyro (login) - ID: 123456789", User("123456789").ToString());
    }

    [Fact]
    public void UserIsBuiltFromChatMessage()
    {
        var user = TwitchUser.FromChatMessage(ChatMessage(userId: "123456789"));

        Assert.NotNull(user);
        Assert.Equal("123456789", user!.TwitchId);
        Assert.Equal("login", user.UserLogin);
        Assert.Equal("Pyro", user.DisplayName);
        Assert.Equal("#FFFFFF", user.ChatColor);
        Assert.False(user.IsModerator);
        Assert.False(user.IsVip);
        Assert.True(user.IsSimpleUser);
    }

    [Fact]
    public void ChatMessageWithoutUserIdYieldsNoUser()
    {
        Assert.Null(TwitchUser.FromChatMessage(ChatMessage(userId: "  ")));
    }

    [Fact]
    public void NullChatMessageYieldsNoUser()
    {
        Assert.Null(TwitchUser.FromChatMessage(null));
    }

    [Fact]
    public void UserIsBuiltFromMessageEvent()
    {
        var user = TwitchUser.FromOnMessageReceivedArgs(
            new OnMessageReceivedArgs(ChatMessage(userId: "123456789"))
        );

        Assert.NotNull(user);
        Assert.Equal("123456789", user!.TwitchId);
    }

    [Fact]
    public void NullEventYieldsNoUser()
    {
        Assert.Null(TwitchUser.FromOnMessageReceivedArgs(null));
    }

    [Fact]
    public void UserIsBuiltFromRedemption()
    {
        var user = TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(
            RedemptionArgs("123456789")
        );

        Assert.NotNull(user);
        Assert.Equal("123456789", user!.TwitchId);
        Assert.Equal("login", user.UserLogin);
        Assert.Equal("Pyro", user.DisplayName);
        Assert.False(user.IsModerator);
        Assert.False(user.IsVip);
    }

    /// <summary>
    /// Награду может погасить кто угодно, но не с пустым или нечисловым id:
    /// событие без такого id не должно порождать пользователя.
    /// </summary>
    [Fact]
    public void RedemptionWithoutEventYieldsNoUser()
    {
        Assert.Null(TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(null));
        Assert.Null(
            TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(
                new ChannelPointsCustomRewardRedemptionArgs()
            )
        );
    }

    [Fact]
    public void RedemptionWithInvalidUserIdYieldsNoUser()
    {
        Assert.Null(
            TwitchUser.FromChannelPointsCustomRewardRedemptionArgs(RedemptionArgs("не-id"))
        );
    }

    [Fact]
    public void UserIsBuiltFromApiUser()
    {
        var user = TwitchUser.FromUser(
            TwitchLibModels.Create<GetUsers.User>(
                ("Id", "123456789"),
                ("Login", "login"),
                ("DisplayName", "Pyro"),
                ("ProfileImageUrl", "https://example.org/avatar.png")
            )
        );

        Assert.Equal("123456789", user!.TwitchId);
        Assert.Equal("login", user.UserLogin);
        Assert.Equal("Pyro", user.DisplayName);
        Assert.Equal("https://example.org/avatar.png", user.ProfileImageUrl);
        Assert.False(user.IsModerator);
    }

    /// <summary>
    /// У API-пользователя имя и отображаемое имя могут не прийти: без них в панели
    /// была бы пустая строка, и искать пользователя было бы нечем.
    /// </summary>
    [Fact]
    public void ApiUserWithoutNamesGetsGeneratedFallbacks()
    {
        var user = TwitchUser.FromUser(TwitchLibModels.Create<GetUsers.User>(("Id", "123456789")));

        Assert.Equal("user_123456789", user!.UserLogin);
        Assert.Equal("User123456789", user.DisplayName);
    }

    [Fact]
    public void ModeratorIsBuiltWithModeratorFlag()
    {
        var user = TwitchUser.FromModerator(
            TwitchLibModels.Create<GetModerators.Moderator>(
                ("UserId", "123456789"),
                ("UserLogin", "login"),
                ("UserName", "Pyro")
            )
        );

        Assert.True(user.IsModerator);
        Assert.False(user.IsVip);
        Assert.Equal("123456789", user.TwitchId);
        Assert.False(user.IsSimpleUser);
    }

    [Fact]
    public void VipIsBuiltWithVipFlag()
    {
        var user = TwitchUser.FromVip(
            TwitchLibModels.Create<GetChannelVIPs.ChannelVIPsResponseModel>(
                ("UserId", "123456789"),
                ("UserLogin", "login"),
                ("UserName", "Pyro")
            )
        )!;

        Assert.True(user.IsVip);
        Assert.False(user.IsModerator);
        Assert.Equal("Pyro", user.DisplayName);
    }

    [Fact]
    public void ApiUserKeepsCreationDate()
    {
        var createdAt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var user = TwitchUser.FromApiUser(
            TwitchLibModels.Create<GetUsers.User>(
                ("Id", "123456789"),
                ("Login", "login"),
                ("DisplayName", "Pyro"),
                ("CreatedAt", createdAt)
            )
        )!;

        Assert.Equal(createdAt, user.CreatedAt);
        Assert.Equal("login", user.UserLogin);
    }

    private static TwitchUser User(
        string twitchId,
        string login = "login",
        string display = "Pyro"
    ) =>
        new()
        {
            TwitchId = twitchId,
            UserLogin = login,
            DisplayName = display,
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
