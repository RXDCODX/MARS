using System.Text.Json;
using Google.Protobuf;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.Twitch.Rewards._9_AudioQuiz;

/// <summary>
/// Обработчик reward-события <c>RewardAudioQuiz</c>.
/// </summary>
public class AudioQuizHandler : RewardHandlerBase
{
    private readonly ILogger<AudioQuizHandler> _logger;
    private readonly ISoundRequestControlClient _soundRequestClient;
    private readonly RickRollerService _rickRollerService;
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster;
    private readonly IMessageBus _messageBus;

    private readonly Lock _stateLock = new();
    private AudioQuizTrackDto? _currentTrack;
    private DateTimeOffset _startedAt;
    private const int RoundSeconds = 30;

    public AudioQuizHandler(
        ILogger<AudioQuizHandler> logger,
        ISoundRequestControlClient soundRequestClient,
        RickRollerService rickRollerService,
        EventBroadcaster<TelegramusEvent> broadcaster,
        IMessageBus messageBus
    )
    {
        _logger = logger;
        _soundRequestClient = soundRequestClient;
        _rickRollerService = rickRollerService;
        _broadcaster = broadcaster;
        _messageBus = messageBus;

        OnTwitchMessage += HandleChatAsync;
    }

    public override string RewardCode => "audio_quiz";

    private Task HandleChatAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
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

                var roundDto = new
                {
                    TrackUrl = track.TrackUrl,
                    ArtworkUrl = track.ArtworkUrl,
                    RoundSeconds,
                };
                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        AudioQuizStart = new AudioQuizStartEvent
                        {
                            RoundJson = ByteString.CopyFromUtf8(JsonSerializer.Serialize(roundDto)),
                        },
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
                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent { AudioQuizStop = new EmptyEvent() }
                    );
                    await _messageBus.PublishAsync(
                        new ChatSendEvent { Message = "Аудиовикторина завершена!" }
                    );
                });
            }
        );
    }
}
