using System.Net;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент MARS.Discord. Отправка сообщения у него параметрами запроса,
/// а не телом, поэтому здесь собственный вызов: <c>PostAsync</c> базового
/// класса умеет только JSON-тело.
/// </summary>
public class DiscordClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<DiscordClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IDiscordClient
{
    public override string ServiceEndpoint => endpoints.Value.Discord;

    public async Task<bool> SendMessageAsync(
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default
    )
    {
        var result = false;

        try
        {
            var requestUri =
                $"api/Discord/send?channelId={channelId}&message={Uri.EscapeDataString(message)}";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            ApplyApiKey(request);

            using var response = await HttpClient.SendAsync(request, cancellationToken);

            // Конверт разбирать не нужно: Discord-сервис сам пишет причину
            // отказа в лог, а вызывающему важно лишь «отправлено или нет».
            if (response.StatusCode is not HttpStatusCode.OK)
            {
                Logger.LogWarning(
                    "Discord вернул {StatusCode} на отправку в канал {ChannelId}",
                    (int)response.StatusCode,
                    channelId
                );
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                result = !body.Contains("\"success\":false", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Logger.LogWarning(
                exception,
                "Не удалось отправить сообщение в Discord-канал {ChannelId}",
                channelId
            );
        }

        return result;
    }
}
