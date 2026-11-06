using MARS.TwitchCore.Data;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.BlackList;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services.BlackList;

/// <summary>
/// <see cref="TwitchBlackListService.ExecuteAsync"/> — защищённый метод
/// <see cref="BackgroundService"/>. Он вызывается через публичный
/// <see cref="IHostedService.StartAsync(CancellationToken)"/> и должен
/// завершиться без исключений даже при отменённом токене.
/// </summary>
public class TwitchBlackListServiceTests
{
    [Fact]
    public async Task ExecuteAsyncCompletesImmediately()
    {
        var factory = new Mock<IDbContextFactory<TwitchDbContext>>();
        var api = Mock.Of<ITwitchAPI>();
        var ensureService = Mock.Of<ITwitchUserEnsureService>();
        var lifetime = Mock.Of<IHostApplicationLifetime>();

        var service = new TwitchBlackListService(
            factory.Object,
            NullLogger<TwitchBlackListService>.Instance,
            api,
            ensureService,
            lifetime
        );

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
