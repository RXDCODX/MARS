using System.Reflection;
using MARS.Discord.Services.Media;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Запуск ffmpeg.
///
/// Фильтр задаётся одной строкой и содержит пробелы в кавычках: наивное разбиение
/// по пробелу разорвало бы <c>-vf scale='min(1280,iw)':...</c> на три аргумента, и
/// нарезка не работала бы. Проверяется разбор аргументов и то, что отсутствие
/// ffmpeg в окружении считается отказом, а не падением.
/// </summary>
public class FfmpegRunnerTests
{
    [Fact]
    public void ArgumentsAreSplitBySpace()
    {
        var parts = Split("-i input.mp4 -c copy output.mp4");

        Assert.Equal(["-i", "input.mp4", "-c", "copy", "output.mp4"], parts);
    }

    /// <summary>
    /// Кучки в кавычках остаются целыми: фильтр с пробелом должен доехать до ffmpeg
    /// одним аргументом.
    /// </summary>
    [Fact]
    public void QuotedFilterStaysInOneArgument()
    {
        var parts = Split("-vf scale='min(1280,iw)':-2 -y out.mp4");

        Assert.Equal(["-vf", "scale=min(1280,iw):-2", "-y", "out.mp4"], parts);
    }

    [Theory]
    [InlineData("\"")]
    [InlineData("")]
    [InlineData("   ")]
    public void NothingToSplitYieldsNoArguments(string arguments)
    {
        Assert.Empty(Split(arguments));
    }

    /// <summary>
    /// Отменённый запуск не стартует процесс: нарезка после отмены не нужна.
    /// </summary>
    [Fact]
    public async Task CancelledRunIsRejected()
    {
        var runner = new FfmpegRunner();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runner.RunAsync("-version", cts.Token)
        );
    }

    /// <summary>
    /// Отсутствие ffmpeg в окружении — это отказ, а не исключение: сервис должен
    /// пережить стенд без ffmpeg.
    /// </summary>
    [Fact]
    public async Task MissingFfmpegIsReportedAsFailure()
    {
        var result = await new FfmpegRunner().RunAsync(
            "-definitely-not-a-real-flag",
            TestContext.Current.CancellationToken
        );

        Assert.False(result);
    }

    /// <summary>
    /// Несуществующий файл не роняет сервис: длительность просто неизвестна.
    /// </summary>
    [Fact]
    public async Task MissingFileHasNoDuration()
    {
        var duration = await new FfmpegRunner().GetDurationAsync(
            Path.Combine(Path.GetTempPath(), $"нет-{Guid.NewGuid():N}.mp3"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.Zero, duration);
    }

    private static IReadOnlyList<string> Split(string arguments)
    {
        var method = typeof(FfmpegRunner).GetMethod(
            "SplitArguments",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return [.. (IEnumerable<string>)method.Invoke(null, [arguments])!];
    }
}
