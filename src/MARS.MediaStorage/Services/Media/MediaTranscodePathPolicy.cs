using System.Security.Cryptography;
using System.Text;

namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Политика путей транскодирования.
/// Аудит №13: прежний код писал результат рядом с исходником (foo.mp4 рядом с
/// foo.webm), удалял оригинал и возвращал новый путь. Второй вызов
/// EnsurePlayableAsync(originalPath) находил уже удалённый файл и падал.
/// Теперь оригинал сохраняется, а результат кэшируется в отдельной папке по
/// стабильному ключу, поэтому путь разрешается одинаково при каждом вызове.
/// </summary>
public static class MediaTranscodePathPolicy
{
    public const string ConvertedFolderName = "_converted";
    public const string ConvertedExtension = ".mp4";

    /// <summary>
    /// Детерминированный путь кэша: одинаковый источник → один и тот же файл,
    /// независимо от времени модификации (время идёт в ключ проверки актуальности).
    /// </summary>
    public static string GetTranscodedCachePath(string sourceFilePath, string convertedRoot)
    {
        var normalized = Path.GetFullPath(sourceFilePath).Replace('\\', '/');
        var hash = Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))
            .ToLowerInvariant();

        return Path.Combine(convertedRoot, hash + ConvertedExtension);
    }

    public static bool ShouldReuseCache(
        bool cacheExists,
        DateTime cacheWriteUtc,
        DateTime sourceWriteUtc
    )
    {
        return cacheExists && cacheWriteUtc >= sourceWriteUtc;
    }

    /// <summary>
    /// Путь dev-копии медиа под wwwroot. В Production возвращает null — там
    /// монолит и сервис работают с одним и тем же каталогом.
    /// </summary>
    public static string? GetMirroredWebRootPath(
        string filePath,
        string webRootPath,
        bool isDevelopment
    )
    {
        if (!isDevelopment || string.IsNullOrWhiteSpace(webRootPath))
        {
            return null;
        }

        var normalized = Path.GetFullPath(filePath);
        return Path.Combine(Path.GetFullPath(webRootPath), Path.GetFileName(normalized));
    }
}
