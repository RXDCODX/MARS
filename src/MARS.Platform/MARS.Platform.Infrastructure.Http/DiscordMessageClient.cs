using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/DiscordMessageClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/DiscordMessageClient.cs

public class DiscordMessageClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<DiscordMessageClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IDiscordMessageClient
{
    private const string SendUrl = "api/internal/messages/send";

    public override string ServiceEndpoint => endpoints.Value.Discord;

    public async Task SendAsync(
        DiscordMessageSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync(SendUrl, request, cancellationToken);
    }
}
