using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Storage;

/// <summary>
/// Ограниченная страница глобального поиска по индексированным файлам.
/// </summary>
public sealed record MediaStorageSearchResult(
    IReadOnlyList<MediaStorageEntry> Entries,
    int Total,
    int Page,
    int PageSize
);
