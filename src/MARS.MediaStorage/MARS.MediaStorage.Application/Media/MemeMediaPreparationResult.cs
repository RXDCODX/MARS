using MARS.MediaStorage.Application.Extensions;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Hosting;

namespace MARS.MediaStorage.Application.Media;

/// <summary>
/// Итог подготовки файла.
/// </summary>
public sealed record MemeMediaPreparationResult(
    MediaInfo? Media,
    bool WasTranscoded,
    bool FileExists
)
{
    public static MemeMediaPreparationResult Ready(MediaInfo media, bool wasTranscoded) =>
        new(media, wasTranscoded, true);

    public static MemeMediaPreparationResult Missing() => new(null, false, false);
}
