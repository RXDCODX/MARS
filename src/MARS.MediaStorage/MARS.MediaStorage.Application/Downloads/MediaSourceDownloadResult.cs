using System;
using MARS.MediaStorage.Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Application.Downloads;

public sealed record MediaSourceDownloadResult(
    string SourcePath,
    string? AudioPath,
    string Title,
    TimeSpan Duration,
    MediaDownloadArtifactKind Kind,
    string MimeType
);