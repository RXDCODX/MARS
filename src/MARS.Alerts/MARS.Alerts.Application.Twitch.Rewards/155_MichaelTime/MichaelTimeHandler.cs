using System.Linq.Expressions;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._155_MichaelTime;

/// <summary>
/// Обработчик reward-события <c>RewardMichaelTime</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class MichaelTimeHandler(
    ILogger<MichaelTimeHandler> logger,
    RickRollerService rickRollerService
) : RewardHandlerBase
{
    public override string RewardCode => "michael_time";

    public override IRewardActivationPolicy Activation => new ServiceActivePolicy(() => true);

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => Task.CompletedTask);
        }
        else { }

        logger.LogInformation("MichaelTime activated by {UserName}", rewardEvent.UserName);
    }
}
