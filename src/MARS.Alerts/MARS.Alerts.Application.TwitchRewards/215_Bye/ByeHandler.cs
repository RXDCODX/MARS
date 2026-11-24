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
/// Обработчик reward-события <c>RewardBye</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class ByeHandler(
    ILogger<ByeHandler> logger,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 215;
    public override string AlertDisplayName { get; set; } = "👋 Пока";
    public override string AlertDescription { get; set; } = "";
    public override Color Color { get; set; } = Color.FromArgb(0, 33, 255);
    public override int Cost { get; init; } = 215;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, Activation)
        );
        return Task.CompletedTask;
    }

    protected override Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        try
        {
            logger.LogInformation("Bye reward redeemed by {UserName}", rewardEvent.UserName);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
