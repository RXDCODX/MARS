using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MARS.Videos365.Services;

/// <inheritdoc cref="ITelegramAdminMessenger"/>
public sealed class TelegramAdminMessenger(ITelegramBotClient botClient) : ITelegramAdminMessenger
{
    public Task SendAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        return botClient.SendMessage(
            chatId,
            text,
            ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
}