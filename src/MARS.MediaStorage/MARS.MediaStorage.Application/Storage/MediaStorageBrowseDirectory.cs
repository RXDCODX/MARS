namespace MARS.MediaStorage.Application.Storage;

public sealed record MediaStorageBrowseDirectory(
    string Name,
    string RelativePath,
    bool HasChildren
);
