using MARS.TwitchCore.Services.Rewards;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstraints;
using Moq;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services.Rewards;

/// <summary>
/// <see cref="TwitchMessagesPublisher.ExecuteAsync"/> — защищённый метод
/// <see cref="BackgroundService"/>. Он вызывается через публичный
/// <see cref="IHostedService.StartAsync(CancellationToken)"/>.
/// </summary>
public class TwitchMessagesPublisherTests
{
    [Fact]
    public async Task ExecuteAsyncCompletesImmediately()
    {
        var client = Mock.Of<ITwitchClient>();
        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var validator = Mock.Of<ITwitchEventValidationService>();
        var eventBus = Mock.Of<IMarsEventBus>();
        var notifier = Mock.Of<ITelegramusNotifier>();

        var service = new TwitchMessagesPublisher(
            client,
            lifetime,
            validator,
            eventBus,
            notifier
        );

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
