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
