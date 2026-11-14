using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Models.Overlay;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// HTTP-клиент вброса события оверлея в MARS.Alerts.
/// </summary>
/// <remarks>
/// Тело запроса — JSON protobuf-сообщения, поэтому оно уходит как есть, без
/// повторной сериализации: <see cref="ServiceHttpClientBase.PostRawJsonAsync{T}"/>
/// отправляет строку телом, а не значением.
/// </remarks>
public class OverlayEventsClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<OverlayEventsClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IOverlayEventsClient
{
    private const string EventsUrl = "api/internal/overlay/events";

    public override string ServiceEndpoint => endpoints.Value.Alerts;

    public async Task<bool> FireAsync(
        string eventJson,
        CancellationToken cancellationToken = default
    )
    {
        var result = await PostRawJsonAsync<OverlayFireResult>(
            EventsUrl,
            eventJson,
            cancellationToken
        );

        return result is not null;
    }
}
