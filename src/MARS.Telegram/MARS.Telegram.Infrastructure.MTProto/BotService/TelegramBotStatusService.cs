using MARS.Telegram.Application.Models;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Infrastructure.MTProto.BotService;

/// <summary>
/// Реализация <see cref="ITelegramBotStatusService"/> поверх <see cref="ITelegramBotSession"/>.
/// </summary>
public sealed class TelegramBotStatusService(
    ITelegramBotSession session,
    ILogger<TelegramBotStatusService> logger
) : ITelegramBotStatusService
{
    public TelegramBotStatus GetStatus()
    {
        var snapshot = session.Snapshot;

        return new TelegramBotStatus
        {
            IsConnected = snapshot?.IsConnected ?? false,
            BotUserId = snapshot?.BotUserId,
            BotUsername = snapshot?.BotUsername,
            ErrorMessage = snapshot?.ErrorMessage,
            CheckedAtUtc = DateTime.UtcNow,
        };
    }

    public async Task<TelegramBotStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await session.GetAsync(cancellationToken);

            var snapshot = session.Snapshot;

            return new TelegramBotStatus
            {
                IsConnected = snapshot?.IsConnected ?? true,
                BotUserId = snapshot?.BotUserId,
                BotUsername = snapshot?.BotUsername,
                ErrorMessage = snapshot?.ErrorMessage,
                CheckedAtUtc = DateTime.UtcNow,
            };
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Активная проверка подключения бота не удалась");

            return new TelegramBotStatus
            {
                IsConnected = false,
                ErrorMessage = e.Message,
                CheckedAtUtc = DateTime.UtcNow,
            };
        }
    }
}
