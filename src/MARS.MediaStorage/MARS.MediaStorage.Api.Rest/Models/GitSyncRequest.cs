namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>Запрос на синхронизацию выбранных записей хранилища.</summary>
public sealed class GitSyncRequest
{
    public required List<Guid> Ids { get; init; }
}
