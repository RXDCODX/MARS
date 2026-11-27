using MARS.Shared.Contracts.TelegramMessage;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public interface ITelegramMessagesApi
{
    Task<bool> SendAsync(long chatId, string text, CancellationToken cancellationToken = default);

    Task<bool> SendMediaAsync(
        long chatId,
        string fileUrl,
        string fileName,
        string mimeType,
        string caption,
        long replyToMessageId,
        int mediaKind,
        double durationSeconds,
        int width,
        int height,
        CancellationToken cancellationToken = default
    );

    Task<string> GetGooglePhotosAuthorizationUrlAsync(
        CancellationToken cancellationToken = default
    );

    Task SetRandomMemEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetWhitelistAsync(CancellationToken cancellationToken = default);

    Task<string> GetWTelegramStatusAsync(CancellationToken cancellationToken = default);
}
