using System.Reflection;
using MARS.Telegram.Data;
using MARS.Telegram.Services.BotService.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Фоновый опрос обновлений Telegram.
///
/// Сервис крутит цикл получения апдейтов и обязан переживать сбой одного захода:
/// иначе после разового отказа Telegram бот молча переставал бы отвечать до
/// перезапуска контейнера.
/// </summary>
public class PollingServiceBaseTests
{
    private readonly ChatTestDbContextFactory _factory = new();

    /// <summary>
    /// Цикл останавливается по отмене токена: пока он жив, апдейты забираются, а
    /// после отмены фоновая задача обязана завершиться, иначе сервис не
    /// останавливался бы при выключении.
    /// </summary>
    [Fact]
    public async Task PollingKeepsReceivingUntilStop()
    {
        await WarmUpDatabaseAsync();

        var receiver = new BlockingReceiver();
        var errors = new ErrorCollector();
        var polling = Create(receiver, errors, out _);

        await polling.StartAsync(TestContext.Current.CancellationToken);
        // Старт возвращается до первого прохода цикла, поэтому отмена раньше
        /// времени означала бы, что проверено ничего.
        await WaitForCallsAsync(receiver, 1);
        await polling.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(
            receiver.Calls == 1,
            $"вызовов {receiver.Calls}, ошибки цикла: {string.Join(" | ", errors.Messages)}"
        );
    }

    /// <summary>
    /// Сбой захода логируется и не выходит наружу фоновой задачей: сбой Telegram
    /// не должен ни ронять сервис, ни прекращать опрос.
    /// </summary>
    /// <remarks>
    /// Токен отменяет сам получатель апдейтов: иначе тест ждал бы реальные пять
    /// секунд паузы между попытками, а проверялась бы не обработка сбоя, а таймер.
    /// </remarks>
    [Fact]
    public async Task FailureOfIterationIsLogged()
    {
        var stopping = new CancellationTokenSource();
        var receiver = new BlockingReceiver { FailFirstCall = true, CancelsOnCall = stopping };
        var errors = new ErrorCollector();
        var polling = Create(receiver, errors, out _);

        var execute = typeof(PollingServiceBase<IReceiverService>).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            (Task)execute.Invoke(polling, [stopping.Token])!
        );

        Assert.Equal(1, receiver.Calls);
        Assert.Equal(1, errors.Errors);
    }

    /// <summary>
    /// Контейнер PostgreSQL поднимается лениво, и первый контекст платит за его
    /// старт несколько секунд. Без прогрева цикл не успел бы дойти до получателя
    /// внутри окна ожидания, и проверка сочла бы его молчащим.
    /// </summary>
    private async Task WarmUpDatabaseAsync()
    {
        await using var _ = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
    }

    private static async Task WaitForCallsAsync(BlockingReceiver receiver, int expected)
    {
        for (var attempt = 0; attempt < 100 && receiver.Calls < expected; attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private TestPollingService Create(
        IReceiverService receiver,
        ILogger<PollingServiceBase<IReceiverService>> logger,
        out IServiceProvider services
    )
    {
        var collection = new ServiceCollection();
        collection.AddScoped(_ => receiver);
        services = collection.BuildServiceProvider();

        return new TestPollingService(services, logger, _factory);
    }

    private sealed class TestPollingService(
        IServiceProvider serviceProvider,
        ILogger<PollingServiceBase<IReceiverService>> logger,
        IDbContextFactory<ChatDbContext> dbContextFactory
    ) : PollingServiceBase<IReceiverService>(serviceProvider, logger, dbContextFactory);

    /// <summary>
    /// Получатель ждёт отмены, поэтому цикл крутится ровно один раз за проход.
    /// </summary>
    private sealed class BlockingReceiver : IReceiverService
    {
        public int Calls { get; private set; }

        public bool FailFirstCall { get; init; }

        public CancellationTokenSource? CancelsOnCall { get; init; }

        public async Task ReceiveAsync(ChatDbContext chatDbContext, CancellationToken stoppingToken)
        {
            Calls++;

            if (FailFirstCall)
            {
                CancelsOnCall?.Cancel();
                throw new InvalidOperationException("Telegram недоступен");
            }

            // Возврат по отмене, а не исключение: иначе отмена попала бы в catch
            // цикла и выглядела бы как сбой Telegram.
            await Task.WhenAny(
                new TaskCompletionSource().Task,
                Task.Delay(Timeout.Infinite, stoppingToken)
            );
        }
    }

    /// <summary>
    /// Считает записанные ошибки: <c>Mock&lt;ILogger&lt;T&gt;&gt;</c> с
    /// <c>It.IsAnyType</c> в колбэке не собирается, поэтому логгер пишется руками.
    /// </summary>
    private sealed class ErrorCollector : ILogger<PollingServiceBase<IReceiverService>>
    {
        public int Errors { get; private set; }

        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (logLevel == LogLevel.Error)
            {
                Errors++;
                Messages.Add(formatter(state, exception));
            }
        }
    }
}
