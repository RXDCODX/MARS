using System.Globalization;
using System.Text;
using MARS.MediaStorage.Services.Media;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Решения и отчёты по перекодированию мемов. Правила перенесены из
/// <c>TwitchMediaPreparationService</c> и <c>TwitchMediaTranscodeWorker</c>
/// монолита и вынесены в чистые функции: проверять их можно без ffmpeg и без БД.
/// </summary>
public static class MemeMediaTranscodePolicy
{
    private const int MinimumAudioBitrateKbps = 128;

    private const int MinimumVideoBitrateKbps = 128;

    private const string WebmExtension = ".webm";

    private const string Mp4Extension = ".mp4";

    /// <summary>
    /// Нужен ли перекод. Порог битрейта и признак переменной частоты кадров
    /// взяты из монолита; проверка кодеков — тоже оттуда: mp4 обязан быть
    /// h264 + mp3, webm — vp8.
    /// </summary>
    public static bool NeedsTranscoding(
        MediaType mediaType,
        MediaProbeResult probe,
        string? targetExtension
    )
    {
        if (mediaType != MediaType.Video)
        {
            return false;
        }

        var hasLowBitrate = probe.BitrateKbps < MinimumVideoBitrateKbps;
        var hasVariableFrameRate = IsVariableFrameRate(probe.AverageFrameRate, probe.RawFrameRate);
        var extension = string.IsNullOrWhiteSpace(targetExtension)
            ? Mp4Extension
            : targetExtension.TrimStart('.').ToLowerInvariant();

        if (extension == WebmExtension.TrimStart('.'))
        {
            return hasLowBitrate
                || hasVariableFrameRate
                || !string.Equals(probe.VideoCodecName, "vp8", StringComparison.OrdinalIgnoreCase);
        }

        var needsH264 = !string.Equals(
            probe.VideoCodecName,
            "h264",
            StringComparison.OrdinalIgnoreCase
        );
        var needsMp3Audio =
            probe.AudioCodecName is not null
            && !string.Equals(probe.AudioCodecName, "mp3", StringComparison.OrdinalIgnoreCase);

        return hasLowBitrate || hasVariableFrameRate || needsH264 || needsMp3Audio;
    }

    /// <summary>
    /// Расхождение average и rfr больше 0.01 fps — переменная частота кадров.
    /// Меньше — шум округления в ffprobe.
    /// </summary>
    public static bool IsVariableFrameRate(double? averageFrameRate, double? rawFrameRate)
    {
        return averageFrameRate is > 0
            && rawFrameRate is > 0
            && Math.Abs(averageFrameRate.Value - rawFrameRate.Value) > 0.01;
    }

    /// <summary>
    /// Куда класть перекодированный файл: webm остаётся webm, всё остальное
    /// переезжает в mp4.
    /// </summary>
    public static string GetTargetFilePath(string sourceFilePath, MediaType mediaType)
    {
        if (mediaType != MediaType.Video)
        {
            return sourceFilePath;
        }

        return string.Equals(
            Path.GetExtension(sourceFilePath),
            WebmExtension,
            StringComparison.OrdinalIgnoreCase
        )
            ? Path.ChangeExtension(sourceFilePath, WebmExtension)
            : Path.ChangeExtension(sourceFilePath, Mp4Extension);
    }

    /// <summary>
    /// Отчёт по одному файлу — как в монолите: что за файл, куда попал, какие
    /// кодеки были на входе.
    /// </summary>
    public static string BuildFileReport(
        string sourceFilePath,
        string targetFilePath,
        MediaType mediaType,
        MediaProbeResult probe
    )
    {
        var report = new StringBuilder();
        var targetExtension = Path.GetExtension(sourceFilePath);
        var detectedBitrateText = probe.BitrateKbps is > 0
            ? $"{probe.BitrateKbps} kbps"
            : "unknown";

        report.AppendLine($"Файл: {Path.GetFileName(sourceFilePath)}");
        report.AppendLine("Тип: видео");
        report.AppendLine($"Исходник: {sourceFilePath}");
        report.AppendLine($"Результат: {targetFilePath}");
        report.AppendLine(
            $"Что сделано: оригинал заменён на файл с расширением {targetExtension} после успешной обработки"
        );
        report.AppendLine($"Подробности: исходная битрейт-оценка {detectedBitrateText}");
        report.AppendLine(
            $"Кодеки источника: video={probe.VideoCodecName ?? "unknown"}, audio={probe.AudioCodecName ?? "unknown"}"
        );

        if (mediaType == MediaType.Video)
        {
            report.AppendLine(
                $"Кадры: average={FormatFrameRate(probe.AverageFrameRate)}, raw={FormatFrameRate(probe.RawFrameRate)}"
            );
        }
        else
        {
            report.AppendLine(
                $"Изменения: выставлен MP3, {MinimumAudioBitrateKbps} kbps, убраны видео-данные и метаданные"
            );
        }

        return report.ToString().TrimEnd();
    }

    /// <summary>
    /// Сводка по проходу воркера: сколько файлов просмотрено и что именно
    /// перекодировалось.
    /// </summary>
    public static string BuildBatchSummary(int totalCount, IReadOnlyList<string> fileReports)
    {
        var summary = new StringBuilder();

        summary.AppendLine("Обработка файлов завершена");
        summary.AppendLine($"Всего файлов: {totalCount}");
        summary.AppendLine($"Требовали конвертацию: {fileReports.Count}");
        summary.AppendLine("Полный список:");

        for (var index = 0; index < fileReports.Count; index++)
        {
            summary.AppendLine($"{index + 1}. {fileReports[index]}");
        }

        return summary.ToString().TrimEnd();
    }

    /// <summary>
    /// Режет отчёт на части по 3900 символов — предел одного сообщения
    /// Telegram. Разрез идёт по строкам; строка длиннее предела режется
    /// принудительно, иначе уведомление молча потерялось бы.
    /// </summary>
    public static IReadOnlyList<string> SplitForTelegram(string message)
    {
        const int maxLength = 3900;

        var parts = new List<string>();

        if (string.IsNullOrWhiteSpace(message))
        {
            parts.Add(string.Empty);

            return parts;
        }

        var lines = message.Split(Environment.NewLine);
        var chunk = new StringBuilder();

        foreach (var line in lines)
        {
            var candidate = chunk.Length == 0 ? line : Environment.NewLine + line;

            if (chunk.Length + candidate.Length <= maxLength)
            {
                chunk.Append(candidate);
                continue;
            }

            if (chunk.Length > 0)
            {
                parts.Add(chunk.ToString());
                chunk.Clear();
            }

            if (line.Length <= maxLength)
            {
                chunk.Append(line);
                continue;
            }

            for (var start = 0; start < line.Length; start += maxLength)
            {
                parts.Add(line.Substring(start, Math.Min(maxLength, line.Length - start)));
            }
        }

        if (chunk.Length > 0)
        {
            parts.Add(chunk.ToString());
        }

        return parts;
    }

    private static string FormatFrameRate(double? frameRate)
    {
        // InvariantCulture: отчёт уходит в Telegram и в лог, поэтому десятичная
        // запятая в тексте зависела бы от локали контейнера.
        return frameRate is > 0
            ? frameRate.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "unknown";
    }
}
