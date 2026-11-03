using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardTyazhelo</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class TyazheloHandler(ILogger<TyazheloHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardTyazhelo;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Tyazhelo reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
