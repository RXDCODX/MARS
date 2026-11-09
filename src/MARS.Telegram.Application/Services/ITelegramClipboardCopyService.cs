using MARS.Shared.Contracts.Models;

namespace MARS.Telegram.Application.Services;

/// <summary>Порт получения файлов, подготовленных обработчиком входящих сообщений Telegram.</summary>
public interface ITelegramClipboardCopyService
{
    Task<OperationResult<string[]>> GetFileUrlsByRequestIdAsync(string requestId);
    Task<OperationResult> MarkRequestAsCompletedAsync(string requestId);
}
