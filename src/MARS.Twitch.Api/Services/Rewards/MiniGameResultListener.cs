using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Api.Services.MiniGamesStats;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Twitch.Api.Services.Rewards;

/// <summary>
/// Потребитель <see cref="MiniGameResultEvent"/>: записывает результат
/// мини-игры в таблицу лидерборда MARS.Twitch.Api.
/// </summary>
public sealed class MiniGameResultListener(
    LeaderboardService leaderboardService,
    ILogger<MiniGameResultListener> logger
) : MarsEventListener<MiniGameResultEvent>
{
    public override async Task Handle(MiniGameResultEvent message, CancellationToken ct)
    {
        if (message is null || string.IsNullOrWhiteSpace(message.UserId))
        {
            logger.LogWarning("MiniGame result has no user id, skipped");
            return;
        }

        try
        {
            switch (message.Game)
            {
                case "russian_roulette":
                    await leaderboardService.RecordRouletteWinAsync(
                        message.UserId,
                        message.WithWaifu,
                        ct
                    );
                    break;
                case "quiz":
                    await leaderboardService.RecordTriviaWinAsync(
                        message.UserId,
                        message.WithWaifu,
                        ct
                    );
                    break;
                case "audio_quiz":
                    // No dedicated leaderboard method; log only.
                    logger.LogInformation("AudioQuiz win by {UserName}", message.UserName);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record mini-game result");
        }
    }
}
