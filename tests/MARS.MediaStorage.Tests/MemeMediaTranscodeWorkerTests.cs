using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Media;
using MARS.Shared.Configuration;
using MARS.Shared.Telegram;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Фоновый проход по файлам мемов: раз в три часа проверяет, что всё
/// проигрывается, и отчитывается в Telegram о перекодированном.
/// </summary>
public class MemeMediaTranscodeWorkerTests : IDisposable
{
    private static readonly MediaProbeResult Playable = new(
        BitrateKbps: 500,
        AverageFrameRate: 30,
        RawFrameRate: 30,
        VideoCodecName: "h264",
        AudioCodecName: "mp3"
    );

    private static readonly MediaProbeResult NotPlayable = Playable with
    {
        VideoCodecName = "hevc",
    };

    private readonly string _webRoot = Directory.CreateTempSubdirectory("mars-memes-").FullName;

    public void Dispose()
    {
        Directory.Delete(_webRoot, true);
        GC.SuppressFinalize(this);
    }

    private MemeOrder Order(string fileName, bool notConvertable = false)
    {
        var path = Path.Combine(_webRoot, fileName);
        File.WriteAllBytes(path, [1, 2, 3]);

        return new MemeOrder
        {
            Id = Guid.CreateVersion7(),
            FilePath = path,
            Order = 1,
            IsFileNotConvertable = notConvertable,
        };
    }

    private MemeMediaPreparationService Preparation(
        Func<string, MediaProbeResult> probe,
        Func<string, string> playable
    )
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(webHost => webHost.WebRootPath).Returns(_webRoot);

        return new MemeMediaPreparationService(
            environment.Object,
            new StubInspector(probe),
            new StubTranscoder(playable),
            NullLogger<MemeMediaPreparationService>.Instance
        );
    }

    private (MemeMediaTranscodeWorker Worker, List<string> Sent) Build(
        IReadOnlyList<MemeOrder> orders,
        Func<string, MediaProbeResult> probe,
        Func<string, string> playable,
        long[]? admins = null
    )
    {
        var memes = new Mock<IRandomMemeService>();
        memes
            .Setup(x => x.GetAllMemeOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var sent = new List<string>();
        var messenger = new Mock<ITelegramAdminMessenger>();
        messenger
            .Setup(x =>
                x.SendAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .Callback<long, string, CancellationToken>((_, text, _) => sent.Add(text))
            .Returns(Task.CompletedTask);

        var worker = new MemeMediaTranscodeWorker(
            memes.Object,
            Preparation(probe, playable),
            messenger.Object,
            Options.Create(new TelegramConfig { AdminIds = admins ?? [123] }),
            NullLogger<MemeMediaTranscodeWorker>.Instance
        );

        return (worker, sent);
    }

    /// <summary>
    /// Файл, помеченный как неконвертируемый, — уже известный битый файл:
    /// повторная попытка лишь жгла бы CPU.
    /// </summary>
    [Fact]
    public async Task RunPassAsync_SkipsFilesMarkedAsNotConvertable()
    {
        var (worker, sent) = Build(
            [Order("a.mp4", notConvertable: true)],
            _ => NotPlayable,
            Converted
        );

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        Assert.Empty(sent);
    }

    /// <summary>
    /// Отчёт уходит только когда что-то реально перекодировали: пустой отчёт
    /// каждые три часа только шумит.
    /// </summary>
    [Fact]
    public async Task RunPassAsync_SendsNothing_WhenNothingWasTranscoded()
    {
        var (worker, sent) = Build([Order("a.mp4")], _ => Playable, Converted);

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        Assert.Empty(sent);
    }

    [Fact]
    public async Task RunPassAsync_ReportsTranscodedFilesToEveryAdmin()
    {
        var order = Order("a.mp4");
        var (worker, sent) = Build([order], _ => NotPlayable, Converted, [111, 222]);

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, sent.Count);
        Assert.All(sent, message => Assert.Contains("Обработка файлов завершена", message));
        Assert.All(
            sent,
            message => Assert.Contains($"Файл: {Path.GetFileName(order.FilePath)}", message)
        );
    }

    /// <summary>
    /// Сводка считает все просмотренные записи, а не только перекодированные:
    /// иначе «Всего файлов» совпадало бы с числом проблем.
    /// </summary>
    [Fact]
    public async Task RunPassAsync_CountsEveryInspectedFileInTheSummary()
    {
        var orders = new[] { Order("a.mp4"), Order("b.mp4") };
        var (worker, sent) = Build(
            orders,
            path => Path.GetFileName(path) == "a.mp4" ? NotPlayable : Playable,
            Converted
        );

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        var summary = Assert.Single(sent);
        Assert.Contains("Всего файлов: 2", summary);
        Assert.Contains("Требовали конвертацию: 1", summary);
    }

    /// <summary>
    /// Без адресатов проход всё равно идёт: перекодирование полезно и без
    /// отчёта, а падать из-за отсутствия Telegram нельзя.
    /// </summary>
    [Fact]
    public async Task RunPassAsync_KeepsWorking_WithoutAdmins()
    {
        var orders = new[] { Order("a.mp4") };
        var (worker, sent) = Build(orders, _ => NotPlayable, Converted, []);

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        Assert.Empty(sent);
        Assert.Single(orders);
    }

    /// <summary>
    /// Ошибка отправки не должна ронять проход: файлы уже перекодированы, а
    /// отчёт — это диагностика, а не результат работы.
    /// </summary>
    [Fact]
    public async Task RunPassAsync_SurvivesAFailingMessenger()
    {
        var memes = new Mock<IRandomMemeService>();
        memes
            .Setup(x => x.GetAllMemeOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Order("a.mp4")]);

        var messenger = new Mock<ITelegramAdminMessenger>();
        messenger
            .Setup(x =>
                x.SendAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Telegram недоступен"));

        var worker = new MemeMediaTranscodeWorker(
            memes.Object,
            Preparation(_ => NotPlayable, Converted),
            messenger.Object,
            Options.Create(new TelegramConfig { AdminIds = [123] }),
            NullLogger<MemeMediaTranscodeWorker>.Instance
        );

        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        messenger.Verify(
            x => x.SendAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /// <summary>
    /// Список файлов на диске — единственное, ради чего нужен настоящий
    /// файловый воркер: остальное подменяется.
    /// </summary>
    private static string Converted(string sourceFullPath) =>
        Path.Combine(Path.GetDirectoryName(sourceFullPath)!, "converted.mp4");

    private sealed class StubInspector(Func<string, MediaProbeResult> probe) : IMediaInspector
    {
        public Task<MediaProbeResult> ProbeAsync(
            string filePath,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(probe(filePath));
    }

    private sealed class StubTranscoder(Func<string, string> playable) : IMediaTranscoder
    {
        public Task<string> EnsurePlayableAsync(
            string sourceFullPath,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(playable(sourceFullPath));
    }
}
