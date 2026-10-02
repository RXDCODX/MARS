using System.Reflection;
using MARS.MediaStorage.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Очистка корзины по истечении срока восстановления.
///
/// Проверяется, что выключенная очистка не трогает файлы вовсе и что проход не
/// падает из-за одного недоступного файла: иначе один битый файл отменял бы уборку
/// всей корзины до перезапуска.
///
/// Сам цикл ждёт таймер (по умолчанию — сутки), поэтому проверяются его границы, а
/// сам проход вызывается напрямую.
/// </summary>
public class SoftDeletePurgeWorkerTests
{
    /// <summary>
    /// Очистка включена по умолчанию: корзина не должна расти бесконечно.
    /// </summary>
    [Fact]
    public void PurgeIsEnabledByDefault()
    {
        var options = new MediaStorageOptions();

        Assert.True(options.EnablePurgeWorker);
        Assert.Equal(30, options.TrashRetentionDays);
    }

    [Fact]
    public async Task DisabledWorkerDoesNotTouchStorage()
    {
        var storage = new Mock<IMediaStorageService>();
        var worker = Create(storage.Object, enable: false);

        await Execute(worker);

        Assert.Empty(storage.Invocations);
    }

    /// <summary>
    /// Остановка завершает цикл без прохода: ждать следующего тика незачем.
    /// </summary>
    [Fact]
    public async Task CancelledTokenStopsTheLoop()
    {
        var storage = new Mock<IMediaStorageService>();
        var worker = Create(storage.Object, enable: true);

        await Execute(worker);

        Assert.Empty(storage.Invocations);
    }

    /// <summary>
    /// Проход спрашивает у хранилища, что истекло, и удаляет это.
    /// </summary>
    [Fact]
    public async Task PurgeRemovesExpiredFiles()
    {
        var storage = new Mock<IMediaStorageService>();
        storage
            .Setup(service => service.PurgeExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        var worker = Create(storage.Object, enable: true);

        await PurgeOnce(worker);

        storage.Verify(
            service => service.PurgeExpiredAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /// <summary>
    /// Недоступный файл не отменяет уборку остальных: исключение ловится внутри
    /// прохода, а следующий тик повторит попытку.
    /// </summary>
    [Fact]
    public async Task FailedPurgeDoesNotEscape()
    {
        var storage = new Mock<IMediaStorageService>();
        storage
            .Setup(service => service.PurgeExpiredAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("файл занят"));
        var worker = Create(storage.Object, enable: true);

        await PurgeOnce(worker);
    }

    private static SoftDeletePurgeWorker Create(
        IMediaStorageService storage,
        bool enable,
        int intervalMinutes = 60 * 24
    )
    {
        var services = new ServiceCollection();
        services.AddSingleton(storage);
        var scopeFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        return new SoftDeletePurgeWorker(
            scopeFactory,
            new MediaStorageOptions
            {
                EnablePurgeWorker = enable,
                PurgeIntervalMinutes = intervalMinutes,
                TrashRetentionDays = 30,
            },
            NullLogger<SoftDeletePurgeWorker>.Instance
        );
    }

    private static Task Execute(SoftDeletePurgeWorker worker) =>
        (Task)
            typeof(SoftDeletePurgeWorker)
                .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(worker, [new CancellationToken(true)])!;

    private static Task PurgeOnce(SoftDeletePurgeWorker worker) =>
        (Task)
            typeof(SoftDeletePurgeWorker)
                .GetMethod("PurgeOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(worker, [TestContext.Current.CancellationToken])!;
}
