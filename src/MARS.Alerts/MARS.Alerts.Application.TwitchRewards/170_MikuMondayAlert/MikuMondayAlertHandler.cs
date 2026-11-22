using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Models.Overlay;
using Microsoft.AspNetCore.SignalR;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Models.Rest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик награды Miku Monday: выбирает трек через MARS.WaifuGacha и
/// публикует событие в оверлей.
/// </summary>
public sealed class MikuMondayAlertHandler(
    ILogger<MikuMondayAlertHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "miku_monday_alert";
    public override int RewardCost { get; } = 170;
    public override string AlertDisplayName { get; set; } = "🎤 Miku Monday Alert";
    public override string AlertDescription { get; set; } = "🎶 Мику заглянула на стрим";
    public override Color Color { get; set; } = Color.FromArgb(57, 197, 187);
    public override int Cost { get; init; } = 170;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override IRewardActivationPolicy Activation =>
        new CompositeRewardPolicy(
            new DayOfWeekPolicy(DayOfWeek.Monday),
            new CooldownPolicy(TimeSpan.FromMinutes(20))
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

        await hubContext.Clients.All.MikuMonday(new MikuMondayEvent { MikuMondayJson = json });

        logger.LogInformation(
            "Miku Monday активирован для {UserName}, трек #{Number}",
            rewardEvent.UserName,
            result.Track.Number
        );
    }
}