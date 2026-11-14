using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Telegram;
using MARS.Shared.Kernel;

namespace MARS.Platform.Infrastructure.Observability.Logging.Telegram;

/// <summary>
/// Доставка записи журнала в Telegram по HTTP к <c>MARS.Telegram</c>.
/// </summary>
/// <remarks>
/// <para>
/// Клиент создаётся отправителем, а не берётся из контейнера зависимостей:
/// отправитель живёт внутри Serilog, настройка которого выполняется до сборки
/// контейнера, и поднимать контейнер ради одного клиента — значит создать
/// второй набор одиночных экземпляров и потерять их утилизацию.
/// </para>
/// <para>
/// Статусы различаются по смыслу, а не только по тексту: <c>412</c> означает,
/// что Telegram не настроен принимать журнал, и повторять доставку бессмысленно;
/// остальные неуспешные ответы — повод для повтора. Общий
/// <c>ServiceHttpClientBase</c> здесь не подходит: он гасит статус и возвращает
/// <c>null</c>, а отправителю нужно именно различие «навсегда» и «повторить».
/// </para>
/// </remarks>
public sealed class HttpTelegramLogPublisher : ITelegramLogPublisher
{
    private const string LogEntriesUrl = "api/internal/telegram/log-entries";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly string? _apiKey;

    /// <summary>
    /// Создаёт отправитель.
    /// </summary>
    /// <param name="address">Базовый HTTP-адрес MARS.Telegram.</param>
    /// <param name="apiKey">
    /// Ключ межсервисного доступа. Пустое значение оставляет заголовок не заданным:
    /// локальный запуск без ключа допустим, стенд ключ раздаёт compose.
    /// </param>
    /// <param name="httpClient">
    /// Готовый клиент. Нужен проверкам: без него отправитель поднимает собственный
    /// и освобождает его сам.
    /// </param>
    public HttpTelegramLogPublisher(
        Uri address,
        string? apiKey = null,
        HttpClient? httpClient = null
    )
    {
        _apiKey = apiKey;
        _ownsClient = httpClient is null;
        _httpClient =
            httpClient
            ?? new HttpClient(
                new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            );

        _httpClient.BaseAddress ??= address;
    }

    /// <inheritdoc />
    public async Task<TelegramLogPublishResult> PublishAsync(
        TelegramLogEntry entry,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, LogEntriesUrl)
        {
            Content = JsonContent.Create(ToRequest(entry), options: JsonOptions),
        };

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.TryAddWithoutValidation(
                ServiceAuthOptions.DefaultApiKeyHeaderName,
                _apiKey
            );
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new TelegramLogNotConfiguredException(
                await ReadErrorAsync(response, cancellationToken)
                    ?? "Telegram не настроен принимать журнал"
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MARS.Telegram ответил {(int)response.StatusCode} на {LogEntriesUrl}"
            );
        }

        var envelope = await ReadEnvelopeAsync(response, cancellationToken);

        if (envelope is not { Success: true })
        {
            throw new InvalidOperationException(
                envelope?.ErrorMessage ?? "MARS.Telegram ответил без результата"
            );
        }

        return new TelegramLogPublishResult
        {
            Recipients = envelope.Result?.Recipients ?? 0,
            Suppressed = envelope.Result?.Suppressed ?? 0,
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsClient)
        {
            _httpClient.Dispose();
        }
    }

    private static TelegramLogEntryRequest ToRequest(TelegramLogEntry entry) =>
        new()
        {
            Service = entry.Service,
            Level = entry.Level,
            Category = entry.Category,
            Message = entry.Message,
            Exception = entry.Exception,
            Timestamp = entry.Timestamp,
            SuppressedCount = entry.SuppressedCount,
        };

    private static async Task<OperationResult<TelegramLogEntryResult>?> ReadEnvelopeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<
                OperationResult<TelegramLogEntryResult>
            >(JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "MARS.Telegram ответил не конвертом результата",
                exception
            );
        }
    }

    private static async Task<string?> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var envelope = await ReadEnvelopeAsync(response, cancellationToken);

            return string.IsNullOrWhiteSpace(envelope?.ErrorMessage)
                ? null
                : envelope.ErrorMessage;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Тело ошибки — диагностика, а не условие: без него статус всё равно
            // понятен, и терять из-за него исключение нельзя.
            return null;
        }
    }
}
