using System.Drawing;
using System.Text.Json;
using Grpc.Core;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._4_FumoRoll;

/// <summary>
/// Обработчик reward-события <c>RewardFumoRoll</c>.
/// </summary>
public class FumoFridayRollHandler(
    ILogger<FumoFridayRollHandler> logger,
    IMarsGrpcClient grpcClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 4;
    public override string AlertDisplayName { get; set; } = "🧸 Fumo Roulette";
    public override string AlertDescription { get; set; } = "Крути рулетку Fumo по пятницам! ♪";
    public override Color Color { get; set; } = Color.FromArgb(255, 182, 193);
    public override int Cost { get; init; } = 4;

    public override IRewardActivationPolicy Activation => new DayOfWeekPolicy(DayOfWeek.Friday);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, Activation)
        );
        return Task.CompletedTask;
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is null)
        {
            return;
        }

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                MARS.Shared.Contracts.WaifuGacha.Rest.FumoRollResultDto? result = null;
                try
                {
                    result = await grpcClient.WaifuGacha.RollFumoFullAsync(user.TwitchId, ct);
                }
                catch (RpcException)
                {
                    result = null;
                }
                if (result?.Fumo is null)
                {
                    if (result is { IsCooldown: true, RemainingSeconds: > 0 })
                    {
                        var minutes = Math.Max(1, (result.RemainingSeconds + 59) / 60);
                        await grpcClient.Twitch.SendChatMessageAsync(
                            new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
                            {
                                Message =
                                    $"@{rewardEvent.UserName}, кулдаун на FumoRoll! Подожди ещё {minutes} мин.",
                            },
                            ct
                        );
                    }

                    logger.LogWarning(
                        "FumoRoll: no fumo returned for {UserName}",
                        rewardEvent.UserName
                    );
                    return;
                }

                await hubContext.Clients.All.FumoRoll(
                    new FumoRollEvent
                    {
                        Fumo = result.Fumo,
                        User = user,
                        CollectedCount = result.CollectedCount,
                        TotalCount = result.TotalCount,
                    }
                );
            }
        );

        logger.LogInformation("FumoRoll activated by {UserName}", rewardEvent.UserName);
    }
}
