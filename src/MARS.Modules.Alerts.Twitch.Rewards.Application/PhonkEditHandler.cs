using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardPhonkEdit</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class PhonkEditHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : IRewardAlertHandler
{
    public string RewardKey => "phonkedit";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => notifier.PhonkEdit());
        }
        else
        {
            await notifier.PhonkEdit();
        }
    }
}
