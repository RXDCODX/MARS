using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Entities;

/// <summary>
/// Метаданные файла хранилища.
/// </summary>
/// <remarks>
/// Аудит Stage 3: у хранилища не было ни даты загрузки, ни даты последней
/// выгрузки, ни состояния удаления — хранилище велось как «просто папка», и
/// массовые операции по нему были невозможны. Метаданные вынесены в
/// отдельную таблицу, потому что <see cref="MediaInfo"/> — общий контракт
/// алертов, публикуемый в MARS.TwitchCore и MARS.Alerts, и дополнять его
/// полями хранилища нельзя.
/// </remarks>
public class MediaStorageEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Путь относительно wwwroot в канонической форме (прямые слэши, без
    /// ведущего «/»). Уникален — он же служит ключом индексации.
    /// </summary>
    public required string Path { get; set; }

    public required string FileName { get; set; }

    public string Extension { get; set; } = string.Empty;

    public MediaType MediaType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>
    /// Когда файл попал в хранилище. Для файлов, найденных при индексации
    /// существующей папки, берётся время изменения файла.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Когда файл последний раз отдавался клиенту. NULL — не отдавался ни разу.
    /// </summary>
    public DateTimeOffset? LastDownloadedAt { get; set; }

    /// <summary>
    /// NULL — файл жив. Не NULL — файл перемещён в <c>_trash/</c> и ждёт
    /// либо восстановления, либо безвозвратного удаления.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Путь до мягкого удаления. Нужен для восстановления.
    /// </summary>
    public string? OriginalPath { get; set; }

    /// <summary>
    /// SHA-256 содержимого. Позволяет отсеять повторную загрузку того же
    /// файла и проверять целостность при восстановлении.
    /// </summary>
    public string? ContentHash { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// Файл считается просроченным для безвозвратного удаления, когда
    /// мягкое удаление было дольше <paramref name="retention"/> назад.
    /// </summary>
    public bool IsPurgeable(DateTimeOffset now, TimeSpan retention) =>
        DeletedAt.HasValue && now - DeletedAt.Value >= retention;
}
