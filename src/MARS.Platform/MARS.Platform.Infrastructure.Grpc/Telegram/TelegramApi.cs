using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Shared.Contracts.TelegramLog;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public sealed class TelegramApi(GrpcClientFactory clientFactory) : ITelegramApi
{
    public ITelegramAdminApi Admin { get; } = new TelegramAdminApi(clientFactory);

    public ITelegramLogsApi Logs { get; } = new TelegramLogsApi(clientFactory);
}

internal sealed class TelegramAdminApi(GrpcClientFactory clientFactory) : ITelegramAdminApi
{
    public Task<AdminMessageReply> SendToAdminsAsync(
        AdminMessageRequest request,
        CancellationToken cancellationToken = default
    ) =>
        clientFactory
            .CreateClient<TelegramAdminService.TelegramAdminServiceClient>("TelegramAdmin")
            .SendToAdminsAsync(request, cancellationToken: cancellationToken)
            .ResponseAsync;
}

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