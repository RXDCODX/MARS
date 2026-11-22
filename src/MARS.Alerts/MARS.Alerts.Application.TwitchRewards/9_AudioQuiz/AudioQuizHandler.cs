using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

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
    private readonly IMessageBus _messageBus;

    private readonly Lock _stateLock = new();
    private AudioQuizTrackDto? _currentTrack;
    private DateTimeOffset _startedAt;
    private const int RoundSeconds = 30;

    public AudioQuizHandler(
        ILogger<AudioQuizHandler> logger,
        ISoundRequestControlClient soundRequestClient,
        RickRollerService rickRollerService,
        IHubContext<AlertsHub, IAlertsHub> hubContext,
        IMessageBus messageBus,
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
        _messageBus = messageBus;
    }

    public override string RewardCode => "audio_quiz";
    public override int RewardCost { get; } = 9;
    public override string AlertDisplayName { get; set; } = "🎵 Аудио викторина";
    public override string AlertDescription { get; set; } = "🔊 Нужно угадать трек по фрагменту.";
    public override Color Color { get; set; } = Color.FromArgb(247, 0, 255);
    public override int Cost { get; init; } = 9;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    private Task HandleChatAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            _lifetime,
            _rewardEventManager,
            onRedeem: ValidatedRedeem(_validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled),
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

                await _hubContext.Clients.All.AudioQuizStart(
                    new AudioQuizStartEvent
                    {
                        TrackUrl = track.TrackUrl,
                        ArtworkUrl = track.ArtworkUrl,
                        RoundSeconds = RoundSeconds,
                    }
                );

                await _messageBus.PublishAsync(
                    new ChatSendEvent
                    {
                        Message = $"{user.DisplayName} запустил(а) аудио викторину!",
                    }
                );

                _logger.LogInformation("AudioQuiz started by {UserName}", rewardEvent.UserName);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(RoundSeconds));
                    await _soundRequestClient.UnmuteAllAsync();
                    await _hubContext.Clients.All.AudioQuizStop();
                    await _messageBus.PublishAsync(
                        new ChatSendEvent { Message = "Аудиовикторина завершена!" }
                    );
                });
            }
        );
    }
}
