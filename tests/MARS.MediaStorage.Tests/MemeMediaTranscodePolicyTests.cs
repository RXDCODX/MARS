using MARS.MediaStorage.Services.Media;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Решение о перекодировании мемов — правила перенесены из
/// <c>TwitchMediaPreparationService</c> монолита.
/// </summary>
public class MemeMediaTranscodePolicyTests
{
    [Fact]
    public void NeedsTranscoding_ReturnsFalse_ForAudio()
    {
        var probe = new MediaProbeResult(BitrateKbps: 64, VideoCodecName: "h264");

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Audio, probe, ".mp4"));
    }

    [Fact]
    public void NeedsTranscoding_ReturnsFalse_ForImages()
    {
        var probe = new MediaProbeResult(BitrateKbps: 10, VideoCodecName: "vp8");

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Image, probe, ".mp4"));
    }

    /// <summary>
    /// Монолит перекодировал видео с битрейтом ниже 128 кбит/с: такие файлы
    /// в OBS рвались.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsTrue_ForLowBitrate()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 96,
            VideoCodecName: "h264",
            AudioCodecName: "mp3"
        );

        Assert.True(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    /// <summary>
    /// Граница 128 кбит/с включительно — уже не перекодируем: монолит
    /// сравнивал строго «меньше».
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsFalse_AtTheBitrateBoundary()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 128,
            VideoCodecName: "h264",
            AudioCodecName: "mp3"
        );

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    /// <summary>
    /// Переменная частота кадров (average ≠ rfr) ломала звук при наложении на
    /// OBS — монолит считал это поводом для перекодирования.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsTrue_ForVariableFrameRate()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 500,
            AverageFrameRate: 29.97,
            RawFrameRate: 60,
            VideoCodecName: "h264",
            AudioCodecName: "mp3"
        );

        Assert.True(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    /// <summary>
    /// Разница меньше 0.01 fps — это шум округления, а не переменная частота.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsFalse_ForNegligibleFrameRateDifference()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 500,
            AverageFrameRate: 30.005,
            RawFrameRate: 30,
            VideoCodecName: "h264",
            AudioCodecName: "mp3"
        );

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    /// <summary>
    /// Контейнер mp4 должен нести h264: браузер и OBS не играют в H.265/VP9
    /// внутри mp4.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsTrue_WhenMp4CarriesANonH264Codec()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 500,
            AverageFrameRate: 30,
            RawFrameRate: 30,
            VideoCodecName: "hevc",
            AudioCodecName: "mp3"
        );

        Assert.True(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    [Fact]
    public void NeedsTranscoding_ReturnsFalse_ForAPlayableMp4()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 500,
            AverageFrameRate: 30,
            RawFrameRate: 30,
            VideoCodecName: "H264",
            AudioCodecName: "MP3"
        );

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    /// <summary>
    /// В webm монолит требовал vp8 и любой аудиокодек: aac в webm OBS играет,
    /// но mp3 внутри webm не поддерживается, поэтому проверки аудио там нет.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsFalse_ForPlayableWebm()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 500,
            AverageFrameRate: 30,
            RawFrameRate: 30,
            VideoCodecName: "vp8",
            AudioCodecName: "vorbis"
        );

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".webm"));
    }

    [Fact]
    public void NeedsTranscoding_ReturnsTrue_WhenWebmCarriesH264()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 500,
            AverageFrameRate: 30,
            RawFrameRate: 30,
            VideoCodecName: "h264",
            AudioCodecName: "vorbis"
        );

        Assert.True(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".webm"));
    }

    /// <summary>
    /// Если ffprobe не назвал кодек — перекодирование не знает, чего добиваться,
    /// и монолит всё равно пытался: неудачная попытка оставляет исходник на
    /// месте, а <c>IsFileNotConvertable</c> помечает файл как битый. Пробовать
    /// безопаснее, чем молча пропустить файл и не узнать о проблеме.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsTrue_WhenCodecsAreUnknown()
    {
        var probe = new MediaProbeResult(BitrateKbps: 500, AverageFrameRate: 30, RawFrameRate: 30);

        Assert.True(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Video, probe, ".mp4"));
    }

    /// <summary>
    /// А вот аудио и картинки перекодировать незачем ни при каких значениях:
    /// монолит ограничивал проверку видео.
    /// </summary>
    [Fact]
    public void NeedsTranscoding_ReturnsFalse_ForAudioEvenWithUnknownCodecs()
    {
        var probe = new MediaProbeResult(BitrateKbps: 500);

        Assert.False(MemeMediaTranscodePolicy.NeedsTranscoding(MediaType.Audio, probe, ".mp4"));
    }
}
