using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик награды Miku Monday: выбирает трек через MARS.WaifuGacha и
/// публикует событие в оверлей.
/// </summary>
public sealed class MikuMondayAlertHandler(
    ILogger<MikuMondayAlertHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "miku_monday_alert";

    public override IRewardActivationPolicy Activation =>
        new CompositeRewardPolicy(
            new DayOfWeekPolicy(DayOfWeek.Monday),
            new CooldownPolicy(TimeSpan.FromMinutes(20))
        );

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        var isStreamer = false;
        var result = await waifuGachaClient.PickMikuMondayTrackAsync(
            rewardEvent.UserId,
            rewardEvent.UserName,
            isStreamer,
            ct
        );

        if (result is null)
        {
            logger.LogWarning(
                "Miku Monday: сервис WaifuGacha недоступен для {UserName}",
                rewardEvent.UserName
            );
            return;
        }

        if (result.Track is null)
        {
            logger.LogWarning(
                "Miku Monday: не получен трек для {UserName}: {Error}",
                rewardEvent.UserName,
                result.Error
            );
            return;
        }

        var twitchUser =
            rewardEvent.User
            ?? new TwitchUser
            {
                TwitchId = rewardEvent.UserId,
                UserLogin = rewardEvent.UserName,
                DisplayName = rewardEvent.UserName,
            };

        var dto = new MikuMondayDto
        {
            Id = Guid.NewGuid(),
            TwitchUser = twitchUser,
            SelectedTrack = result.Track,
            AvailableTracks = result.AvailableTracks,
            SkipAvailableTracksUpdate = isStreamer,
        };

        var json = JsonSerializer.Serialize(dto);
        var notification = new TelegramusEvent
        {
            MikuMonday = new MikuMondayEvent { MikuMondayJson = json },
        };

        await broadcaster.BroadcastAsync(notification);

        logger.LogInformation(
            "Miku Monday активирован для {UserName}, трек #{Number}",
            rewardEvent.UserName,
            result.Track.Number
        );
    }
}
