using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._9_AudioQuiz;

/// <summary>
/// Обработчик reward-события <c>RewardAudioQuiz</c>.
/// </summary>
public class AudioQuizHandler : RewardHandlerBase
{
    private readonly ITwitchEventValidationService _validator;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IRewardEventManager _rewardEventManager;

    private readonly ILogger<AudioQuizHandler> _logger;
    private readonly ISoundRequestControlClient _soundRequestClient;
    private readonly RickRollerService _rickRollerService;
    private readonly IHubContext<AlertsHub, IAlertsHub> _hubContext;
    private readonly ITwitchInternalClient _twitchClient;

    private readonly Lock _stateLock = new();
    private AudioQuizTrackDto? _currentTrack;
    private DateTimeOffset _startedAt;
    private const int RoundSeconds = 30;

    public AudioQuizHandler(
        ILogger<AudioQuizHandler> logger,
        ISoundRequestControlClient soundRequestClient,
        RickRollerService rickRollerService,
        IHubContext<AlertsHub, IAlertsHub> hubContext,
        ITwitchInternalClient twitchClient,
        ITwitchEventValidationService validator,
        IHostApplicationLifetime lifetime,
        IRewardEventManager rewardEventManager
    )
    {
        _validator = validator;
        _lifetime = lifetime;
        _rewardEventManager = rewardEventManager;

        _logger = logger;
        _soundRequestClient = soundRequestClient;
        _rickRollerService = rickRollerService;
        _hubContext = hubContext;
        _twitchClient = twitchClient;
    }

    public override int RewardCost { get; } = 9;
    public override string AlertDisplayName { get; set; } = "🎵 Аудио викторина";
    public override string AlertDescription { get; set; } = "🔊 Нужно угадать трек по фрагменту.";
    public override Color Color { get; set; } = Color.FromArgb(247, 0, 255);
    public override int Cost { get; init; } = 9;

    public override IRewardActivationPolicy Activation => DisabledPolicy.Instance;

    private Task HandleChatAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            _lifetime,
            _rewardEventManager,
            onRedeem: ValidatedRedeem(_validator, OnRewardRedeemedAsync, Cost, Activation),
            onChat: HandleChatAsync
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

        await _rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                var track = await _soundRequestClient.GetRandomTrackAsync(ct);
                if (track is null || string.IsNullOrWhiteSpace(track.TrackUrl))
                {
                    _logger.LogWarning("AudioQuiz: no track available");
                    return;
                }

                lock (_stateLock)
                {
                    _currentTrack = track;
                    _startedAt = DateTimeOffset.UtcNow;
                }

                await _soundRequestClient.MuteAllAsync(ct);

                await _twitchClient.SendChatMessageAsync(
                    new TwitchChatSendRequest
                    {
                        Message = $"{user.DisplayName} запустил(а) аудио викторину!",
                    }
                );

                _logger.LogInformation("AudioQuiz started by {UserName}", rewardEvent.UserName);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(RoundSeconds), ct);
                        await _soundRequestClient.UnmuteAllAsync(ct);
                        await _twitchClient.SendChatMessageAsync(
                            new TwitchChatSendRequest { Message = "Аудиовикторина завершена!" },
                            ct
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка завершения аудиовикторины");
                    }
                });
            }
        );
    }
}
