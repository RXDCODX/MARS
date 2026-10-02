using System.Reflection;
using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.CinemaQueue.Tests.Services;

/// <summary>
/// Фоновый цикл напоминаний очереди.
///
/// Цикл живёт до отмены хоста и повторяет проверку раз в час: без него напоминание
/// о недосмотренном пришло бы только один раз — при старте сервиса.
/// </summary>
public class CinemaQueueNotificationLoopTests
{
    /// <summary>
    /// Проверка очереди выполняется и цикл останавливается по отмене токена.
    /// Токен отменяет сама очередь: так не приходится ждать реальный час между
    /// итерациями.
    /// </summary>
    [Fact]
    public async Task LoopChecksQueueUntilCancelled()
    {
        var stopping = new CancellationTokenSource();
        var queue = new Mock<ICinemaQueueService>();
        queue
            .Setup(instance => instance.GetAllMediaItemsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => stopping.Cancel())
            .ReturnsAsync([]);
        var service = new CinemaQueueNotificationService(
            queue.Object,
            NullLogger<CinemaQueueNotificationService>.Instance
        );

        var execute = typeof(CinemaQueueNotificationService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)execute.Invoke(service, [stopping.Token])!;

        queue.Verify(
            instance => instance.GetAllMediaItemsAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );
    }
}
