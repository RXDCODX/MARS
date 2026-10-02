using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Сведения о пользователях Twitch: аватары и цвета чата.
///
/// Бо́льшая часть проверок — на отказы: пустой идентификатор и отсутствие токена
/// обязаны давать «ничего», а не поход в Twitch. Исключение внутри обёрнуто в
/// null и словарь, поэтому недоступность Twitch не должна оставлять сервисы
/// планировщика без данных.
/// </summary>
public class TwitchUserInfoServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankUserIdYieldsNoInfo(string? userId)
    {
        var service = Create();

        Assert.Null(await service.GetUserInfoAsync(userId!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NoTokenYieldsNoInfo()
    {
        var service = Create();

        Assert.Null(
            await service.GetUserInfoAsync("123456789", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnavailableTwitchYieldsNoInfo()
    {
        var service = Create(
            await TwitchTokenServiceStub.WithTokenAsync(
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        Assert.Null(
            await service.GetUserInfoAsync("123456789", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task EmptyIdListYieldsNoUsers()
    {
        var service = Create();

        Assert.Empty(await service.GetUsersInfoAsync([], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BlankIdListYieldsNoUsers()
    {
        var service = Create();

        Assert.Empty(
            await service.GetUsersInfoAsync(["", "   "], TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NoTokenYieldsNoUsers()
    {
        var service = Create();

        Assert.Empty(
            await service.GetUsersInfoAsync(["123456789"], TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnavailableTwitchYieldsNoUsers()
    {
        var service = Create(
            await TwitchTokenServiceStub.WithTokenAsync(
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        Assert.Empty(
            await service.GetUsersInfoAsync(["123456789"], TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task BlankUserIdYieldsNoColor(string? userId)
    {
        var service = Create();

        Assert.Null(
            await service.GetUserChatColorAsync(userId!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NoTokenYieldsNoColor()
    {
        var service = Create();

        Assert.Null(
            await service.GetUserChatColorAsync("123456789", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnavailableTwitchYieldsNoColor()
    {
        var service = Create(
            await TwitchTokenServiceStub.WithTokenAsync(
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        Assert.Null(
            await service.GetUserChatColorAsync("123456789", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task EmptyColorIdListYieldsNoColors()
    {
        var service = Create();

        Assert.Empty(
            await service.GetUsersChatColorsAsync([], TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task BlankColorIdListYieldsNoColors()
    {
        var service = Create();

        Assert.Empty(
            await service.GetUsersChatColorsAsync(["", "  "], TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NoTokenYieldsNoColors()
    {
        var service = Create();

        Assert.Empty(
            await service.GetUsersChatColorsAsync(
                ["123456789"],
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task UnavailableTwitchYieldsNoColors()
    {
        var service = Create(
            await TwitchTokenServiceStub.WithTokenAsync(
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        Assert.Empty(
            await service.GetUsersChatColorsAsync(
                ["123456789"],
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public void NoFollowersYieldsNoAvatarsToFetch()
    {
        var service = Create();

        Assert.Empty(service.GetUsersWithoutAvatars([]));
    }

    /// <summary>
    /// В список попадают только те, у кого аватара нет или он неизвестен:
    /// остальные уже отрисованы, и повторный запрос им ничего не добавит.
    /// </summary>
    [Fact]
    public void UsersWithoutAvatarsAreSelected()
    {
        var service = Create();
        var followers = new List<FollowerInfo>
        {
            new() { UserId = "0", TwitchUser = null },
            new()
            {
                UserId = "1",
                TwitchUser = new TwitchUser
                {
                    TwitchId = "1",
                    UserLogin = "без аватара",
                    DisplayName = "Без аватара",
                    ProfileImageUrl = "   ",
                },
            },
            new()
            {
                UserId = "2",
                TwitchUser = new TwitchUser
                {
                    TwitchId = "2",
                    UserLogin = "с аватаром",
                    DisplayName = "С аватаром",
                    ProfileImageUrl = "https://example.org/a.png",
                },
            },
        };

        var result = service.GetUsersWithoutAvatars(followers);

        Assert.Equal(2, result.Count);
        Assert.Contains("1", result);
    }

    private static TwitchUserInfoService Create(TokenService? tokenService = null) =>
        new(
            Mock.Of<ITwitchAPI>(),
            tokenService ?? TwitchTokenServiceStub.WithoutToken(),
            NullLogger<TwitchUserInfoService>.Instance
        );
}
