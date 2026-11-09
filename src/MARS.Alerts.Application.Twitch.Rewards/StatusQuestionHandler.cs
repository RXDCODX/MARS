using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardStatusQuestion</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class StatusQuestionHandler(ILogger<StatusQuestionHandler> logger) : RewardHandlerBase
{
    public string RewardKey => "statusquestion";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("StatusQuestion reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
