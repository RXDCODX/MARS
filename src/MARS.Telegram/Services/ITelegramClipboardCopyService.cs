using MARS.Shared.Models;
using global::Telegram.Bot;
using global::Telegram.Bot.Types;

namespace MARS.Telegram.Services;

public interface ITelegramClipboardCopyService
{
    Task<OperationResult<string[]>> GetFileUrlsByRequestIdAsync(string requestId);
    Task<OperationResult> MarkRequestAsCompletedAsync(string requestId);
    Task HandMessage(ITelegramBotClient client, Update update);
}
