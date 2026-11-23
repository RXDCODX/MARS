using MARS.Twitch.Infrastructure.Services.Commands;
using MARS.Twitch.Infrastructure.Services.Validation;
using Microsoft.Extensions.Hosting;
using Moq;
using TwitchLib.Client.Interfaces;
using Wolverine;

namespace MARS.TwitchCore.Tests.Services.Commands;

/// <summary>
/// <see cref="TwitchCommandForwarderService"/> — BackgroundService, детектит команды
/// в чате и публикует <see cref="MARS.Shared.Contracts.Messaging.CommandExecuteRequestedEvent"/>.
/// Проверяется, что сервис поднимается без исключений.
/// </summary>
public class TwitchCommandForwarderServiceTests
{
    [Fact]
    public async Task ExecuteAsyncCompletesImmediately()
    {
        var client = Mock.Of<ITwitchClient>();
        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var validator = Mock.Of<ITwitchEventValidationService>();
        var bus = Mock.Of<IMessageBus>();
        var logger = Microsoft
            .Extensions
            .Logging
            .Abstractions
            .NullLogger<TwitchCommandForwarderService>
            .Instance;

        var service = new TwitchCommandForwarderService(client, lifetime, validator, bus, logger);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
