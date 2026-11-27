using System;
using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Downloads;

public sealed record MediaSourceDownloadResult(
    string SourcePath,
    string? AudioPath,
    string Title,
    TimeSpan Duration,
    MediaDownloadArtifactKind Kind,
    string MimeType
);