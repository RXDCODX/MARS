using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardFumoFridayNight</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class FumoFridayNightHandler(
    ILogger<FumoFridayNightHandler> logger,
    RickRollerService rickRollerService,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "fumo_friday_night";
    public override int RewardCost { get; } = 170;
    public override string AlertDisplayName { get; set; } = "🧸 Fumo Friday Night";
    public override string AlertDescription { get; set; } =
        "🎪 Твоя уникальная возможность активации Fumo Friday Night.";
    public override Color Color { get; set; } = Color.FromArgb(128, 128, 128);
    public override int Cost { get; init; } = 170;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override IRewardActivationPolicy Activation =>
        new CompositeRewardPolicy(
            new DayOfWeekPolicy(DayOfWeek.Friday),
            new CooldownPolicy(TimeSpan.FromMinutes(3))
        );

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled)
        );
        return Task.CompletedTask;
    }

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
