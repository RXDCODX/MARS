using System;
using System.Diagnostics.CodeAnalysis;
using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Downloads;

[ExcludeFromCodeCoverage]
public sealed record MediaSourceDownloadResult(
    string SourcePath,
    string? AudioPath,
    string Title,
    TimeSpan Duration,
    MediaDownloadArtifactKind Kind,
    string MimeType
);
