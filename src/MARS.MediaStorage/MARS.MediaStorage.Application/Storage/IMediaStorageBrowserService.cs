namespace MARS.MediaStorage.Application.Storage;

/// <summary>
/// Читает физическую структуру хранилища. Каталоги не индексируются в БД,
/// поэтому источником истины для дерева остаётся файловая система.
/// </summary>
public interface IMediaStorageBrowserService
{
    Task<MediaStorageBrowseResult> BrowseAsync(
        string? directory,
        CancellationToken cancellationToken = default
    );
}
