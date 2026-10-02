using MARS.Shared.Models;
using MARS.TwitchCore.Controllers;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.ChannelRewards;
using MARS.TwitchCore.Tests.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Controllers;

/// <summary>
/// Контроллер реестра наград поверх настоящего менеджера и базы в памяти.
///
/// Проверяются ответы, которые видит панель: найденная награда приходит данными,
/// ненайденная — внятной ошибкой, а не пустым ��телом, по которому непонятно,
/// где причина.
/// </summary>
public class ChannelRewardsManagerControllerTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly ChannelRewardsManager _manager;
    private readonly ChannelRewardsManagerController _controller;

    public ChannelRewardsManagerControllerTests()
    {
        var rewards = new ChannelRewardsService(
            Mock.Of<ITwitchAPI>(),
            new TokenService(
                Mock.Of<ITwitchAPI>(),
                NullLogger<TokenService>.Instance,
                new TwitchTestDbContextFactory()
            ),
            NullLogger<ChannelRewardsService>.Instance,
            new StaticOptionsMonitor<TwitchRewardsOptions>(new TwitchRewardsOptions())
        );

        _manager = new ChannelRewardsManager(
            rewards,
            _factory,
            NullLogger<ChannelRewardsManager>.Instance,
            new ServiceCollection().BuildServiceProvider()
        );

        _controller = new ChannelRewardsManagerController(
            _manager,
            NullLogger<ChannelRewardsManagerController>.Instance,
            new ChannelRewardsSyncService(
                rewards,
                _factory,
                NullLogger<ChannelRewardsSyncService>.Instance
            )
        );
    }

    [Fact]
    public async Task AllRewardsAreReturnedAsEnvelope()
    {
        var result = Unwrap(await _controller.GetAll());

        Assert.True(result.Success);
    }

    [Fact]
    public async Task LocalRewardsAreReturnedAsEnvelope()
    {
        var result = Unwrap(await _controller.GetAllLocal());

        Assert.True(result.Success);
        Assert.Empty(result.Result!);
    }

    [Fact]
    public async Task LocalRewardIsReturnedById()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        var result = Unwrap(await _controller.GetLocalById(stored!.Id));

        Assert.True(result.Success);
        Assert.Equal("награда", result.Result!.Title);
    }

    [Fact]
    public async Task UnknownLocalRewardIsReportedWithId()
    {
        var localId = Guid.NewGuid();

        var result = Unwrap(await _controller.GetLocalById(localId));

        Assert.False(result.Success);
        Assert.Contains(localId.ToString(), result.ErrorMessage);
    }

    /// <summary>
    /// Без токена Twitch ответ не приходит вовсе, и контроллер обязан вернуть
    /// внятную ошибку, а не уронить соединение 500-м.
    /// </summary>
    [Fact]
    public async Task RewardFromTwitchIsReportedAsErrorWhenApiIsUnavailable()
    {
        var result = Unwrap(await _controller.GetById("reward-1"));

        Assert.False(result.Success);
        Assert.Equal("Ошибка при получении награды", result.ErrorMessage);
    }

    [Fact]
    public async Task UpsertStoresRewardAndReturnsIt()
    {
        var result = Unwrap(await _controller.UpsertLocal(Definition("награда", 100)));

        Assert.True(result.Success);
        Assert.Equal(100, result.Result!.Cost);
    }

    [Fact]
    public async Task UpdateReportsFailureForUnknownReward()
    {
        var result = Unwrap(
            await _controller.UpdateLocal(Guid.NewGuid(), new UpdateCustomRewardDto { Cost = 1 })
        );

        Assert.False(result.Success);
        Assert.Equal("Не удалось обновить награду", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAppliesToStoredReward()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        var result = Unwrap(
            await _controller.UpdateLocal(stored!.Id, new UpdateCustomRewardDto { Cost = 250 })
        );

        Assert.True(result.Success);
        Assert.Equal(250, (await _manager.GetLocalByIdAsync(stored.Id))!.Cost);
    }

    [Fact]
    public async Task SoftDeleteReportsFailureForUnknownReward()
    {
        var result = Unwrap(await _controller.SoftDeleteLocal(Guid.NewGuid()));

        Assert.False(result.Success);
        Assert.Equal("Не удалось удалить награду", result.ErrorMessage);
    }

    [Fact]
    public async Task SoftDeleteHidesStoredReward()
    {
        var stored = await _manager.UpsertLocalAsync(Definition("награда", 100));

        var result = Unwrap(await _controller.SoftDeleteLocal(stored!.Id));

        Assert.True(result.Success);
    }

    /// <summary>
    /// Ручная синхронизация завершается отчётом, даже когда Twitch недоступен:
    /// администратор видит «синхронизировано», а не 500.
    /// </summary>
    [Fact]
    public async Task ManualSyncReturnsReportEvenWhenApiIsDown()
    {
        var result = Unwrap(await _controller.SyncNow(TestContext.Current.CancellationToken));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SyncOfServicesReturnsCount()
    {
        var result = Unwrap(await _controller.SyncServicesToLocal());

        Assert.True(result.Success);
        Assert.Equal(0, result.Result);
    }

    private static ChannelRewardDefinition Definition(string title, int cost) =>
        new ProbeRewardService { Title = title, Cost = cost };

    private sealed class ProbeRewardService : ChannelRewardDefinition { }

    private static OperationResult<T> Unwrap<T>(ActionResult<OperationResult<T>> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);

        return Assert.IsType<OperationResult<T>>(ok.Value);
    }

    private static OperationResult Unwrap(ActionResult<OperationResult> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);

        return Assert.IsType<OperationResult>(ok.Value);
    }
}
