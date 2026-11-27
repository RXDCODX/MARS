using System;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Событие завершения durable media-download job в MARS.MediaStorage.
/// Публикуется только после окончательного завершения (успех/ошибка/отмена).
/// </summary>
public sealed class MediaDownloadJobCompletedEvent : IMarsEventMessage
{
    public Guid JobId { get; set; }

    public bool Succeeded { get; set; }

    public string? FailureCategory { get; set; }

    public string? FailureReason { get; set; }

    public string? ArtifactUrl { get; set; }

    public string? FileName { get; set; }

    public string? MimeType { get; set; }

    public int? Kind { get; set; }

    public long SizeBytes { get; set; }

    public double? DurationSeconds { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public bool Trimmed { get; set; }

    public string? Caption { get; set; }

    public DateTimeOffset CompletedAtUtc { get; set; }
}
