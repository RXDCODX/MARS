namespace MARS.MediaStorage.Application.Storage;

/// <summary>
/// Физическая структура одного каталога хранилища и индексированные метаданные его файлов.
/// </summary>
public sealed record MediaStorageBrowseResult(
    string Name,
    string RelativePath,
    string? ParentDirectory,
    IReadOnlyList<MediaStorageBrowseDirectory> Directories,
    IReadOnlyList<MediaStorageBrowseFile> Files
);

public sealed record MediaStorageBrowseDirectory(
    string Name,
    string RelativePath,
    bool HasChildren
);

public sealed record MediaStorageBrowseFile(
    string Name,
    string RelativePath,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc,
    Guid? StorageEntryId,
    int? MediaType,
    bool IsIndexed,
    DateTimeOffset? UploadedAt,
    DateTimeOffset? LastDownloadedAt
);
