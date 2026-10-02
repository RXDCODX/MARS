using MARS.TwitchCore.Data;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.ChannelRewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Кэш наград канала: список от Twitch меняется редко, но опрашивается на каждый
/// чек. Кэш держит две минуты, поэтому проверяются и попадание в кэш, и его
/// сброс — без него после правки награда продолжала бы показываться старой.
/// </summary>
public class RewardsCacheServiceTests
{
    [Fact]
    public async Task RewardsAreFetchedOnFirstCall()
    {
        var calls = 0;
        var cache = Create(() =>
        {
            calls++;
            return Task.FromResult<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>([]);
        });

        await cache.GetRewardsAsync();

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SecondCallIsServedFromCache()
    {
        var calls = 0;
        var cache = Create(() =>
        {
            calls++;
            return Task.FromResult<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>([]);
        });

        await cache.GetRewardsAsync();
        await cache.GetRewardsAsync();

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task InvalidateForcesNextFetch()
    {
        var calls = 0;
        var cache = Create(() =>
        {
            calls++;
            return Task.FromResult<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>([]);
        });

        await cache.GetRewardsAsync();
        await cache.InvalidateCacheAsync();
        await cache.GetRewardsAsync();

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task EmptyCacheInfoIsReportedBeforeFirstFetch()
    {
        var info = Create(() =>
                Task.FromResult<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>([])
            )
            .GetCacheInfo();

        Assert.False(info.IsCached);
        Assert.Null(info.CachedAt);
        Assert.Null(info.CachedRewardsCount);
        Assert.Null(info.TimeToExpire);
    }

    [Fact]
    public async Task FilledCacheInfoCarriesCountAndTtl()
    {
        var cache = Create(() =>
            Task.FromResult<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>([
                Reward("11111111-1111-1111-1111-111111111111", "награда"),
            ])
        );

        await cache.GetRewardsAsync();
        var info = cache.GetCacheInfo();

        Assert.True(info.IsCached);
        Assert.Equal(1, info.CachedRewardsCount);
        Assert.NotNull(info.CachedAt);
        Assert.NotNull(info.TimeToExpire);
        Assert.True(info.TimeToExpire > TimeSpan.Zero);
    }

    [Fact]
    public async Task NullFromLoaderIsNotCached()
    {
        var calls = 0;
        var cache = Create(() =>
        {
            calls++;
            return Task.FromResult<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>(
                null
            );
        });

        Assert.Null(await cache.GetRewardsAsync());
        Assert.Null(await cache.GetRewardsAsync());
        Assert.Equal(2, calls);
        Assert.False(cache.GetCacheInfo().IsCached);
    }

    [Fact]
    public async Task FailureOfLoaderIsNotCachedAndDoesNotThrow()
    {
        var cache = Create(() => throw new InvalidOperationException("twitch недоступен"));

        Assert.Null(await cache.GetRewardsAsync());
        Assert.False(cache.GetCacheInfo().IsCached);
    }

    [Fact]
    public void LoaderIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RewardsCacheService(null!, NullLogger.Instance)
        );
    }

    private static RewardsCacheService Create(
        Func<Task<IEnumerable<TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward>?>> loader
    ) => new(loader, NullLogger.Instance);

    private static TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward Reward(
        string id,
        string title
    ) =>
        (TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward)
            Activator.CreateInstance(
                typeof(TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward),
                nonPublic: true
            )!;
}
