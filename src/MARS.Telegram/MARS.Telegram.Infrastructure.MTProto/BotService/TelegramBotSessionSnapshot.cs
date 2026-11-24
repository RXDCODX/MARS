using MARS.Telegram.Application.Models;

namespace MARS.Telegram.Infrastructure.MTProto.BotService;

/// <summary>
/// Пассивный снимок состояния MTProto-бота: подключён ли, кто он.
/// </summary>
public sealed class TelegramBotSessionSnapshot
{
    public bool IsConnected { get; init; }

    public long? BotUserId { get; init; }

    public string? BotUsername { get; init; }

    public string? ErrorMessage { get; init; }
}
