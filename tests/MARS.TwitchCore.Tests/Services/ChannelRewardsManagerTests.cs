using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.ChannelRewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Локальный реестр наград поверх настоящей базы в памяти.
///
/// Реестр — источник правды для панели админки: из него читают включение и
/// лимиты. Поэтому проверяется, что повторная синхронизация не плодит дубликаты,
/// а мягкое удаление прячет запись, но не стирает её историю.
/// </summary>
public class ChannelRewardsManagerTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly ChannelRewardsManager _manager;

    public ChannelRewardsManagerTests()
    {
        _manager = new ChannelRewardsManager(
            CreateRewardsService(),
            _factory,
            NullLogger<ChannelRewardsManager>.Instance,
            new ServiceCollection().BuildServiceProvider()
        );
    }

    [Fact]
    public async Task NewDefinitionIsStoredLocally()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        Assert.NotNull(stored);
        Assert.Equal("награда", stored!.Title);
        Assert.Equal(100, stored.Cost);
        Assert.True(stored.IsEnabled);
    }

    /// <summary>
    /// Награда опознаётся и по стоимости, и по названию: иначе после перезапуска
    /// с другим текстом в базе оказались бы две записи об одном треке.
    /// </summary>
    [Theory]
    [InlineData("другое название", 100)]
    [InlineData("награда", 200)]
    public async Task ExistingDefinitionIsUpdatedNotDuplicated(string title, int cost)
    {
        await _manager.UpsertLocalAsync(Definition("награда", 100));

        var updated = await _manager.UpsertLocalAsync(Definition(title, cost));

        Assert.NotNull(updated);
        Assert.Equal(title, updated!.Title);
        Assert.Single(await _manager.GetLocalAsync());
    }

    [Fact]
    public async Task LocalRewardsAreListedByTitle()
    {
        await _manager.UpsertLocalAsync(Definition("вторая", 200));
        await _manager.UpsertLocalAsync(Definition("первая", 100));

        var titles = (await _manager.GetLocalAsync()).Select(reward => reward.Title).ToArray();

        Assert.Equal(["вторая", "первая"], titles);
    }

    [Fact]
    public async Task LocalRewardIsFoundById()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        var found = await _manager.GetLocalByIdAsync(stored!.Id);

        Assert.NotNull(found);
        Assert.Equal("награда", found!.Title);
    }

    [Fact]
    public async Task UnknownLocalRewardIsNotFound()
    {
        Assert.Null(await _manager.GetLocalByIdAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Мягкое удаление прячет запись, а не стирает: по id награды продолжают
    /// приходить события погашения, и без записи они падали бы в никуда.
    /// </summary>
    [Fact]
    public async Task SoftDeleteHidesRewardButKeepsIt()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        Assert.True(await _manager.SoftDeleteLocalAsync(stored!.Id));

        var reloaded = await _manager.GetLocalByIdAsync(stored.Id);
        Assert.True(reloaded!.IsDeleted);
    }

    [Fact]
    public async Task SoftDeleteOfUnknownRewardReportsFailure()
    {
        Assert.False(await _manager.SoftDeleteLocalAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateChangesOnlyGivenFields()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        Assert.True(
            await _manager.UpdateLocalAsync(stored!.Id, new UpdateCustomRewardDto { Cost = 250 })
        );

        var updated = await _manager.GetLocalByIdAsync(stored.Id);
        Assert.Equal(250, updated!.Cost);
        Assert.Equal("награда", updated.Title);
    }

    [Fact]
    public async Task UpdateOfUnknownRewardReportsFailure()
    {
        Assert.False(
            await _manager.UpdateLocalAsync(Guid.NewGuid(), new UpdateCustomRewardDto { Cost = 1 })
        );
    }

    /// <summary>
    /// Синхронизация добавляет только недостающие награды: повторный запуск не
    /// должен ни плодить строки, ни затирать правки администратора.
    /// </summary>
    [Fact]
    public async Task SyncAddsMissingRewardsOnce()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var manager = new ChannelRewardsManager(
            CreateRewardsService(),
            _factory,
            NullLogger<ChannelRewardsManager>.Instance,
            provider
        );

        Assert.Equal(0, await manager.SyncRewardServicesToLocalAsync());
    }

    [Fact]
    public async Task SyncUsesRegisteredRewardServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ChannelRewardDefinition>(Definition(ProbeRewardService.Name, 777));
        var manager = new ChannelRewardsManager(
            CreateRewardsService(),
            _factory,
            NullLogger<ChannelRewardsManager>.Instance,
            services.BuildServiceProvider()
        );

        var added = await manager.SyncRewardServicesToLocalAsync();
        var secondRun = await manager.SyncRewardServicesToLocalAsync();

        Assert.Equal(1, added);
        Assert.Equal(0, secondRun);
        var stored = await manager.GetLocalAsync();
        Assert.Single(stored);
        // Название берётся из имени класса, а не из Title определения:
        // иначе переименование класса разъехалось бы с панелью.
        Assert.Equal("Probe", stored[0].Title);
        Assert.True(stored[0].IsEnabled);
        Assert.Equal("#9146FF", stored[0].BackgroundColor);
    }

    private static ChannelRewardsService CreateRewardsService() =>
        new(
            Mock.Of<ITwitchAPI>(),
            new TokenService(
                Mock.Of<ITwitchAPI>(),
                NullLogger<TokenService>.Instance,
                new TwitchTestDbContextFactory()
            ),
            NullLogger<ChannelRewardsService>.Instance,
            new StaticOptionsMonitor<TwitchRewardsOptions>(new TwitchRewardsOptions())
        );

    private static ChannelRewardDefinition Definition(string title, int cost) =>
        new ProbeRewardService
        {
            Title = title,
            Cost = cost,
            Prompt = "подсказка",
        };

    /// <summary>
    /// Награда-заглушка: имя класса идёт в Title при синхронизации, поэтому класс
    /// назван так, чтобы из него получилось «Пробная».
    /// </summary>
    private sealed class ProbeRewardService : ChannelRewardDefinition
    {
        public const string Name = "Пробная";
    }
}
