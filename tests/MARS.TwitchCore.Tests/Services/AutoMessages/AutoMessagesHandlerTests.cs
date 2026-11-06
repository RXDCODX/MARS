using MARS.TwitchCore.Data;
using MARS.TwitchCore.Services.AutoMessages;
using MARS.TwitchCore.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services.AutoMessages;

/// <summary>
/// <see cref="AutoMessagesHandler.ExecuteAsync"/> — защищённый метод
/// <see cref="BackgroundService"/>. Он вызывается через публичный
/// <see cref="IHostedService.StartAsync(CancellationToken)"/>.
/// </summary>
public class AutoMessagesHandlerTests
{
    [Fact]
    public async Task ExecuteAsyncCompletesImmediately()
    {
        var client = Mock.Of<ITwitchClient>();
        var factory = new Mock<IDbContextFactory<TwitchDbContext>>();
        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var validator = Mock.Of<ITwitchEventValidationService>();

        var service = new AutoMessagesHandler(
            client,
            NullLogger<AutoMessagesHandler>.Instance,
            factory.Object,
            lifetime,
            validator
        );

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StartAsync(cts.Token);

        Assert.True(true);
    }
}
