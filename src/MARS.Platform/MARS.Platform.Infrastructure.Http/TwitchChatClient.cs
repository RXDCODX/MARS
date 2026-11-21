using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/TwitchChatClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/TwitchChatClient.cs

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
