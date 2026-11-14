using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// HTTP-клиент доставки сообщений администраторам в MARS.Telegram.
/// Эндпоинт закрыт политикой <c>ServiceApiKey</c>, ключ подставляет
/// <see cref="ServiceHttpClientBase"/>.
/// </summary>
public class TelegramAdminClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TelegramAdminClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITelegramAdminClient
{
    private const string AdminMessagesUrl = "api/internal/telegram/admin-messages";

    public override string ServiceEndpoint => endpoints.Value.Telegram;

    public Task<TelegramAdminMessageResult?> SendToAdminsAsync(
        string service,
        string message,
        CancellationToken cancellationToken = default
    )
    {
        return PostAsync<TelegramAdminMessageRequest, TelegramAdminMessageResult>(
            AdminMessagesUrl,
            new TelegramAdminMessageRequest { Service = service, Message = message },
            cancellationToken
        );
    }
}
