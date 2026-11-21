using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Media;

/// <summary>
/// Единая таблица расширений файлов: тип медиа и MIME-тип.
/// </summary>
/// <remarks>
/// Раньше один и тот же вопрос решался одиннадцатью разными switch-ами и
/// словарями, которые уже разошлись на .mkv, .svg, .flac, .opus и .bmp.
/// Сервисы, которым нужна своя подвыборка, фильтруют результат этого метода,
/// а не заводят копию таблицы.
/// </remarks>
public static class MediaFileTypes
{
    private static readonly Dictionary<string, MediaType> Types = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [".tgs"] = MediaType.TelegramSticker,
        [".ogg"] = MediaType.Audio,
        [".oga"] = MediaType.Audio,
        [".mp3"] = MediaType.Audio,
        [".wav"] = MediaType.Audio,
        [".webm"] = MediaType.Video,
        [".mp4"] = MediaType.Video,
        [".jpg"] = MediaType.Image,
        [".jpeg"] = MediaType.Image,
        [".png"] = MediaType.Image,
        [".webp"] = MediaType.Image,
        [".gif"] = MediaType.Gif,
    };

    private static readonly Dictionary<string, string> ContentTypes = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".mkv"] = "video/x-matroska",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".wmv"] = "video/x-ms-wmv",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".flac"] = "audio/flac",
        [".m4a"] = "audio/mp4",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
        [".bmp"] = "image/bmp",
        [".avif"] = "image/avif",
        [".pdf"] = "application/pdf",
        [".json"] = "application/json",
        [".xml"] = "application/xml",
        [".csv"] = "text/csv",
        [".md"] = "text/markdown",
        [".markdown"] = "text/markdown",
        [".log"] = "text/plain",
        [".yaml"] = "text/yaml",
        [".yml"] = "text/yaml",
        [".txt"] = "text/plain",
    };

    public static MediaType GetMediaType(string? fileNameOrPath)
    {
        var extension = Path.GetExtension(fileNameOrPath ?? string.Empty);
        return Types.TryGetValue(extension, out var type) ? type : MediaType.None;
    }

    public static string GetContentType(string? fileNameOrPath)
    {
        var extension = Path.GetExtension(fileNameOrPath ?? string.Empty);
        return ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
