using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardRandomMem</c>: показывает следующий
/// мем из очереди Alerts/random_meme.
/// </summary>
public sealed class RandomMemRewardHandler(
    IMediaStorageClient mediaStorageClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ILogger<RandomMemRewardHandler> logger,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    private const string MemeFolderPath = "Alerts/random_meme";

    public override int RewardCost { get; } = 11;
    public override string AlertDisplayName { get; set; } = "😂 Random Mem";
    public override string AlertDescription { get; set; } = "🤣 Рандомный мем на экране";
    public override Color Color { get; set; } = Color.FromArgb(243, 255, 0);
    public override int Cost { get; init; } = 11;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

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
        if (rewardEvent.Cost != RewardCost)
        {
            logger.LogWarning(
                "RandomMem ignored: expected cost {ExpectedCost}, received {ActualCost}",
                RewardCost,
                rewardEvent.Cost
            );
            return;
        }

        var user = rewardEvent.User;
        if (user is null)
        {
            logger.LogWarning(
                "RandomMem ignored: Twitch user data is missing for {UserName}",
                rewardEvent.UserName
            );
            return;
        }

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                var media = await mediaStorageClient.GetNextRandomMemeAsync(
                    MemeFolderPath,
                    user.DisplayName,
                    ct
                );

                if (media is null)
                {
                    logger.LogWarning(
                        "RandomMem queue {FolderPath} is empty or MediaStorage is unavailable",
                        MemeFolderPath
                    );
                    return;
                }

                await hubContext.Clients.All.RandomMem(
                    new AlertEvent { Media = new MediaDto(media) }
                );
            }
        );
    }
}
