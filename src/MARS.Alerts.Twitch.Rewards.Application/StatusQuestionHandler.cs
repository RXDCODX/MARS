using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardStatusQuestion</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class StatusQuestionHandler(ILogger<StatusQuestionHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "statusquestion";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("StatusQuestion reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
