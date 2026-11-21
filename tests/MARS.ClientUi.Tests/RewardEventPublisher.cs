using System.Reflection;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Издатель события награды для сквозных тестов.
/// </summary>
/// <remarks>
/// Публикует <see cref="RewardRedeemedEvent"/> ровно тем же конвейером, что и
/// <c>MARS.Twitch.Api.Services.Rewards.RewardRedemptionPublisher</c>: Wolverine поверх
/// RabbitMQ с общими правилами именования из <c>AddMarsWolverine</c>. Руками собирать
/// envelope RabbitMQ нельзя — его формат зависит от версии Wolverine и ломался бы
/// молча при обновлении пакета.
/// </remarks>
public sealed class RewardEventPublisher : IAsyncDisposable
{
    private readonly WebApplication _app;

    private RewardEventPublisher(WebApplication app)
    {
        _app = app;
    }

    public static async Task<RewardEventPublisher> StartAsync(
        string host,
        int port,
        string userName,
        string password,
        CancellationToken cancellationToken
    )
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = host,
                ["RabbitMq:Port"] = port.ToString(),
                ["RabbitMq:UserName"] = userName,
                ["RabbitMq:Password"] = password,
            }
        );

        // Сборка контрактов — только для обнаружения exchange'ей (PublishMessage).
        // Слушателей там нет, публикатор ничего не потребляет.
        builder.AddMarsWolverine("e2e-reward-trigger", typeof(RewardRedeemedEvent).Assembly);

        var app = builder.Build();
        await app.StartAsync(cancellationToken);

        return new RewardEventPublisher(app);
    }

    public async Task PublishAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken cancellationToken
    )
    {
        var bus = _app.Services.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(rewardEvent);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
