using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Storage;

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

    Task<MediaStorageSearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 50,
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
