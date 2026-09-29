using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Shared.Configuration;
using MARS.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

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

    public async Task<T?> GetAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken = default
    )
    {
        T? result = default;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
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

                return default;
            }

            var envelope = await response.Content.ReadFromJsonAsync<OperationResult<T>>(
                JsonOptions,
                cancellationToken
            );

            if (envelope is not null && envelope.Success)
            {
                result = envelope.Result;
            }
            else
            {
                Logger.LogWarning(
                    "{Service} ответил без успеха на {Url}: {Error}",
                    ServiceEndpoint,
                    relativeUrl,
                    envelope?.ErrorMessage
                );
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
        }

        return result;
    }

    public async Task<T?> PostAsync<TRequest, T>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default
    )
    {
        T? result = default;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
            {
                Content = JsonContent.Create(body, options: JsonOptions),
            };

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

                return default;
            }

            var envelope = await response.Content.ReadFromJsonAsync<OperationResult<T>>(
                JsonOptions,
                cancellationToken
            );

            if (envelope is not null && envelope.Success)
            {
                result = envelope.Result;
            }
            else
            {
                Logger.LogWarning(
                    "{Service} ответил без успеха на {Url}: {Error}",
                    ServiceEndpoint,
                    relativeUrl,
                    envelope?.ErrorMessage
                );
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
        }

        return result;
    }

    protected void ApplyApiKey(HttpRequestMessage request)
    {
        var options = authOptions.Value;

        if (options.IsEnabled)
        {
            request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        }
    }
}
