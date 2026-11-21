using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/TelegramMessageClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/TelegramMessageClient.cs

public class TelegramMessageClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TelegramMessageClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITelegramMessageClient
{
    private const string SendUrl = "api/internal/telegram/messages/send";
    private const string EditInlineUrl = "api/internal/telegram/messages/edit-inline";

    public override string ServiceEndpoint => endpoints.Value.Telegram;

    public async Task SendAsync(
        TelegramMessageSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync(SendUrl, request, cancellationToken);
    }

    public async Task EditInlineAsync(
        TelegramInlineEditRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync(EditInlineUrl, request, cancellationToken);
    }
}
