using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.ChannelRewards;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Награды канала с недоступным Twitch API.
///
/// Проверяется поведение, ради которого сервис и написан: включён ли он,
/// есть ли токен и что он отвечает, когда Twitch недоступен. Успешный путь
/// проходит через конкретный класс <c>ChannelPoints</c> без интерфейса, а
/// <c>Helix</c> падает ещё до обращения к сети — подменить его нечем, поэтому
/// успешные ответы Twitch проверяются не здесь, а разбором приходит через
/// <see cref="RewardsCacheServiceTests"/>.
/// </summary>
public class ChannelRewardsServiceTests
{
    [Fact]
    public async Task UnavailableTwitchIsReportedAsNotCreated()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        Assert.Null(
            await service.CreateRewardAsync(new CreateCustomRewardsRequest { Title = "награда" })
        );
    }

    [Fact]
    public async Task UnavailableTwitchIsReportedAsNotDeleted()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        Assert.False(await service.DeleteRewardAsync("reward-1"));
    }

    [Fact]
    public async Task UnavailableTwitchYieldsNoReward()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        Assert.Null(await service.GetRewardByIdAsync("reward-1"));
        Assert.Null(await service.GetRewardsAsync());
    }

    [Fact]
    public async Task UnavailableTwitchIsReportedAsNotUpdated()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        Assert.False(
            await service.UpdateRewardAsync(
                "reward-1",
                new UpdateCustomRewardRequest { Cost = 100 }
            )
        );
    }

    /// <summary>
    /// Выключенный сервис не ходит в Twitch: иначе правкой флага можно было бы
    /// упереться в лимит запросов аккаунта, не заметив этого.
    /// </summary>
    [Fact]
    public async Task DisabledServiceSkipsTwitch()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());
        service.IsServiceActive = false;

        Assert.Null(await service.CreateRewardAsync(new CreateCustomRewardsRequest()));
        Assert.False(await service.DeleteRewardAsync("reward-1"));
        Assert.Null(await service.GetRewardByIdAsync("reward-1"));
        Assert.False(await service.UpdateRewardAsync("reward-1", new UpdateCustomRewardRequest()));
        Assert.Null(await service.GetRewardsAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankRewardIdIsRejectedBeforeAnyCall(string rewardId)
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteRewardAsync(rewardId));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRewardByIdAsync(rewardId));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateRewardAsync(rewardId, new UpdateCustomRewardRequest())
        );
    }

    /// <summary>
    /// Без токена вызов отвергается до похода в Twitch: с пустым токеном Twitch
    /// вернул бы 401 на каждый запрос награды.
    /// </summary>
    [Fact]
    public async Task WithoutTokenCallsAreRejected()
    {
        using var service = Create(Mock.Of<ITwitchAPI>(), token: null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            service.CreateRewardAsync(new CreateCustomRewardsRequest())
        );
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.DeleteRewardAsync("reward-1"));
    }

    [Fact]
    public async Task FreshTokenIsTakenFromDatabase()
    {
        var tokenService = await CreateTokenServiceAsync("токен");

        Assert.Equal("токен", tokenService.Token?.AccessToken);
    }

    [Fact]
    public void CostOverrideIsTakenFromOptions()
    {
        using var service = Create(
            Mock.Of<ITwitchAPI>(),
            rewardsOptions: new TwitchRewardsOptions { EnabledByCost = { [100] = false } }
        );

        Assert.False(service.GetEnabledOverrideForCost(100));
        Assert.Null(service.GetEnabledOverrideForCost(200));
    }

    [Fact]
    public void CostOverrideIsAbsentWithoutOptions()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        Assert.Null(service.GetEnabledOverrideForCost(100));
    }

    [Fact]
    public void RewardsMonitorIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ChannelRewardsService(
                Mock.Of<ITwitchAPI>(),
                CreateTokenServiceAsync("токен").GetAwaiter().GetResult(),
                NullLogger<ChannelRewardsService>.Instance,
                null!
            )
        );
    }

    private static ChannelRewardsService Create(
        ITwitchAPI api,
        string? token = "токен",
        TwitchRewardsOptions? rewardsOptions = null
    ) =>
        new(
            api,
            CreateTokenServiceAsync(token).GetAwaiter().GetResult(),
            NullLogger<ChannelRewardsService>.Instance,
            new StaticOptionsMonitor<TwitchRewardsOptions>(
                rewardsOptions ?? new TwitchRewardsOptions()
            )
        );

    /// <summary>
    /// Токен берётся из базы, как в проде: у <c>TokenService.Token</c> сеттер
    /// внутренний, а свежий токен хранится ровно там, откуда его читает сервис.
    /// </summary>
    private static async Task<TokenService> CreateTokenServiceAsync(string? token)
    {
        var factory = new TwitchTestDbContextFactory();
        if (token is not null)
        {
            await using var db = await factory.CreateDbContextAsync(
                TestContext.Current.CancellationToken
            );
            db.TwitchToken.Add(
                new TokenInfo
                {
                    AccessToken = token,
                    RefreshToken = "refresh",
                    ExpiresIn = TimeSpan.FromHours(1),
                    WhenCreated = DateTime.Now,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        return new TokenService(Mock.Of<ITwitchAPI>(), NullLogger<TokenService>.Instance, factory);
    }
}

/// <summary>
/// Монитор опций с фиксированным значением: проверяется чтение переопределения по
/// стоимости, а не подписка на конфигурацию.
/// </summary>
internal sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
