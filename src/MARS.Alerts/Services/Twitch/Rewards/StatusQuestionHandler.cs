using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardStatusQuestion</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class StatusQuestionHandler(ILogger<StatusQuestionHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardStatusQuestion;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("StatusQuestion reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
