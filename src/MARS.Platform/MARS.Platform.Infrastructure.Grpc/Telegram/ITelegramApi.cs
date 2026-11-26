namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public interface ITelegramApi
{
    ITelegramAdminApi Admin { get; }

    ITelegramLogsApi Logs { get; }
}