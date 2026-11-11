namespace MARS.MediaStorage.Application.Storage;

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
