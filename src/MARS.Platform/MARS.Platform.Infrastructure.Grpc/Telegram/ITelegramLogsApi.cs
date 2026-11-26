using MARS.Shared.Contracts.TelegramLog;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public interface ITelegramLogsApi
{
    Task<LogEntryReply> PublishAsync(
        LogEntryRequest request,
        CancellationToken cancellationToken = default
    );
}
