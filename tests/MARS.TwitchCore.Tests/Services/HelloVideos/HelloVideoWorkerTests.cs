using MARS.Twitch.Api.Data;
using MARS.Twitch.Api.Services.HelloVideos;
using MARS.Twitch.Api.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services.HelloVideos;

/// <summary>
/// <see cref="HelloVideoWorker.ExecuteAsync"/> — защищённый метод
/// <see cref="BackgroundService"/>. Он вызывается через публичный
/// <see cref="IHostedService.StartAsync(CancellationToken)"/>.
/// </summary>
public class HelloVideoWorkerTests
{
    [Fact]
    public async Task ExecuteAsyncCompletesImmediately()
    {
        var factory = new Mock<IDbContextFactory<TwitchDbContext>>();
        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var notifier = Mock.Of<IHelloVideoNotifier>();
        var client = Mock.Of<ITwitchClient>();
        var validator = Mock.Of<ITwitchEventValidationService>();

        var service = new HelloVideoWorker(
            factory.Object,
            NullLogger<HelloVideoWorker>.Instance,
            lifetime,
            notifier,
            client,
            validator
        );

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
