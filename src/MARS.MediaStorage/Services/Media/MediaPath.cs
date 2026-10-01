namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Единая работа с путями хранилища медиа.
/// </summary>
/// <remarks>
/// Аудит: в проекте одновременно использовались «/» и «\» (HasData сидил
/// «Alerts\random_meme», а валидатор загрузки — «Alerts/uploaded_mems/»), плюс
/// четыре разных способа отрезолвить корень. На Linux обратный слеш становится
/// частью имени файла, поэтому такие пути молча не находились.
/// </remarks>
public static class MediaPath
{
    private const char Separator = '/';

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".wmv"] = "video/x-ms-wmv",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".flac"] = "audio/flac",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
        [".bmp"] = "image/bmp",
        [".json"] = "application/json",
        [".txt"] = "text/plain",
    };

    /// <summary>
    /// Приводит путь к каноническому виду: прямые слеши, без ведущего и
    /// дублирующих разделителей. Сегменты «..» сохраняются — их отсекает
    /// <see cref="IsSafeRelative"/>.
    /// </summary>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var value = path.Trim().Replace('\\', Separator);
        var segments = value.Split(
            Separator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        return string.Join(Separator, segments);
    }

    /// <summary>
    /// Соединяет сегменты пути и нормализует результат.
    /// </summary>
    public static string Combine(params string?[] segments)
    {
        if (segments is null || segments.Length == 0)
        {
            return string.Empty;
        }

        return Normalize(string.Join(Separator, segments));
    }

    /// <summary>
    /// Имя файла без директорий. В отличие от <c>Path.GetFileName</c> работает
    /// с любым разделителем и не схлопывает подкаталоги.
    /// </summary>
    public static string GetFileName(string? path)
    {
        var normalized = Normalize(path);

        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var index = normalized.LastIndexOf(Separator);

        return index < 0 ? normalized : normalized[(index + 1)..];
    }

    /// <summary>
    /// Проверяет, что путь ведёт внутрь хранилища: не абсолютный, не на диске
    /// Windows, без выхода за пределы корня.
    /// </summary>
    public static bool IsSafeRelative(string? path)
    {
        var normalized = Normalize(path);

        if (normalized.Length == 0)
        {
            return false;
        }

        var raw = path!.Trim();

        // «C:/...» и «\server\share» — абсолютные пути в любом соглашении.
        if (Path.IsPathRooted(raw) || raw.StartsWith('/') || raw.StartsWith('\\'))
        {
            return false;
        }

        if (raw.Length >= 2 && raw[1] == ':')
        {
            return false;
        }

        foreach (var segment in normalized.Split(Separator))
        {
            if (segment == "..")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// MIME-тип по расширению. Заменяет два расходящихся switch-а
    /// (12 расширений в RandomMemeController и 13 в MediaInfoApiController).
    /// </summary>
    public static string GetContentType(string? fileNameOrPath)
    {
        var extension = Path.GetExtension(fileNameOrPath ?? string.Empty);

        if (string.IsNullOrEmpty(extension))
        {
            return "application/octet-stream";
        }

        return ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
