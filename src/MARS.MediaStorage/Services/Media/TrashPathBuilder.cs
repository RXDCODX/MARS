using System.Security.Cryptography;
using System.Text;

namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Строит пути внутри корзины <c>_trash/</c> для мягкого удаления.
/// </summary>
/// <remarks>
/// Аудит Stage 3: удалённые файлы нужно куда-то складывать, и схема пути
/// обязана быть однозначной. Наивные варианты не годятся:
/// <c>_trash/&lt;имя&gt;</c> склеивает файлы с одинаковым именем из разных
/// папок, а повторное удаление после восстановления затёрло бы предыдущую
/// копию. Поэтому в путь входит хеш исходного пути, а расширение файла
/// сохраняется — по нему определяется тип.
/// </remarks>
public static class TrashPathBuilder
{
    public const string TrashFolder = "_trash";

    private const int ShortHashLength = 8;

    /// <summary>
    /// Символы, недопустимые в имени файла, объединённые для всех ОС: путь
    /// корзины едет в гит-репозиторий, который читают и с Windows.
    /// </summary>
    private static readonly char[] ForbiddenFileNameChars =
    [
        .. Path.GetInvalidFileNameChars().Concat("\\\"<>|:?*"),
    ];

    /// <summary>
    /// Даёт уникальный путь в корзине для исходного пути.
    /// </summary>
    /// <param name="relativePath">Путь файла относительно wwwroot.</param>
    /// <param name="deletedAt">Момент удаления — входит в путь, поэтому
    /// два удаления одного файла дают разные пути.</param>
    public static string BuildTrashPath(string? relativePath, DateTimeOffset deletedAt)
    {
        var normalized = MediaPath.Normalize(relativePath);
        var fileName = MediaPath.GetFileName(normalized);
        var extension = Path.GetExtension(fileName);

        // Хеш от пути + времени удаления: гарантирует различие и для разных
        // папок, и для повторных удалений одного файла.
        var seed = $"{normalized}|{deletedAt.ToUnixTimeMilliseconds()}";
        var hash = Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)))[..ShortHashLength]
            .ToLowerInvariant();

        // «..» внутри результата недопустим: Normalize сохраняет такие
        // сегменты, а нам нужен гарантированно безопасный относительный путь.
        // Запрещённые символы берутся не с текущей ОС: на Linux
        // Path.GetInvalidFileNameChars знает только про NUL и «/», и имя вроде
        // мем.jpg осталось бы в общем томе, который читают с Windows.
        var safeName = new string(
            fileName
                .Replace("..", "_", StringComparison.Ordinal)
                .Where(c => Array.IndexOf(ForbiddenFileNameChars, c) < 0)
                .ToArray()
        );

        if (safeName.Length == 0)
        {
            safeName = "file";
        }

        return MediaPath.Combine(
            TrashFolder,
            deletedAt.ToString("yyyy-MM-dd"),
            hash,
            safeName[..(safeName.Length - extension.Length)] + extension
        );
    }

    /// <summary>
    /// Находится ли путь внутри корзины.
    /// </summary>
    public static bool IsUnderTrash(string? relativePath)
    {
        var normalized = MediaPath.Normalize(relativePath);

        return normalized.Equals(TrashFolder, StringComparison.Ordinal)
            || normalized.StartsWith(TrashFolder + "/", StringComparison.Ordinal);
    }
}
