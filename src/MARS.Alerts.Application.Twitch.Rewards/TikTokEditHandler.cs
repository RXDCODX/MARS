using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardTikTokEdit</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class TikTokEditHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : RewardHandlerBase
{
    public string RewardKey => "tiktokedit";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var text = rewardEvent.UserInput ?? string.Empty;
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.TikTokEdit(Guid.NewGuid(), text)
            );
        }
        else
        {
            await notifier.TikTokEdit(Guid.NewGuid(), text);
        }
    }
}
