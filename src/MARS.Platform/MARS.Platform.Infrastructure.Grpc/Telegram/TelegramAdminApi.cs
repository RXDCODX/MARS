using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.TelegramAdmin;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

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
