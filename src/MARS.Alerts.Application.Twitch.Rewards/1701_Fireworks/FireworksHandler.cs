using MARS.Alerts.Application.Twitch.Rewards.Models;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardFireworks</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class FireworksHandler(ITelegramusNotifier notifier, RickRollerService rickRollerService)
    : RewardHandlerBase
{
    public override string RewardKey => "fireworks";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.MakeScreenParticles(TwitchScreenParticles.Fireworks)
            );
        }
        else
        {
            await notifier.MakeScreenParticles(TwitchScreenParticles.Fireworks);
        }
    }
}
