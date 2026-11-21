using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardRandomMem</c>: показывает следующий
/// мем из очереди Alerts/random_meme.
/// </summary>
public sealed class RandomMemRewardHandler(
    IMediaStorageClient mediaStorageClient,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster,
    ILogger<RandomMemRewardHandler> logger
) : RewardHandlerBase
{
    private const int RewardCost = 11;
    private const string MemeFolderPath = "Alerts/random_meme";

    public override string RewardCode => "random_mem";

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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

                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        RandomMem = new AlertEvent
                        {
                            Media = MediaPayloadMapper.ToPayload(new MediaDto(media)),
                        },
                    }
                );
            }
        );
    }
}
