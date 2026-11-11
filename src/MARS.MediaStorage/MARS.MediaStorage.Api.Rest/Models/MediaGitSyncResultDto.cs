namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>Результат Git-синхронизации выбранных медиафайлов.</summary>
public sealed record MediaGitSyncResultDto(int FilesSelected, bool Committed);
