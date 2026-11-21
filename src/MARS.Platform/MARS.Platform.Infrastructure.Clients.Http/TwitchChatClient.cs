using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

public class TwitchChatClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TwitchChatClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITwitchChatClient
{
    private const string SendUrl = "api/internal/chat/send";

    public override string ServiceEndpoint => endpoints.Value.TwitchCore;

    public async Task SendAsync(
        TwitchChatSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync(SendUrl, request, cancellationToken);
    }
}
