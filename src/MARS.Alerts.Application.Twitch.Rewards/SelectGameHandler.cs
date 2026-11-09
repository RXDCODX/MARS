using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardSelectGame</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class SelectGameHandler(ILogger<SelectGameHandler> logger) : RewardHandlerBase
{
    public string RewardKey => "selectgame";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation(
            "SelectGame reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
