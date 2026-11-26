using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.TelegramLog;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

internal sealed class TelegramLogsApi(GrpcClientFactory clientFactory) : ITelegramLogsApi
{
    public Task<LogEntryReply> PublishAsync(
        LogEntryRequest request,
        CancellationToken cancellationToken = default
    ) =>
        clientFactory
            .CreateClient<TelegramLogService.TelegramLogServiceClient>("TelegramLog")
            .PublishAsync(request, cancellationToken: cancellationToken)
            .ResponseAsync;
}
