using System.Net;
using System.Text.Encodings.Web;
using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace MARS.Twitch.Infrastructure.Services.Management;

public class TelegramRewardNotifier(
    IMarsGrpcClient grpcClient,
    TwitchAuthRedirectUriProvider redirectUri,
    ILogger<TelegramRewardNotifier> logger
) : ITelegramRewardNotifier
{
    public async Task NotifyRewardSyncIssueAsync(
        string message,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var safeMessage = HtmlEncoder.Default.Encode(message);
            var reply = await grpcClient.Telegram.Admin.SendToAdminsAsync(
                new AdminMessageRequest
                {
                    Service = "MARS.TwitchCore",
                    Message = $"<b>Синхронизация наград Twitch</b>\n\n{safeMessage}",
                },
                cancellationToken
            );

            if (reply.Recipients <= 0)
            {
                logger.LogWarning("Уведомление о синхронизации наград не доставлено");
            }
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "MARS.Telegram не доставил уведомление о синхронизации наград");
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }
}
