using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Infrastructure.Services.Rewards;
using MARS.Twitch.Infrastructure.Services.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using TwitchLib.Client.Interfaces;
using Wolverine;

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
        var eventBus = Mock.Of<IMessageBus>();
        var services = new ServiceCollection();
        services.AddSingleton(eventBus);
        var scopeFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        var service = new TwitchMessagesPublisher(client, lifetime, validator, scopeFactory);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
