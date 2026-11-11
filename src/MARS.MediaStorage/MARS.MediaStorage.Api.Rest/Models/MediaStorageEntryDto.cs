using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>
/// Представление записи хранилища для UI.
/// </summary>
/// <remarks>
/// Даты отдаются в ISO-8601 UTC, чтобы клиент не зависел от локальной зоны.
/// </remarks>
public class MediaStorageEntryDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Путь относительно wwwroot в канонической форме.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public MediaType MediaType { get; set; }

    public long SizeBytes { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public DateTimeOffset? LastDownloadedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? OriginalPath { get; set; }

    public bool IsDeleted { get; set; }

    public static MediaStorageEntryDto From(MediaStorageEntry entry) =>
        new()
        {
            Id = entry.Id,
            Path = entry.Path,
            FileName = entry.FileName,
            Extension = entry.Extension,
            MediaType = entry.MediaType,
            SizeBytes = entry.SizeBytes,
            UploadedAt = entry.UploadedAt,
            LastDownloadedAt = entry.LastDownloadedAt,
            DeletedAt = entry.DeletedAt,
            OriginalPath = entry.OriginalPath,
            IsDeleted = entry.IsDeleted,
        };
}
