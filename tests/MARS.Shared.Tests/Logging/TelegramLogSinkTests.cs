using MARS.Shared.Logging.Telegram;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Serilog.Parsing;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Синк доставки журнала в Telegram.
///
/// Проверяется ровно то, что видно снаружи: что уходит, что не уходит и что
/// происходит с потерями. Сеть не проверяется — отправитель подменяется
/// заглушкой, потому что настоящий ходит в Telegram по MTProto.
/// </summary>
public class TelegramLogSinkTests
{
    /// <summary>
    /// Главный случай: ошибка уходит с именем сервиса, уровнем, категорией,
    /// текстом и исключением. Потерянные поля означали бы сообщение, по
    /// которому нельзя понять, что сломалось.
    /// </summary>
    [Fact]
    public async Task ErrorIsDeliveredWithAllFields()
    {
        var publisher = new RecordingPublisher();
        using var sink = Sink(publisher);

        sink.Emit(
            LogEvent(
                LogEventLevel.Error,
                "Что-то сломалось",
                "MARS.TwitchCore.Services.Foo",
                new InvalidOperationException("причина")
            )
        );

        await WaitForAsync(() => publisher.Published.Count == 1);

        var entry = publisher.Published[0];
        Assert.Equal("MARS.TwitchCore", entry.Service);
        Assert.Equal("Error", entry.Level);
        Assert.Equal("MARS.TwitchCore.Services.Foo", entry.Category);
        Assert.Equal("Что-то сломалось", entry.Message);
        Assert.Contains("причина", entry.Exception);
        Assert.Equal(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero), entry.Timestamp);
    }

    /// <summary>
    /// Порог по умолчанию — <c>Error</c>: предупреждения в чат не идут, иначе
    /// он превратился бы в поток шума.
    /// </summary>
    [Fact]
    public async Task RecordsBelowThresholdAreNotDelivered()
    {
        var publisher = new RecordingPublisher();
        using var sink = Sink(publisher);

        sink.Emit(LogEvent(LogEventLevel.Warning, "предупреждение"));
        sink.Emit(LogEvent(LogEventLevel.Information, "информация"));

        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Published);
    }

    /// <summary>
    /// Порог из конфигурации меняет, что уходит: с <c>Warning</c> в чат идут
    /// предупреждения.
    /// </summary>
    [Fact]
    public async Task ConfiguredThresholdIsHonoured()
    {
        var publisher = new RecordingPublisher();
        using var sink = Sink(publisher, minimumLevel: "Warning");

        sink.Emit(LogEvent(LogEventLevel.Warning, "предупреждение"));

        await WaitForAsync(() => publisher.Published.Count == 1);

        Assert.Equal("Warning", publisher.Published[0].Level);
    }

    /// <summary>
    /// Недоставленная запись не теряется молча: отправитель повторяет попытку,
    /// а после исчерпания попыток сообщает об этом в stderr.
    /// </summary>
    [Fact]
    public async Task FailedDeliveryIsRetriedUpToMaxAttempts()
    {
        var publisher = new RecordingPublisher(
            new InvalidOperationException("Telegram недоступен")
        );
        using var sink = Sink(publisher, maxAttempts: 3);

        sink.Emit(LogEvent(LogEventLevel.Error, "ошибка"));

        await WaitForAsync(() => publisher.Attempts >= 3);

        Assert.Equal(3, publisher.Attempts);
        Assert.Empty(publisher.Published);
    }

    /// <summary>
    /// Telegram, который не настроен принимать журнал, не станет настроен сам:
    /// повторять доставку бессмысленно, поэтому синк отключается после первой
    /// такой ошибки.
    /// </summary>
    [Fact]
    public async Task NotConfiguredErrorStopsFurtherDelivery()
    {
        var publisher = new RecordingPublisher(
            new TelegramLogNotConfiguredException("не задан список чатов")
        );
        using var sink = Sink(publisher, maxAttempts: 3);

        sink.Emit(LogEvent(LogEventLevel.Error, "первая"));
        await WaitForAsync(() => publisher.Attempts >= 1);

        sink.Emit(LogEvent(LogEventLevel.Error, "вторая"));
        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Equal(1, publisher.Attempts);
    }

    /// <summary>
    /// Переполненная очередь означает потерю записей. Молчаливая потеря
    /// выглядела бы как «доставилось всё», поэтому потеря считается и едет
    /// вместе со следующей записью.
    /// </summary>
    [Fact]
    public async Task OverflowIsCountedAndReportedWithNextEntry()
    {
        var gate = new TaskCompletionSource();
        var publisher = new RecordingPublisher(gate: gate);
        using var sink = Sink(publisher, queueCapacity: 1);

        sink.Emit(LogEvent(LogEventLevel.Error, "первая"));
        await WaitForAsync(() => publisher.Attempts >= 1);

        sink.Emit(LogEvent(LogEventLevel.Error, "вторая"));
        sink.Emit(LogEvent(LogEventLevel.Error, "третья"));

        gate.SetResult();
        await WaitForAsync(() => publisher.Published.Count >= 2);

        // Первая запись ушла до потери, поэтому свою потерю она не знает.
        Assert.Equal("первая", publisher.Published[0].Message);
        Assert.Equal(0, publisher.Published[0].SuppressedCount);

        // Потерянная третья запись посчитана и едет со следующей доставленной.
        Assert.Equal("вторая", publisher.Published[1].Message);
        Assert.Equal(1, publisher.Published[1].SuppressedCount);

        // Третья запись отброшена локально и не доставлена вовсе.
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(2, publisher.Published.Count);
    }

    /// <summary>
    /// При остановке сервиса очередь должна быть дренирована: запись, принятая
    /// до остановки, уходит, а не теряется молча.
    /// </summary>
    [Fact]
    public void DisposeDrainsQueuedEntries()
    {
        var publisher = new RecordingPublisher();
        var sink = Sink(publisher);

        sink.Emit(LogEvent(LogEventLevel.Error, "ошибка"));
        sink.Dispose();

        Assert.Single(publisher.Published);
    }

    /// <summary>
    /// Владелец бота не отправляет журнал сам себе: его собственные ошибки
    /// доставки возвращались бы к нему же.
    /// </summary>
    [Fact]
    public void OwnerServiceDoesNotShipLogsToItself()
    {
        var sink = TelegramLogSink.TryCreate(
            Configuration(("ServiceEndpoints:Telegram", "http://telegram:8080")),
            "MARS.Telegram"
        );

        Assert.Null(sink);
    }

    [Fact]
    public void DisabledShippingCreatesNoSink()
    {
        var sink = TelegramLogSink.TryCreate(
            Configuration(
                ("ServiceEndpoints:Telegram", "http://telegram:8080"),
                ("Telegram:LogEnabled", "false")
            ),
            "MARS.TwitchCore"
        );

        Assert.Null(sink);
    }

    /// <summary>
    /// Ошибка настройки не должна ронять сервис: доставка журнала —
    /// дополнительный канал, а не условие работы.
    /// </summary>
    [Fact]
    public void UnresolvableAddressCreatesNoSinkAndDoesNotThrow()
    {
        var sink = TelegramLogSink.TryCreate(
            Configuration(("ServiceEndpoints:Telegram", "")),
            "MARS.TwitchCore"
        );

        Assert.Null(sink);
    }

    [Fact]
    public void SinkIsCreatedForRegularService()
    {
        using var sink = TelegramLogSink.TryCreate(
            Configuration(("ServiceEndpoints:Telegram", "http://telegram:8080")),
            "MARS.TwitchCore"
        );

        Assert.NotNull(sink);
    }

    private static TelegramLogSink Sink(
        RecordingPublisher publisher,
        string minimumLevel = "Error",
        int queueCapacity = 16,
        int maxAttempts = 3
    ) =>
        new(
            "MARS.TwitchCore",
            new TelegramLogOptions
            {
                MinimumLevel = minimumLevel,
                QueueCapacity = queueCapacity,
                MaxAttempts = maxAttempts,
            },
            publisher
        );

    private static LogEvent LogEvent(
        LogEventLevel level,
        string message,
        string? category = null,
        Exception? exception = null
    )
    {
        var properties = new List<LogEventProperty>();

        if (category is not null)
        {
            properties.Add(new LogEventProperty("SourceContext", new ScalarValue(category)));
        }

        return new LogEvent(
            new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero),
            level,
            exception,
            new MessageTemplateParser().Parse(message),
            properties
        );
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.Fail("Ожидаемое состояние не наступило за отведённое время.");
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value))
            )
            .Build();

    /// <summary>
    /// Отправитель-заглушка: записывает доставленные записи и умеет падать.
    /// </summary>
    private sealed class RecordingPublisher(
        Exception? failure = null,
        TaskCompletionSource? gate = null
    ) : ITelegramLogPublisher
    {
        private readonly List<TelegramLogEntry> _published = [];
        private readonly Lock _sync = new();

        public IReadOnlyList<TelegramLogEntry> Published
        {
            get
            {
                lock (_sync)
                {
                    return _published.ToList();
                }
            }
        }

        public int Attempts { get; private set; }

        public bool Disposed { get; private set; }

        public async Task<TelegramLogPublishResult> PublishAsync(
            TelegramLogEntry entry,
            CancellationToken cancellationToken
        )
        {
            Attempts++;

            if (gate is not null)
            {
                await gate.Task.WaitAsync(cancellationToken);
            }

            if (failure is not null)
            {
                throw failure;
            }

            lock (_sync)
            {
                _published.Add(entry);
            }

            return new TelegramLogPublishResult { Recipients = 1 };
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
