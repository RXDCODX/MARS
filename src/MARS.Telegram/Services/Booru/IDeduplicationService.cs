using MARS.Shared.Models;

namespace MARS.Telegram.Services.Booru;

/// <summary>
/// Проверка и отметка уже опубликованных изображений.
/// </summary>
public interface IDeduplicationService
{
    Task<OperationResult<bool>> IsAlreadyPostedAsync(
        string source,
        int imageId,
        ulong discordChannelId,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult> RecordPostAsync(
        string source,
        int imageId,
        ulong discordChannelId,
        CancellationToken cancellationToken = default
    );
}
