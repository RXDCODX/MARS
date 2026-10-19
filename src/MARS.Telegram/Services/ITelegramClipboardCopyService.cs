using MARS.Shared.Models;
using MARS.Telegram.Services.BotService;

namespace MARS.Telegram.Services;

public interface ITelegramClipboardCopyService : ITelegramIncomingHandler
{
    Task<OperationResult<string[]>> GetFileUrlsByRequestIdAsync(string requestId);
    Task<OperationResult> MarkRequestAsCompletedAsync(string requestId);
}
