using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Models.Overlay;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using MARS.WaifuGacha.Models.Rest;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Application.TwitchRewards._10_RandomSound;

/// <summary>
/// Показывает следующий звук из очереди Alerts/zvik при погашении Twitch-награды.
/// </summary>
public sealed class RandomSoundHandler(
    IMediaStorageClient mediaStorageClient,
    ITwitchEventValidationService validator,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ILogger<RandomSoundHandler> logger,
    IHostApplicationLifetime lifetime,
    RickRollerService rickRollerService,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "random_sound";
    public override int RewardCost { get; } = 10;
    public override string AlertDisplayName { get; set; } = "🔊 Random sound";
    public override string AlertDescription { get; set; } = "😂 Нажимать ради смешного момента";
    public override Color Color { get; set; } = Color.FromArgb(122, 167, 255);
    public override int Cost { get; init; } = 10;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    private const string MemeFolderPath = "Alerts/zvik";

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(
                validator,
                OnRewardRedeemedAsync,
                Cost,
                IsRewardEnabled,
                requireFollower: true
            )
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
                var media = await mediaStorageClient.GetNextRandomMemeAsync(
                    MemeFolderPath,
                    user.DisplayName,
                    ct
                );

                if (media is null)
                {
                    logger.LogWarning(
                        "RandomSound queue {FolderPath} is empty or MediaStorage is unavailable",
                        MemeFolderPath
                    );
                    return;
                }

                await hubContext.Clients.All.RandomMem(
                    new AlertEvent
                    {
                        Media = MediaPayloadMapper.ToPayload(
                            new MediaDto(media) { MediaInfo = media }
                        ),
                    }
                );
            }
        );
    }
}