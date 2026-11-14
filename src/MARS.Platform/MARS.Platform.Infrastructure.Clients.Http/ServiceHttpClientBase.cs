using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Общая реализация межсервисного HTTP-клиента: API-key заголовок, разбор конверта
/// <see cref="OperationResult{T}"/> и безопасное «проглатывание» недоступности сервиса
/// (возвращается <c>null</c> вместо исключения — вызывающий решает, критична ли ошибка).
/// </summary>
public abstract class ServiceHttpClientBase(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    ILogger logger
) : IServiceHttpClient
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected ILogger Logger { get; } = logger;

    protected HttpClient HttpClient { get; } = httpClient;

    public abstract string ServiceEndpoint { get; }

    public Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);

        return SendAsync<T>(request, relativeUrl, cancellationToken);
    }

    public Task<T?> PostAsync<TRequest, T>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };

        return SendAsync<T>(request, relativeUrl, cancellationToken);
    }

    /// <summary>
    /// POST с уже готовым JSON-телом.
    /// </summary>
    /// <remarks>
    /// Нужен там, где тело — не объект этого контракта, а чужая схема: событие
    /// оверлея едет JSON'ом protobuf-сообщения, и <c>JsonContent.Create</c>
    /// завернул бы его в строку, а не отправил телом.
    /// </remarks>
    public Task<T?> PostRawJsonAsync<T>(
        string relativeUrl,
        string json,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        return SendAsync<T>(request, relativeUrl, cancellationToken);
    }

    /// <summary>
    /// POST, у которого важен только факт успеха, а не тело ответа.
    /// </summary>
    /// <returns><see langword="true"/>, если сервис ответил успешным конвертом.</returns>
    public Task<bool> PostAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl);

        return PostForSuccessAsync(request, relativeUrl, cancellationToken);
    }

    /// <summary>
    /// POST с телом, у которого важен только факт успеха.
    /// </summary>
    /// <returns><see langword="true"/>, если сервис ответил успешным конвертом.</returns>
    public Task<bool> PostAsync<TRequest>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };

        return PostForSuccessAsync(request, relativeUrl, cancellationToken);
    }

    protected void ApplyApiKey(HttpRequestMessage request)
    {
        var options = authOptions.Value;

        if (options.IsEnabled)
        {
            request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        }
    }

    private async Task<bool> PostForSuccessAsync(
        HttpRequestMessage request,
        string relativeUrl,
        CancellationToken cancellationToken
    )
    {
        var envelope = await SendEnvelopeAsync<OperationResult>(
            request,
            relativeUrl,
            cancellationToken
        );

        return envelope is { Success: true };
    }

    private async Task<T?> SendAsync<T>(
        HttpRequestMessage request,
        string relativeUrl,
        CancellationToken cancellationToken
    )
    {
        var envelope = await SendEnvelopeAsync<T>(request, relativeUrl, cancellationToken);

        return envelope is { Success: true } ? envelope.Result : default;
    }

    /// <summary>
    /// Отправляет запрос и читает конверт целиком.
    /// </summary>
    /// <remarks>
    /// Отдельными методами «нужен результат» и «нужен только факт успеха»
    /// пришлось разделиться: у ответа без полезной нагрузки <c>result</c> равен
    /// <c>null</c>, и проверка успеха по нему отвечала бы «не доставлено» на
    /// успешном запросе.
    /// </remarks>
    private async Task<OperationResult<T>?> SendEnvelopeAsync<T>(
        HttpRequestMessage request,
        string relativeUrl,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using (request)
            {
                ApplyApiKey(request);

                using var response = await HttpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning(
                        "{Service} вернул {StatusCode} на {Url}",
                        ServiceEndpoint,
                        (int)response.StatusCode,
                        relativeUrl
                    );

                    return null;
                }

                var envelope = await response.Content.ReadFromJsonAsync<OperationResult<T>>(
                    JsonOptions,
                    cancellationToken
                );

                if (envelope is { Success: true })
                {
                    return envelope;
                }

                Logger.LogWarning(
                    "{Service} ответил без успеха на {Url}: {Error}",
                    ServiceEndpoint,
                    relativeUrl,
                    envelope?.ErrorMessage
                );

                return null;
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
                "Запрос к {Service} по адресу {Url} не удался",
                ServiceEndpoint,
                relativeUrl
            );

            return null;
        }
    }
}
