using System.Net;
using System.Text;
using System.Text.Json;
using MARS.Platform.Infrastructure.Observability.Logging.Telegram;
using MARS.Shared.Contracts.Telegram;
using MARS.Shared.Kernel;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Отправитель журнала в Telegram работает по HTTP: контракт, путь и различимые
/// статусы проверяются подменённым обработчиком, а не сетью.
/// </summary>
/// <remarks>
/// Обработчик подменён намеренно: настоящий MARS.Telegram требует бота и чатов,
/// а проверяемое здесь — то, что отправитель кладёт в запрос и как читает ответ.
/// </remarks>
public sealed class HttpTelegramLogPublisherTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Главный случай: запись уходит со всеми полями, ключ доступа — в заголовке,
    /// а ответ разбирается в итог доставки. Потерянное поле означало бы сообщение,
    /// по которому нельзя понять, что сломалось.
    /// </summary>
    [Fact]
    public async Task Запись_Уходит_На_Эндпоинт_Со_Всеми_Полями()
    {
        var handler = new StubHandler(
            _ =>
                Envelope(
                    OperationResult<TelegramLogEntryResult>.Ok(
                        new TelegramLogEntryResult { Recipients = 2, Suppressed = 1 }
                    )
                )
        );
        using var publisher = CreatePublisher(handler);
        var entry = new TelegramLogEntry
        {
            Service = "MARS.Shared.Tests",
            Level = "Error",
            Category = "MARS.Shared.Tests.Logging",
            Message = "Что-то сломалось",
            Exception = "System.InvalidOperationException: причина",
            Timestamp = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero),
            SuppressedCount = 3,
        };

        var result = await publisher.PublishAsync(entry, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Recipients);
        Assert.Equal(1, result.Suppressed);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal("/api/internal/telegram/log-entries", request.RequestUri!.AbsolutePath);
        Assert.Equal("ключ-из-конфигурации", Assert.Single(request.Headers.GetValues("X-Api-Key")));

        var sent = JsonSerializer.Deserialize<TelegramLogEntryRequest>(
            handler.Body!,
            JsonOptions
        );
        Assert.NotNull(sent);
        Assert.Equal("MARS.Shared.Tests", sent!.Service);
        Assert.Equal("Error", sent.Level);
        Assert.Equal("MARS.Shared.Tests.Logging", sent.Category);
        Assert.Equal("Что-то сломалось", sent.Message);
        Assert.Equal("System.InvalidOperationException: причина", sent.Exception);
        Assert.Equal(3, sent.SuppressedCount);
        Assert.Equal(entry.Timestamp, sent.Timestamp);
    }

    /// <summary>
    /// <c>412</c> означает, что Telegram не настроен принимать журнал: повторять
    /// доставку бессмысленно, и отправитель обязан отключить её сам. Без
    /// отдельного исключения синк считал бы это обычным недоступением сервиса и
    /// ретраил бы до бесконечности.
    /// </summary>
    [Fact]
    public async Task Отказ_Настройки_Превращается_В_Отдельное_Исключение()
    {
        var handler = new StubHandler(
            _ =>
                Envelope(
                    OperationResult<TelegramLogEntryResult>.Fail(
                        "Telegram не настроен получать журнал",
                        HttpStatusCode.PreconditionFailed
                    ),
                    HttpStatusCode.PreconditionFailed
                )
        );
        using var publisher = CreatePublisher(handler);

        var error = await Assert.ThrowsAsync<TelegramLogNotConfiguredException>(() =>
            publisher.PublishAsync(Entry(), TestContext.Current.CancellationToken)
        );

        Assert.Contains("не настроен", error.Message);
    }

    /// <summary>
    /// <c>503</c> — временная недоступность: отправитель обязан бросить исключение,
    /// чтобы синк повторил попытку, а не потерял запись молча.
    /// </summary>
    [Fact]
    public async Task Недоступность_Сервиса_Повод_Для_Повтора()
    {
        var handler = new StubHandler(
            _ =>
                Envelope(
                    OperationResult<TelegramLogEntryResult>.Fail(
                        "не удалось доставить",
                        HttpStatusCode.ServiceUnavailable
                    ),
                    HttpStatusCode.ServiceUnavailable
                )
        );
        using var publisher = CreatePublisher(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            publisher.PublishAsync(Entry(), TestContext.Current.CancellationToken)
        );
    }

    private static HttpTelegramLogPublisher CreatePublisher(StubHandler handler) =>
        new(
            new Uri("http://telegram.test"),
            "ключ-из-конфигурации",
            new HttpClient(handler)
        );

    private static TelegramLogEntry Entry() =>
        new()
        {
            Service = "MARS.Shared.Tests",
            Level = "Error",
            Message = "Что-то сломалось",
        };

    private static HttpResponseMessage Envelope<T>(
        OperationResult<T> result,
        HttpStatusCode status = HttpStatusCode.OK
    ) =>
        new(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(result, JsonOptions),
                Encoding.UTF8,
                "application/json"
            ),
        };

    /// <summary>
    /// Подменённый обработчик: запоминает запрос и отвечает заранее заданным
    /// ответом, не выходя в сеть.
    /// </summary>
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Request = request;
            Body =
                request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

            return respond(request);
        }
    }
}
