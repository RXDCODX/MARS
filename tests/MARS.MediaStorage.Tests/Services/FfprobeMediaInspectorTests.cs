using System.Reflection;
using MARS.MediaStorage.Services.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Разбор медиа через ffprobe.
///
/// Настоящий ffprobe в тестах недоступен, поэтому проверяется то, что отвечает за
/// результат до и после вызова: пустой файл не должен давать «характеристики», а
/// имя кодека приводится к единому виду — иначе в интерфейсе всплывали бы «H264»
/// и «h264» как разные кодеки.
/// </summary>
public class FfprobeMediaInspectorTests
{
    [Theory]
    [InlineData("H264", "h264")]
    [InlineData("  AAC  ", "aac")]
    [InlineData("Vp9", "vp9")]
    public void CodecNameIsNormalized(string codec, string expected)
    {
        Assert.Equal(expected, Normalize(codec));
    }

    /// <summary>
    /// Пустое имя кодека остаётся пустым, а не превращается в пробел: в карточке
    /// файла для него нет поля.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCodecNameBecomesNull(string? codec)
    {
        Assert.Null(Normalize(codec));
    }

    /// <summary>
    /// Отсутствующий файл возвращает пустой результат: проверка идёт перед запуском
    /// ffprobe, чтобы не тратить процесс на пустое.
    /// </summary>
    [Fact]
    public async Task MissingFileYieldsEmptyProbe()
    {
        var inspector = new FfprobeMediaInspector(NullLogger<FfprobeMediaInspector>.Instance);

        var probe = await inspector.ProbeAsync(
            Path.Combine(Path.GetTempPath(), $"нет-{Guid.NewGuid():N}.mp4"),
            TestContext.Current.CancellationToken
        );

        Assert.Null(probe.VideoCodecName);
        Assert.Null(probe.AudioCodecName);
        Assert.Null(probe.BitrateKbps);
    }

    [Fact]
    public async Task EmptyPathYieldsEmptyProbe()
    {
        var inspector = new FfprobeMediaInspector(NullLogger<FfprobeMediaInspector>.Instance);

        var probe = await inspector.ProbeAsync("  ", TestContext.Current.CancellationToken);

        Assert.Null(probe.VideoCodecName);
    }

    private static string? Normalize(string? codecName)
    {
        var method = typeof(FfprobeMediaInspector).GetMethod(
            "NormalizeCodecName",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (string?)method.Invoke(null, [codecName]);
    }
}
