using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardFumoFridayNight</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class FumoFridayNightHandler(
    ILogger<FumoFridayNightHandler> logger,
    RickRollerService rickRollerService
) : RewardHandlerBase
{
    public override string RewardCode => "fumo_friday_night";

    public override IRewardActivationPolicy Activation =>
        new CompositeRewardPolicy(
            new DayOfWeekPolicy(DayOfWeek.Friday),
            new CooldownPolicy(TimeSpan.FromMinutes(3))
        );

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
        else
        {
            // Медиа-алерт показывает TwitchMediaAlertsListener (Media крепится издателем);
            // per-user/refund/чат — фаза 4. Старый вызов notifier.FumoFriday здесь был
            // неверным: монолит звал Alert с seeded-медиа.
        }

        logger.LogInformation("FumoFridayNight activated by {UserName}", rewardEvent.UserName);
    }
}
