using MARS.Shared.Contracts.Models;
using MARS.Telegram.Infrastructure.Integrations.Telegram.BotService;

namespace MARS.Telegram.Infrastructure.Integrations.Telegram.Services;

public interface ITelegramClipboardCopyService : ITelegramIncomingHandler
{
    Task<OperationResult<string[]>> GetFileUrlsByRequestIdAsync(string requestId);
    Task<OperationResult> MarkRequestAsCompletedAsync(string requestId);
}
