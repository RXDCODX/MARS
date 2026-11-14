using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Telegramus;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Показывает следующий звук из очереди Alerts/zvik при погашении Twitch-награды.
/// Проверка подписки и стоимости выполняется в MARS.TwitchCore до публикации события.
/// </summary>
public sealed class RandomSoundHandler(
    IMediaStorageClient mediaStorageClient,
    EventBroadcaster<TelegramusEvent> eventBroadcaster,
    RickRollerService rickRollerService,
    ILogger<RandomSoundHandler> logger
) : RewardHandlerBase
{
    private const int RewardCost = 10;
    private const string MemeFolderPath = "Alerts/zvik";

    public override string RewardKey => "randomsound";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        if (rewardEvent.Cost != RewardCost)
        {
            logger.LogWarning(
                "RandomSound ignored: expected cost {ExpectedCost}, received {ActualCost}",
                RewardCost,
                rewardEvent.Cost
            );
            return;
        }

        var user = rewardEvent.User;
        if (user is null)
        {
            logger.LogWarning(
                "RandomSound ignored: Twitch user data is missing for {UserName}",
                rewardEvent.UserName
            );
            return;
        }

        await rickRollerService.TryRickRollAsync(user, async () =>
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

            await eventBroadcaster.BroadcastAsync(
                new TelegramusEvent
                {
                    RandomMem = new AlertEvent
                    {
                        Media = MediaGrpcMapper.ToProto(new MediaDto(media)),
                    },
                }
            );
        });
    }
}
