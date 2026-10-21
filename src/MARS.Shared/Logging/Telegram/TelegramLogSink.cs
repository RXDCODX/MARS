using System.Threading.Channels;
using MARS.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Синк Serilog, отправляющий записи журнала в Telegram.
/// </summary>
/// <remarks>
/// <para>
/// Сборщик живёт в каждом сервисе, а доставка — в <c>MARS.Telegram</c>: иначе
/// сервис должен был бы держать своего клиента Telegram, а это второй
/// владелец прав на одного бота.
/// </para>
/// <para>
/// Синк вызывается из потока Serilog, поэтому он ничего не ждёт: запись
/// кладётся в конечную очередь, а доставкой занимается фоновая задача.
/// Очередь конечна намеренно: неограниченная очередь при недоступном
/// Telegram съела бы память сервиса, а потеря записей честно считается и
/// показывается в чате как «(+N подавлено)».
/// </para>
/// <para>
/// Диагностика пишется в <see cref="Console"/>:Error, а не через ILogger.
/// Через ILogger она породила бы новую запись, а та — новое сообщение: ошибка
/// доставки превратилась бы в бесконечную петлю.
/// </para>
/// </remarks>
public sealed class TelegramLogSink : ILogEventSink, IDisposable
{
    /// <summary>
    /// Сервис, который принимает журнал. Он не отправляет журнал сам себе:
    /// иначе его собственные ошибки доставки возвращались бы к нему же.
    /// </summary>
    public const string OwnerServiceName = "MARS.Telegram";

    private readonly string _serviceName;
    private readonly LogEventLevel _minimumLevel;
    private readonly int _maxAttempts;
    private readonly Channel<TelegramLogEntry> _queue;
    private readonly ITelegramLogPublisher _publisher;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _worker;
    private int _suppressed;
    private int _disabled;
    private int _disposed;

    /// <summary>
    /// Создаёт синк.
    /// </summary>
    /// <param name="serviceName">Имя сервиса-отправителя.</param>
    /// <param name="options">Настройки доставки.</param>
    /// <param name="publisher">Отправитель записи в Telegram.</param>
    public TelegramLogSink(
        string serviceName,
        TelegramLogOptions options,
        ITelegramLogPublisher publisher
    )
    {
        _serviceName = serviceName;
        _minimumLevel = TelegramLogLevel.Parse(options.MinimumLevel);
        _maxAttempts = Math.Max(1, options.MaxAttempts);
        _publisher = publisher;
        _queue = Channel.CreateBounded<TelegramLogEntry>(
            new BoundedChannelOptions(Math.Max(1, options.QueueCapacity))
            {
                // Wait, а не DropWrite: при DropWrite TryWrite возвращает true
                // даже при отброшенной записи, и потеря осталась бы незамеченной.
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            }
        );
        _worker = Task.Run(RunAsync);
    }

    /// <summary>
    /// Создаёт синк, если доставка включена, а адрес получателя известен.
    /// </summary>
    /// <remarks>
    /// Любая ошибка здесь не должна ронять сервис: доставка журнала —
    /// дополнительный канал, а не условие работы. Поэтому исключение
    /// превращается в одну строку в stderr, и сервис живёт без синка.
    /// </remarks>
    /// <returns>Синк или null, если доставка выключена или недоступна.</returns>
    public static TelegramLogSink? TryCreate(IConfiguration configuration, string serviceName)
    {
        TelegramLogSink? result = null;

        try
        {
            var options = TelegramLogOptions.FromConfiguration(configuration);

            if (options.Enabled && !IsOwnerService(serviceName))
            {
                var address = GrpcClientExtensions.ResolveGrpcAddress(configuration, "Telegram");
                var publisher = new GrpcTelegramLogPublisher(address);

                result = new TelegramLogSink(serviceName, options, publisher);
            }
        }
        catch (Exception exception)
        {
            Report($"синк не создан: {exception.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        if (Volatile.Read(ref _disabled) != 0)
        {
            return;
        }

        if (logEvent.Level < _minimumLevel)
        {
            return;
        }

        var entry = CreateEntry(logEvent);

        if (entry is not null && !_queue.Writer.TryWrite(entry))
        {
            var suppressed = Interlocked.Increment(ref _suppressed);
            Report($"очередь переполнена, запись отброшена (отброшено всего {suppressed})");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _queue.Writer.TryComplete();

        try
        {
            _worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // Фоновая задача не должна ронять остановку сервиса.
        }

        try
        {
            _shutdown.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Уже освобождён.
        }

        try
        {
            _publisher.Dispose();
        }
        catch (Exception)
        {
            // Отправитель освобождается best effort.
        }
    }

    private static bool IsOwnerService(string serviceName) =>
        string.Equals(serviceName, OwnerServiceName, StringComparison.OrdinalIgnoreCase);

    private static void Report(string message) =>
        Console.Error.WriteLine($"[telegram-log] {message}");

    private TelegramLogEntry? CreateEntry(LogEvent logEvent)
    {
        TelegramLogEntry? result = null;

        var message = logEvent.RenderMessage();
        var exception = logEvent.Exception?.ToString() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(message) || !string.IsNullOrWhiteSpace(exception))
        {
            result = new TelegramLogEntry
            {
                Service = _serviceName,
                Level = logEvent.Level.ToString(),
                Category = ResolveCategory(logEvent),
                Message = message,
                Exception = exception,
                Timestamp = logEvent.Timestamp.ToUniversalTime(),
            };
        }

        return result;
    }

    /// <summary>
    /// Категория записи из свойства <c>SourceContext</c>.
    /// </summary>
    /// <remarks>
    /// В Serilog 4 это обычное свойство события, а не отдельное поле класса:
    /// читать его надо из словаря, иначе категория молча пропала бы.
    /// </remarks>
    private static string ResolveCategory(LogEvent logEvent)
    {
        var result = string.Empty;

        if (logEvent.Properties.TryGetValue("SourceContext", out var value) && value is not null)
        {
            result =
                value is ScalarValue scalar && scalar.Value is string text
                    ? text
                    : value.ToString() ?? string.Empty;
        }

        return result;
    }

    private async Task RunAsync()
    {
        try
        {
            await foreach (var entry in _queue.Reader.ReadAllAsync(_shutdown.Token))
            {
                await DeliverAsync(entry);
            }
        }
        catch (OperationCanceledException)
        {
            // Остановка при освобождении синка.
        }
    }

    private async Task DeliverAsync(TelegramLogEntry entry)
    {
        var attempt = 0;
        var delay = TimeSpan.FromSeconds(1);

        entry.SuppressedCount = Interlocked.Exchange(ref _suppressed, 0);

        while (true)
        {
            attempt++;

            try
            {
                var result = await _publisher.PublishAsync(entry, _shutdown.Token);

                if (result.Suppressed > 0)
                {
                    Report($"часть записей отброшена лимитом частоты ({result.Suppressed})");
                }

                return;
            }
            catch (TelegramLogNotConfiguredException exception)
            {
                Volatile.Write(ref _disabled, 1);
                Report($"доставка отключена: {exception.Message}");

                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                if (attempt >= _maxAttempts)
                {
                    Report($"запись не доставлена после {attempt} попыток: {exception.Message}");

                    return;
                }

                try
                {
                    await Task.Delay(delay, _shutdown.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }
    }
}
