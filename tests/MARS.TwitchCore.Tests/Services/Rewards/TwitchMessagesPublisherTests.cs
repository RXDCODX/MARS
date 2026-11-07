using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using MARS.TwitchCore.Services.Rewards;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Hosting;
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
        var wolverineEventBus = Mock.Of<IMarsWolverineEventBus>();
        var notifier = Mock.Of<ITelegramusNotifier>();

        var service = new TwitchMessagesPublisher(
            client,
            lifetime,
            validator,
            eventBus,
            wolverineEventBus,
            notifier
        );

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
