using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MARS.MediaStorage.Application.Git;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace MARS.MediaStorage.Presentation.Storage.Pages.Storage;

public sealed partial class IndexModel
{
    private async Task LoadPreviewAsync(CancellationToken cancellationToken)
    {
        if (ShowTrash || string.IsNullOrWhiteSpace(PreviewPath))
        {
            return;
        }

        var relativePath = MediaPath.Normalize(PreviewPath);
        if (!MediaPath.IsSafeRelative(relativePath) || !MediaPath.IsVisibleRelativePath(relativePath))
        {
            PreviewMessage = "Недопустимый путь файла.";
            return;
        }

        var parent = GetParentDirectory(relativePath);
        var browseResult = await GetBrowseAsync(parent, cancellationToken);
        PreviewFile = browseResult.Files.FirstOrDefault(file =>
            string.Equals(file.RelativePath, relativePath, StringComparison.Ordinal)
        );

        if (PreviewFile is null)
        {
            PreviewMessage = "Файл не найден или больше не доступен.";
            return;
        }

        PreviewPath = relativePath;
        var extension = Path.GetExtension(relativePath);

        if (ImageExtensions.Contains(extension))
        {
            PreviewKind = "image";
            return;
        }

        if (VideoExtensions.Contains(extension))
        {
            PreviewKind = "video";
            return;
        }

        if (AudioExtensions.Contains(extension))
        {
            PreviewKind = "audio";
            return;
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            PreviewKind = "pdf";
            return;
        }

        if (!TextExtensions.Contains(extension))
        {
            PreviewMessage = "Для этого формата предпросмотр не поддерживается. Файл можно скачать.";
            return;
        }

        if (!TryResolveSafeFilePath(relativePath, out var fullPath))
        {
            PreviewMessage = "Файл не найден или недоступен.";
            return;
        }

        try
        {
            var fileInfo = new FileInfo(fullPath);
            var buffer = new byte[TextPreviewLimitBytes + 1];
            var bytesRead = 0;

            await using (var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            ))
            {
                while (bytesRead < buffer.Length)
                {
                    var read = await stream.ReadAsync(
                        buffer.AsMemory(bytesRead, buffer.Length - bytesRead),
                        cancellationToken
                    );
                    if (read == 0)
                    {
                        break;
                    }

                    bytesRead += read;
                }
            }

            var contentLength = Math.Min(bytesRead, TextPreviewLimitBytes);
            using var contentStream = new MemoryStream(buffer, 0, contentLength);
            using var reader = new StreamReader(
                contentStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true
            );
            PreviewText = await reader.ReadToEndAsync(cancellationToken);
            PreviewTextTruncated = fileInfo.Length > TextPreviewLimitBytes || bytesRead > TextPreviewLimitBytes;
            PreviewKind = "text";
            await storage.MarkDownloadedAsync(relativePath, cancellationToken);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or DecoderFallbackException
        )
        {
            logger.LogWarning(exception, "Unable to preview MediaStorage text file {Path}.", relativePath);
            PreviewMessage = "Не удалось прочитать содержимое файла.";
        }
    }
}
