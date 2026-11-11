namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>Выбранные записи хранилища для ручной Git-синхронизации.</summary>
public sealed class MediaGitSyncRequest
{
    public required List<Guid> Ids { get; set; }
}

/// <summary>Результат Git-синхронизации выбранных медиафайлов.</summary>
public sealed record MediaGitSyncResultDto(int FilesSelected, bool Committed);
