using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Models;
using MARS.CinemaQueue.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MARS.CinemaQueue.Tests.Services;

/// <summary>
/// Напоминание о недосмотренных фильмах в очереди.
///
/// Напоминание уходит один раз в трое суток и попадает в журнал предупреждением.
/// Проверяется, что в напоминание попадают только фильмы, которые действительно
/// ждут просмотра, и что часовой цикл не превращает его в поток одинаковых
/// сообщений.
/// </summary>
public class CinemaQueueNotificationServiceTests
{
    private readonly List<string> _warnings = [];

    [Fact]
    public async Task NothingToRemindAbout()
    {
        var service = Create(Queue());

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Empty(Reminders());
    }

    /// <summary>
    /// Фильм, ожидающий просмотра меньше троих суток, не тревожит: иначе о нём
    /// напоминали бы сразу после добавления.
    /// </summary>
    [Fact]
    public async Task FreshNextItemIsNotReminded()
    {
        var service = Create(Queue(Item(daysOld: 1)));

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Empty(Reminders());
    }

    /// <summary>
    /// Старый фильм, помеченный следующим, попадает в напоминание вместе с
    /// названием: без названия напоминание бесполезно.
    /// </summary>
    [Fact]
    public async Task OldNextItemIsReminded()
    {
        var service = Create(Queue(Item(daysOld: 5, title: "Начало")));

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Contains("Напоминание", Reminders()[0]);
        Assert.Contains("Начало", Reminders()[0]);
    }

    /// <summary>
    /// Фильм без отметки «следующий» не ждёт просмотра по этой очереди.
    /// </summary>
    [Fact]
    public async Task OldItemWithoutNextFlagIsSkipped()
    {
        var service = Create(Queue(Item(daysOld: 5, isNext: false)));

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Empty(Reminders());
    }

    /// <summary>
    /// Уже просмотренный фильм не ждёт: напоминание о нём было бы ошибкой.
    /// </summary>
    [Fact]
    public async Task WatchedItemIsSkipped()
    {
        var service = Create(Queue(Item(daysOld: 5, status: MediaStatus.Completed)));

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Empty(Reminders());
    }

    /// <summary>
    /// Второй проход в то же окно молчит: иначе часовой цикл писал бы одно и то же
    /// напоминание двадцать четыре раза.
    /// </summary>
    [Fact]
    public async Task SecondCheckInSamePeriodIsSilent()
    {
        var service = Create(Queue(Item(daysOld: 5)));

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);
        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Single(Reminders());
    }

    /// <summary>
    /// Несколько старых фильмов дают одно напоминание со счётом, а не по сообщению
    /// на каждый: иначе журнал засорялся бы десятками строк.
    /// </summary>
    [Fact]
    public async Task SeveralOldItemsShareOneReminder()
    {
        var service = Create(
            Queue(Item(daysOld: 5, title: "Начало"), Item(daysOld: 6, title: "Бегущий"))
        );

        await service.CheckAndNotifyUnwatchedNextItemsAsync(Token);

        var reminder = Assert.Single(Reminders());
        Assert.Contains("2", reminder);
        Assert.Contains("Начало", reminder);
        Assert.Contains("Бегущий", reminder);
    }

    /// <summary>
    /// Ошибка чтения очереди не роняет проверку: она повторится через час.
    /// </summary>
    [Fact]
    public async Task QueueFailureIsSurvived()
    {
        var queue = new Mock<ICinemaQueueService>();
        queue
            .Setup(service => service.GetAllMediaItemsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("база недоступна"));

        await Create(queue).CheckAndNotifyUnwatchedNextItemsAsync(Token);

        Assert.Empty(Reminders());
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Все предупреждения, начинающиеся с текста напоминания.</summary>
    private IReadOnlyList<string> Reminders() =>
        [.. _warnings.Where(warning => warning.Contains("Напоминание"))];

    private CinemaQueueNotificationService Create(Mock<ICinemaQueueService> queue) =>
        new(queue.Object, new WarningCollector(_warnings));

    /// <summary>
    /// Журнал, который сохраняет предупреждения: через них сервис сообщает о
    /// найденных фильмах, и иначе напоминание было бы нечем проверить.
    /// </summary>
    private sealed class WarningCollector(List<string> warnings)
        : ILogger<CinemaQueueNotificationService>
    {
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
            if (logLevel == LogLevel.Warning)
            {
                warnings.Add(formatter(state, exception));
            }
        }
    }

    private static Mock<ICinemaQueueService> Queue(params CinemaMediaItemDto[] items)
    {
        var queue = new Mock<ICinemaQueueService>();
        queue
            .Setup(service => service.GetAllMediaItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        return queue;
    }

    private static CinemaMediaItemDto Item(
        int daysOld,
        string title = "Фильм",
        bool isNext = true,
        MediaStatus status = MediaStatus.Pending
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            MediaUrl = "https://example.test/movie.mp4",
            IsNext = isNext,
            Status = status,
            CreatedAt = DateTime.Now.AddDays(-daysOld),
        };
}
