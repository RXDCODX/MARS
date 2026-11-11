namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>Выбранные записи хранилища для ручной Git-синхронизации.</summary>
public sealed class MediaGitSyncRequest
{
    public required List<Guid> Ids { get; set; }
}
