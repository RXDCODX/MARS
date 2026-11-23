using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Twitch.Infrastructure.Http;

/// <summary>
/// Реализация <see cref="ITwitchInternalClient"/>: межсервисные вызовы
/// MARS.Twitch.Api (Internal-эндпоинты). Живёт рядом с серверной частью (MARS.Twitch).
/// </summary>
public class TwitchInternalClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TwitchInternalClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITwitchInternalClient
{
    public override string ServiceEndpoint => endpoints.Value.TwitchCore;

    public Task<bool> SendChatMessageAsync(
        TwitchChatSendRequest request,
        CancellationToken cancellationToken = default
    ) => PostAsync("api/internal/chat/send", request, cancellationToken);

    public Task<bool> GrantVipAsync(
        TwitchVipGrantRequest request,
        CancellationToken cancellationToken = default
    ) => PostAsync("api/internal/vip/grant", request, cancellationToken);

    public Task<bool> RefundRewardAsync(
        RewardRefundRequest request,
        CancellationToken cancellationToken = default
    ) => PostAsync("api/internal/rewards/refund", request, cancellationToken);

    public Task<bool> RecordMiniGameResultAsync(
        MiniGameResultRequest request,
        CancellationToken cancellationToken = default
    ) => PostAsync("api/internal/minigames/result", request, cancellationToken);
}
