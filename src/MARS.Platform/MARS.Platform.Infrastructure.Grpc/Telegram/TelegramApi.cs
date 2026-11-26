using Grpc.Net.ClientFactory;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public sealed class TelegramApi(GrpcClientFactory clientFactory) : ITelegramApi
{
    public ITelegramAdminApi Admin { get; } = new TelegramAdminApi(clientFactory);

    public ITelegramLogsApi Logs { get; } = new TelegramLogsApi(clientFactory);

    public ITelegramMessagesApi Messages { get; } = new TelegramMessagesApi(clientFactory);
}
