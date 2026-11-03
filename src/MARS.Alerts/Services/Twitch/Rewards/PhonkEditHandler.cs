using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardPhonkEdit</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class PhonkEditHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardPhonkEdit;

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
