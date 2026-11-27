using System.Drawing;
using System.Text.Json;
using Grpc.Core;
using MARS.Alerts.Application.TwitchRewards._170_MikuMondayAlert;
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
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._170_MikuMondayAlert;

/// <summary>
/// Обработчик награды Miku Monday: выбирает трек через MARS.WaifuGacha и
/// публикует событие в оверлей.
/// </summary>
public sealed class MikuMondayAlertHandler(
    ILogger<MikuMondayAlertHandler> logger,
    IMarsGrpcClient grpcClient,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 170;
    public override string AlertDisplayName { get; set; } = "🎤 Miku Monday Alert";
    public override string AlertDescription { get; set; } = "🎶 Мику заглянула на стрим";
    public override Color Color { get; set; } = Color.FromArgb(57, 197, 187);
    public override int Cost { get; init; } = 170;

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
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, Activation)
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
        MARS.Shared.Contracts.Clients.MikuMondayResult? result = null;
        try
        {
            result = await grpcClient.WaifuGacha.PickMikuMondayTrackAsync(
                rewardEvent.UserId,
                rewardEvent.UserName,
                isStreamer,
                ct
            );
        }
        catch (RpcException)
        {
            result = null;
        }

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

        await hubContext.Clients.All.MikuMonday(
            new MikuMondayEvent
            {
                Id = dto.Id,
                TwitchUser = dto.TwitchUser,
                SelectedTrack = new MARS.Shared.Contracts.Models.MikuTrackDto
                {
                    Id = dto.SelectedTrack.Id,
                    Number = dto.SelectedTrack.Number,
                    Artist = dto.SelectedTrack.Artist,
                    Title = dto.SelectedTrack.Title,
                    Url = dto.SelectedTrack.Url,
                    ThumbnailUrl = dto.SelectedTrack.ThumbnailUrl,
                },
                AvailableTracks = dto
                    .AvailableTracks.Select(t => new MARS.Shared.Contracts.Models.MikuTrackDto
                    {
                        Id = t.Id,
                        Number = t.Number,
                        Artist = t.Artist,
                        Title = t.Title,
                        Url = t.Url,
                        ThumbnailUrl = t.ThumbnailUrl,
                    })
                    .ToList(),
                SkipAvailableTracksUpdate = dto.SkipAvailableTracksUpdate,
            }
        );

        logger.LogInformation(
            "Miku Monday активирован для {UserName}, трек #{Number}",
            rewardEvent.UserName,
            result.Track.Number
        );
    }
}
