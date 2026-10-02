using MARS.Discord.Services.Media;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MARS.Discord.Tests;

/// <summary>
/// Сжатие вложений до лимита Discord.
/// </summary>
/// <remarks>
/// Изображения проверяются по-настоящему (ImageSharp — управляемая библиотека),
/// видео и аудио — через подменённый <see cref="IFfmpegRunner"/>, который
/// «выполняет» команду, создавая выходной файл. Иначе тест зависел бы от ffmpeg
/// на машине.
/// </remarks>
public class MediaCompressorTests
{
    private const long OneMegabyte = 1024 * 1024;

    private sealed class FakeFfmpegRunner : IFfmpegRunner
    {
        public List<string> Invocations { get; } = [];

        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Что записать в выходной файл: полный путь → размер в байтах.</summary>
        public Func<string, long>? OutputSize { get; set; }

        /// <summary>Сколько фрагментов создать при нарезке.</summary>
        public int SegmentCount { get; set; }

        public bool FailAll { get; set; }

        public Task<bool> RunAsync(string arguments, CancellationToken cancellationToken = default)
        {
            Invocations.Add(arguments);

            if (FailAll)
            {
                return Task.FromResult(false);
            }

            foreach (var output in ResolveOutputs(arguments))
            {
                if (output.Contains("%03d"))
                {
                    var directory = Path.GetDirectoryName(output)!;
                    Directory.CreateDirectory(directory);

                    for (var i = 0; i < SegmentCount; i++)
                    {
                        var path = Path.Combine(directory, $"segment_{i:D3}.mp4");
                        File.WriteAllBytes(path, new byte[OutputSize?.Invoke(path) ?? 16]);
                    }
                }
                else
                {
                    File.WriteAllBytes(output, new byte[OutputSize?.Invoke(output) ?? 16]);
                }
            }

            return Task.FromResult(true);
        }

        public Task<TimeSpan> GetDurationAsync(
            string path,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Duration);

        private static IEnumerable<string> ResolveOutputs(string arguments)
        {
            var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var position = Array.IndexOf(parts, "-y");

            if (position >= 0 && position + 1 < parts.Length)
            {
                yield return parts[position + 1];
            }
        }
    }

    private static MediaCompressor Build(FakeFfmpegRunner runner) =>
        new(runner, NullLogger<MediaCompressor>.Instance);

