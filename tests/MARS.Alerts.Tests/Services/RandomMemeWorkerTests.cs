using System.Reflection;
using MARS.Alerts.Services.Twitch.Rewards;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Фоновое сканирование папки мемов.
///
/// Проверяется, что служебные файлы пересчитанных картинок не считаются мемами:
/// без этого кэш раздавался бы зрителям как случайные картинки.
/// </summary>
public class RandomMemeWorkerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mars-random-meme",
        Guid.NewGuid().ToString("N")
    );

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Theory]
    [InlineData("C:\\alerts\\_converted\\meme.jpg", true)]
    [InlineData("/alerts/_converted/meme.jpg", true)]
    [InlineData("C:\\Alerts\\_Converted\\nested\\meme.jpg", true)]
    [InlineData("C:\\alerts\\meme.jpg", false)]
    [InlineData("C:\\alerts\\converted\\meme.jpg", false)]
    public void CacheFolderIsRecognized(string filePath, bool isCache)
    {
        Assert.Equal(isCache, IsCacheFile(filePath));
    }

    /// <summary>
    /// Папка без файлов не роняет фоновое сканирование: сведения об отсутствии
    /// мемов — обычное состояние, а не ошибка. Обход выполняется до первого
    /// ожидания, поэтому тест дожидается именно записи в журнал.
    /// </summary>
    [Fact]
    public async Task ScanOfEmptyFolderIsNotAnError()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Alerts"));
        var scanned = new TaskCompletionSource();
        var worker = new RandomMemeWorker(Fake(_root), ScanLogger(scanned));

        using var cts = new CancellationTokenSource();
        var running = Execute(worker, cts.Token);

        await scanned.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken
        );
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
    }

    /// <summary>
    /// Выключенный воркер не сканирует вовсе: выключатель
    /// <c>IsServiceActive</c> останавливает только перебор файлов.
    /// </summary>
    [Fact]
    public async Task InactiveWorkerSkipsScan()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Alerts"));
        var scanned = new TaskCompletionSource();
        var worker = new RandomMemeWorker(Fake(_root), ScanLogger(scanned))
        {
            IsServiceActive = false,
        };

        await Execute(worker, new CancellationToken(true));

        Assert.False(scanned.Task.IsCompleted);
    }

    /// <summary>
    /// Журнал отмечает момент, когда обход папки закончен. Ни одной записи быть
    /// не должно — выключенный воркер обязан вернуться сразу.
    /// </summary>
    private static ScanSignalLogger ScanLogger(TaskCompletionSource scanned) => new(scanned);

    private static FakeEnvironment Fake(string webRoot) => new(webRoot) { WebRootPath = webRoot };

    /// <summary>
    /// Тело цикла завершается отменой на ожидании таймера, поэтому достаточно
    /// предотменённого токена: сам обход папки при этом выполняется по-настоящему.
    /// </summary>
    private static Task Execute(RandomMemeWorker worker, CancellationToken token)
    {
        var method = typeof(RandomMemeWorker).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(worker, [token])!;
    }

    private static bool IsCacheFile(string filePath)
    {
        var method = typeof(RandomMemeWorker).GetMethod(
            "IsCacheFile",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (bool)method.Invoke(null, [filePath])!;
    }

    private sealed class FakeEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = contentRoot;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MARS.Alerts.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Test";
    }

    /// <summary>
    /// Журнал, который сигнализирует о первой записи. Нужен, чтобы дождаться
    /// обхода папки: он выполняется до первого ожидания, а сам цикл живёт
    /// получаса.
    /// </summary>
    private sealed class ScanSignalLogger(TaskCompletionSource scanned) : ILogger<RandomMemeWorker>
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
        ) => scanned.TrySetResult();
    }
}
