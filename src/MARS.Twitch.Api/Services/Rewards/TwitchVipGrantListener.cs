using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Api.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;
using Wolverine;

namespace MARS.Twitch.Api.Services.Rewards;

/// <summary>
/// Потребитель <see cref="TwitchVipGrantEvent"/>: выдаёт VIP-статус
/// пользователю канала через единственное Helix-подключение.
/// </summary>
public sealed class TwitchVipGrantListener(
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<TwitchVipGrantListener> logger
) : MarsEventListener<TwitchVipGrantEvent>
{
    public override async Task Handle(TwitchVipGrantEvent message, CancellationToken ct)
    {
        if (message is null || string.IsNullOrWhiteSpace(message.UserId))
        {
            logger.LogWarning("VIP grant has no user id, skipped");
            return;
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("VIP grant: no access token available");
            return;
        }

        try
        {
            await api.Helix.Channels.AddChannelVIPAsync(
                TwitchConstants.ChannelId,
                message.UserId,
                accessToken
            );
            logger.LogInformation("VIP granted to user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }
}
