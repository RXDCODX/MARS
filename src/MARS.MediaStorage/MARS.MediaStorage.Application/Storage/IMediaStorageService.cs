using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Storage;

/// <summary>
/// Итог операции над набором файлов.
/// </summary>
/// <param name="Requested">Сколько записей передали.</param>
/// <param name="Succeeded">Сколько обработано без ошибок.</param>
/// <param name="Failed">Сколько пропущено с ошибкой.</param>
/// <param name="Errors">Человекочитаемые причины пропуска.</param>
public readonly record struct BulkOperationResult(
    int Requested,
    int Succeeded,
    int Failed,
    IReadOnlyList<string> Errors
);

public readonly record struct IndexResult(int Added, int Updated);

public interface IMediaStorageService
{
    Task<IndexResult> IndexAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaStorageEntry>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Возвращает только индексированные файлы, непосредственно лежащие в каталоге.
    /// </summary>
    Task<IReadOnlyList<MediaStorageEntry>> ListDirectoryAsync(
        string directory,
        CancellationToken cancellationToken = default
    );

    Task<BulkOperationResult> SoftDeleteAsync(
        IReadOnlyCollection<Guid> ids,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    );

    Task<int> RestoreAsync(
        IReadOnlyCollection<Guid> ids,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    );

    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Приём файлов в хранилище.
    /// </summary>
    Task<BulkOperationResult> UploadAsync(
        IReadOnlyCollection<MediaUploadFile> files,
        string targetDirectory,
        CancellationToken cancellationToken = default
    );

    Task<BulkOperationResult> MoveAsync(
        IReadOnlyCollection<Guid> ids,
        string targetDirectory,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Фиксирует факт выдачи файла. Ошибка фиксации не должна ломать отдачу:
    /// дата — вспомогательные данные, а не условие успешного ответа.
    /// </summary>
    Task MarkDownloadedAsync(string relativePath, CancellationToken cancellationToken = default);
}
