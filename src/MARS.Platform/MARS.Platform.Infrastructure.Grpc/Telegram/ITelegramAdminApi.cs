using MARS.Shared.Contracts.TelegramAdmin;

namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public interface ITelegramAdminApi
{
    Task<AdminMessageReply> SendToAdminsAsync(
        AdminMessageRequest request,
        CancellationToken cancellationToken = default
    );
}