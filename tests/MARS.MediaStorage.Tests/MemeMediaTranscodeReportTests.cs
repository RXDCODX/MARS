using MARS.MediaStorage.Services.Media;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Отчёт о перекодировании и его разбивка на сообщения Telegram.
/// </summary>
public class MemeMediaTranscodeReportTests
{
    [Fact]
    public void BuildFileReport_MentionsTheSourceProbeAndResult()
    {
        var probe = new MediaProbeResult(
            BitrateKbps: 96,
            AverageFrameRate: 29.97,
            RawFrameRate: 60,
            VideoCodecName: "h264",
            AudioCodecName: "aac"
        );

        var report = MemeMediaTranscodePolicy.BuildFileReport(
            "/wwwroot/Alerts/random_meme/videos/meme.avi",
            "/wwwroot/Alerts/random_meme/_converted/meme.mp4",
            MediaType.Video,
            probe
        );

        Assert.Contains("Файл: meme.avi", report);
        Assert.Contains("Исходник: /wwwroot/Alerts/random_meme/videos/meme.avi", report);
        Assert.Contains("Результат: /wwwroot/Alerts/random_meme/_converted/meme.mp4", report);
        Assert.Contains("96 kbps", report);
        Assert.Contains("video=h264, audio=aac", report);
        Assert.Contains("average=29.97, raw=60", report);
    }

    /// <summary>
    /// Файл без потока видео: строки про кадры быть не должно, но строка про
    /// MP3 в отчёте остаётся — так же, как в монолите.
    /// </summary>
    [Fact]
    public void BuildFileReport_OmitsFrameRates_ForAudioOnly()
    {
        var report = MemeMediaTranscodePolicy.BuildFileReport(
            "/wwwroot/meme.ogg",
            "/wwwroot/meme.ogg",
            MediaType.Audio,
            new MediaProbeResult(BitrateKbps: 64, AudioCodecName: "vorbis")
        );

        Assert.DoesNotContain("Кадры:", report);
        Assert.Contains("выставлен MP3", report);
    }

    [Fact]
    public void BuildBatchSummary_ListsEveryTranscodedFile()
    {
        var summary = MemeMediaTranscodePolicy.BuildBatchSummary(
            10,
            ["Файл: a.avi", "Файл: b.mov"]
        );

        Assert.Contains("Всего файлов: 10", summary);
        Assert.Contains("Требовали конвертацию: 2", summary);
        Assert.Contains("1. Файл: a.avi", summary);
        Assert.Contains("2. Файл: b.mov", summary);
    }

    [Fact]
    public void BuildBatchSummary_ReportsZero_WhenNothingWasTranscoded()
    {
        var summary = MemeMediaTranscodePolicy.BuildBatchSummary(10, []);

        Assert.Contains("Требовали конвертацию: 0", summary);
        Assert.DoesNotContain("1. ", summary);
    }

    [Fact]
    public void SplitForTelegram_KeepsShortMessageInOnePart()
    {
        var parts = MemeMediaTranscodePolicy.SplitForTelegram("Обработка файлов завершена");

        Assert.Single(parts);
        Assert.Equal("Обработка файлов завершена", parts[0]);
    }

    /// <summary>
    /// Граница Telegram — 4096 символов; в монолите взято 3900 с запасом. Каждая
    /// часть должна укладываться в лимит, иначе уведомление терялось.
    /// </summary>
    [Fact]
    public void SplitForTelegram_CutsByLinesAndKeepsEveryPartUnderTheLimit()
    {
        var line = new string('x', 100);
        var message = string.Join(Environment.NewLine, Enumerable.Repeat(line, 100));

        var parts = MemeMediaTranscodePolicy.SplitForTelegram(message);

        Assert.True(parts.Count > 1);
        Assert.All(parts, part => Assert.True(part.Length <= 3900));

        // Разрез по строкам съедает разделитель на границе частей, поэтому
        // сравнивается содержимое, а не текст целиком.
        Assert.Equal(line.Length * 100, string.Concat(parts).Count(character => character == 'x'));
    }

    /// <summary>
    /// Строка длиннее лимита режется принудительно: иначе она не поместилась бы
    /// ни в одну часть и потерялась.
    /// </summary>
    [Fact]
    public void SplitForTelegram_ForceCutsAnOversizedLine()
    {
        var message = new string('y', 5000);

        var parts = MemeMediaTranscodePolicy.SplitForTelegram(message);

        Assert.Equal(2, parts.Count);
        Assert.All(parts, part => Assert.True(part.Length <= 3900));
        Assert.Equal(message, string.Concat(parts));
    }

    [Fact]
    public void SplitForTelegram_ReturnsEmptyPart_ForEmptyMessage()
    {
        var parts = MemeMediaTranscodePolicy.SplitForTelegram("   ");

        Assert.Single(parts);
        Assert.Equal(string.Empty, parts[0]);
    }

    /// <summary>
    /// Контейнер после перекодирования: webm остаётся webm, остальное — mp4.
    /// </summary>
    [Fact]
    public void GetTargetFilePath_KeepsWebmAndMovesEverythingElseToMp4()
    {
        Assert.Equal(
            "/memes/a.webm",
            MemeMediaTranscodePolicy.GetTargetFilePath("/memes/a.webm", MediaType.Video)
        );
        Assert.Equal(
            "/memes/a.mp4",
            MemeMediaTranscodePolicy.GetTargetFilePath("/memes/a.avi", MediaType.Video)
        );
        Assert.Equal(
            "/memes/a.ogg",
            MemeMediaTranscodePolicy.GetTargetFilePath("/memes/a.ogg", MediaType.Audio)
        );
    }
}
