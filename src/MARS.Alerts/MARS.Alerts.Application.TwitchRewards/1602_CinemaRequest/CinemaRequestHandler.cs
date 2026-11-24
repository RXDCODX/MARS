using System.Drawing;
using MARS.Alerts.Application.TwitchRewards._1602_CinemaRequest;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._1602_CinemaRequest;

/// <summary>
/// Обработчик reward-события <c>RewardCinemaRequest</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CinemaRequestHandler(
    ILogger<CinemaRequestHandler> logger,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 1602;
    public override string AlertDisplayName { get; set; } = "🎬 CINEMA/ANIME REQUEST";
    public override string AlertDescription { get; set; } = "🎥 Ссылку на кинопоиск/шикимори";
    public override Color Color { get; set; } = Color.FromArgb(255, 130, 128);
    public override int Cost { get; init; } = 1602;

    public override IRewardActivationPolicy Activation => DisabledPolicy.Instance;

    public override bool IsUserInputRequired { get; } = true;

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
            logger.LogInformation(
                "CinemaRequest reward redeemed by {UserName} with input: {Input}",
                rewardEvent.UserName,
                rewardEvent.UserInput
            );
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
