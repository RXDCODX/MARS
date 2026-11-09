using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCredits</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CreditsHandler(
    ITelegramusNotifier notifier,
    ILogger<CreditsHandler> logger,
    RickRollerService rickRollerService
) : RewardHandlerBase
{
    public string RewardKey => "credits";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => notifier.Credits());
        }
        else
        {
            await notifier.Credits();
        }

        logger.LogInformation("Credits activated by {UserName}", rewardEvent.UserName);
    }
}
