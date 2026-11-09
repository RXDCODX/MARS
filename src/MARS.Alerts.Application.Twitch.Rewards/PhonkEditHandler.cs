using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardPhonkEdit</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class PhonkEditHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : RewardHandlerBase
{
    public string RewardKey => "phonkedit";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
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