    private static MemoryStream MakeImage(int width, int height, int quality = 90)
    {
        // Пиксели заполняются псевдослучайно: периодический узор JPEG сжимает
        // почти в ноль, и тест на сжатие проходил бы вхолостую.
        using var image = new Image<Rgba32>(width, height);
        var seed = 12345u;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                seed = seed * 1664525u + 1013904223u;
                image[x, y] = new Rgba32((byte)(seed >> 16), (byte)(seed >> 8), (byte)seed, 255);
            }
        }

        var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder { Quality = quality });

        // Позиция обязана быть в начало: сервис копирует поток с текущей
        // позиции, и после кодирования она в конце.
        stream.Position = 0;

        return stream;
    }

    /// <summary>
    /// Файл, который уже укладывается в лимит, отдаётся как есть: пережимать
    /// заново quality JPEG было бы потерей качества без выигрыша.
    /// </summary>
    [Fact]
    public async Task CompressImageAsync_PassesThroughASmallFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var compressor = Build(new FakeFfmpegRunner());

        using var source = MakeImage(100, 100);
        var result = await compressor.CompressImageAsync(source, "photo.jpg", OneMegabyte, ct);

        Assert.True(result.Success);
        Assert.Equal(source.Length, result.Result!.Length);
    }

    [Fact]
    public async Task CompressImageAsync_ShrinksAnOversizedFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var compressor = Build(new FakeFfmpegRunner());

        using var source = MakeImage(800, 800, 100);

        // Лимит считается от самого файла: у градиента JPEG очень мал, и
        // фиксированное число не гарантировало бы, что файл вообще больше
        // лимита — тогда проверка прошла бы на pass-through.
        var result = await compressor.CompressImageAsync(
            source,
            "photo.jpg",
            source.Length / 2,
            ct
        );

        Assert.True(result.Success);
        Assert.True(result.Result!.Length < source.Length);
    }

    /// <summary>
    /// Крупное изображение уменьшается до 1920 по большей стороне до
    /// перекодирования, а не после: иначе JPEG сначала кодируется в полном
    /// разрешении и качество теряется дважды.
    /// </summary>
    [Fact]
    public async Task CompressImageAsync_DownsizesBeforeEncoding()
    {
        var ct = TestContext.Current.CancellationToken;
        var compressor = Build(new FakeFfmpegRunner());

        using var source = MakeImage(4000, 2000, 100);
        var result = await compressor.CompressImageAsync(
            source,
            "photo.jpg",
            source.Length / 4,
            ct
        );

        Assert.True(result.Success);

        using var decoded = await SixLabors.ImageSharp.Image.LoadAsync(
            result.Result!,
            TestContext.Current.CancellationToken
        );
        Assert.True(decoded.Width <= 1920 && decoded.Height <= 1920);
    }

    [Fact]
    public async Task CompressImageAsync_ReportsANonImage()
    {
        var ct = TestContext.Current.CancellationToken;
        var compressor = Build(new FakeFfmpegRunner());

        // Не-картинка проверяется только когда файл больше лимита: до лимита
        // файл отдаётся как есть, и это поведение перенесено из монолита.
        using var source = new MemoryStream(new byte[4096]);
        var result = await compressor.CompressImageAsync(source, "text.jpg", 1024, ct);

        Assert.False(result.Success);
        Assert.Contains("изображением", result.ErrorMessage);
    }

    [Fact]
    public async Task CompressVideoAsync_ReturnsOneSegment_WhenTheFileFits()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner { OutputSize = _ => 2048 };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[4096]);
        var result = await compressor.CompressVideoAsync(source, "clip.mp4", OneMegabyte, ct);

        Assert.True(result.Success);
        var segment = Assert.Single(result.Result!);
        Assert.Equal("clip.mp4", segment.FileName);
        Assert.Equal(2048, segment.Stream.Length);
    }

    [Fact]
    public async Task CompressVideoAsync_SplitsWhenTheFileIsStillTooLarge()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner
        {
            OutputSize = path =>
                path.EndsWith(".mp4", StringComparison.Ordinal) && !path.Contains("segment_")
                    ? 4096
                    : 1024,
            SegmentCount = 3,
        };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[4096]);
        var result = await compressor.CompressVideoAsync(source, "clip.mp4", 2048, ct);

        Assert.True(result.Success);
        Assert.Equal(3, result.Result!.Count);
        Assert.Equal("segment_000.mp4", result.Result[0].FileName);
        Assert.Contains(runner.Invocations, invocation => invocation.Contains("-f segment"));
    }

    /// <summary>
    /// Фрагмент длиннее десяти секунд бесполезен: зритель не успевает ни
    /// посмотреть, ни понять, что произошло.
    /// </summary>
    [Fact]
    public async Task CompressVideoAsync_KeepsFragmentsAtLeastTenSecondsLong()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner
        {
            Duration = TimeSpan.FromSeconds(30),
            OutputSize = path => path.Contains("segment_") ? 512 : 8192,
            SegmentCount = 8,
        };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[1024]);
        await compressor.CompressVideoAsync(source, "clip.mp4", 1024, ct);

        var split = runner.Invocations.Single(invocation => invocation.Contains("-f segment"));
        Assert.Contains("-segment_time 10", split);
    }

    [Fact]
    public async Task CompressVideoAsync_Fails_WhenAnOversizedSegmentRemains()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner { OutputSize = _ => 8192, SegmentCount = 2 };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[1024]);
        var result = await compressor.CompressVideoAsync(source, "clip.mp4", 1024, ct);

        Assert.False(result.Success);
        Assert.Contains("рагмент", result.ErrorMessage);
    }

    [Fact]
    public async Task CompressVideoAsync_ReportsFfmpegFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner { FailAll = true };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[1024]);
        var result = await compressor.CompressVideoAsync(source, "clip.mp4", OneMegabyte, ct);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CompressVideoAsync_ReportsEmptySplit()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner
        {
            OutputSize = path => path.Contains("segment_") ? 0 : 8192,
            SegmentCount = 0,
        };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[1024]);
        var result = await compressor.CompressVideoAsync(source, "clip.mp4", 1024, ct);

        Assert.False(result.Success);
        Assert.Contains("рагмент", result.ErrorMessage);
    }

    [Fact]
    public async Task CompressAudioAsync_ReturnsTheEncodedFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner { OutputSize = _ => 1024 };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[2048]);
        var result = await compressor.CompressAudioAsync(source, "track.wav", OneMegabyte, ct);

        Assert.True(result.Success);
        Assert.Equal(1024, result.Result!.Length);
        Assert.Contains("libmp3lame", runner.Invocations[0]);
    }

    /// <summary>
    /// Аудио не режется: если после сжатия файл не влез, отправлять нечего, и
    /// вызывающая сторона должна узнать об этом, а не получить пустой поток.
    /// </summary>
    [Fact]
    public async Task CompressAudioAsync_Fails_WhenStillTooLarge()
    {
        var ct = TestContext.Current.CancellationToken;
        var runner = new FakeFfmpegRunner { OutputSize = _ => 4096 };
        var compressor = Build(runner);

        using var source = new MemoryStream(new byte[8192]);
        var result = await compressor.CompressAudioAsync(source, "track.wav", 1024, ct);

        Assert.False(result.Success);
        Assert.Contains("лимит", result.ErrorMessage);
    }
}
