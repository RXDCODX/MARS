using MARS.Shared.Contracts.Messaging;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Infrastructure.MTProto.BotService;
using MARS.Telegram.Infrastructure.MTProto.Messaging;
using MARS.Telegram.Infrastructure.MTProto.Models;
using Microsoft.Extensions.Options;
using Wolverine;

namespace MARS.Telegram.Host.Services;

/// <summary>
/// Принимает входящие сообщения Telegram, детектит команды (префикс «/») и
/// публикует запрос на выполнение в MARS.Commands.
/// </summary>
public sealed class TelegramCommandForwarder(
    IOptions<TelegramConfiguration> options,
    IMessageBus bus,
    ILogger<TelegramCommandForwarder> logger
) : ITelegramIncomingHandler
{
    public async Task HandleAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        var text = message.Text.Trim();
        if (!text.StartsWith('/'))
        {
            return;
        }

        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var commandName = parts[0].TrimStart('/').Split('@')[0].Trim();
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return;
        }

        var input = parts.Length > 1 ? parts[1] : string.Empty;
        var isAdmin = options.Value.AdminIds.Contains(message.ChatId);

        try
        {
            await bus.PublishAsync(
                new CommandExecuteRequestedEvent
                {
                    Platform = CommandPlatform.Telegram,
                    CommandName = commandName,
                    Input = input,
                    IsAdmin = isAdmin,
                    UserId = message.ChatId.ToString(),
                    UserName = string.Empty,
                    DisplayName = string.Empty,
                    ReplyTo = message.ChatId.ToString(),
                    SourceMessageId = message.MessageId.ToString(),
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось опубликовать запрос команды '{Command}'", commandName);
        }
    }
}
