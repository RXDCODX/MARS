using MARS.MediaStorage.Extensions;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Hosting;

namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Подготовка файла мема к показу: решить, нужен ли перекод, и перекодировать.
/// </summary>
/// <remarks>
/// Перенос <c>TwitchMediaPreparationService.PrepareMediaAsync</c> из монолита.
/// Отличия, связанные с устройством репозитория:
/// <list type="bullet">
/// <item>мемы и алерты читает владелец — сам MARS.MediaStorage, поэтому
/// переписывать строки в БД после перекодирования не нужно: пути не меняются;</item>
/// <item>оригинал не удаляется (аудит №13) — результат кладётся в кэш
/// <c>_converted</c>, исходник остаётся на месте, и повторный проход не ломается
/// об отсутствующий файл.</item>
/// </list>
/// </remarks>
public sealed class MemeMediaPreparationService(
    IWebHostEnvironment environment,
    IMediaInspector inspector,
    IMediaTranscoder transcoder,
    ILogger<MemeMediaPreparationService> logger
)
{
    public async Task<MemeMediaPreparationResult> PrepareAsync(
        string? relativeFilePath,
        string? displayName = null,
        Func<string, Task>? onFileTranscoded = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = MemeMediaPreparationResult.Missing();

        if (string.IsNullOrWhiteSpace(relativeFilePath))
        {
            return result;
        }

        var sourcePath = ResolveFullPath(relativeFilePath);

        if (!File.Exists(sourcePath))
        {
            return result;
        }

        var extension = Path.GetExtension(sourcePath);
        var mediaType = await extension.GetFileMediaTypeAsync();
        var probe = await inspector.ProbeAsync(sourcePath, cancellationToken);
        var targetPath = MemeMediaTranscodePolicy.GetTargetFilePath(sourcePath, mediaType);

        if (
            !MemeMediaTranscodePolicy.NeedsTranscoding(
                mediaType,
                probe,
                Path.GetExtension(targetPath)
            )
        )
        {
            result = MemeMediaPreparationResult.Ready(
                BuildMediaInfo(sourcePath, displayName),
                false
            );
        }
        else
        {
            var playablePath = await transcoder.EnsurePlayableAsync(sourcePath, cancellationToken);
            var wasTranscoded = !string.Equals(playablePath, sourcePath, StringComparison.Ordinal);

            if (wasTranscoded && onFileTranscoded is not null)
            {
                await onFileTranscoded(
                    MemeMediaTranscodePolicy.BuildFileReport(
                        sourcePath,
                        playablePath,
                        mediaType,
                        probe
                    )
                );
            }

            result = MemeMediaPreparationResult.Ready(
                BuildMediaInfo(playablePath, displayName),
                wasTranscoded
            );
        }

        logger.LogDebug(
            "Мем {FilePath} подготовлен: transcode={Transcoded}, итог {PlayablePath}",
            relativeFilePath,
            result.WasTranscoded,
            result.Media?.FileInfo.FilePath
        );

        return result;
    }

    private string ResolveFullPath(string relativeFilePath)
    {
        return Path.IsPathRooted(relativeFilePath)
            ? relativeFilePath
            : Path.Combine(environment.WebRootPath, MediaPath.Normalize(relativeFilePath));
    }

    private MediaInfo BuildMediaInfo(string fullPath, string? displayName)
    {
        return new MediaInfo
        {
            FileInfo = new MediaFileInfo
            {
                Extension = Path.GetExtension(fullPath),
                Type = Path.GetExtension(fullPath).GetFileMediaType(),
                FileName = Path.GetFileName(fullPath),
                FilePath = BuildWebPath(fullPath),
                IsLocalFile = true,
            },
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = displayName ?? string.Empty,
                IsLooped = false,
            },
            PositionInfo = new MediaPositionInfo
            {
                Height = 400,
                Width = 400,
                IsProportion = true,
                IsResizeRequires = true,
            },
            StylesInfo = new MediaStylesInfo { IsBorder = false },
            TextInfo = new MediaTextInfo(),
        };
    }

    private string BuildWebPath(string fullPath)
    {
        return "/"
            + Path.GetRelativePath(environment.WebRootPath, Path.GetFullPath(fullPath))
                .Replace('\\', '/');
    }
}

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
