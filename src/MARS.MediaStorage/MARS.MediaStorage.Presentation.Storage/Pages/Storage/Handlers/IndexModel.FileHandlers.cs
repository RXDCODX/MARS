using System.Text;
using MARS.MediaStorage.Application.Git;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Presentation.Storage.Pages.Storage;

public sealed partial class IndexModel
{
    public async Task<IActionResult> OnPostDeletePathAsync(CancellationToken cancellationToken)
    {
        var relativePath = MediaPath.Normalize(DeletePath);
        if (
            !MediaPath.IsSafeRelative(relativePath)
            || !MediaPath.IsVisibleRelativePath(relativePath)
        )
        {
            SetStatus("Недопустимый путь файла.", "danger");
            return RedirectToCurrentPage();
        }

        if (!TryResolveSafeFilePath(relativePath, out _))
        {
            SetStatus("Файл не найден или недоступен.", "danger");
            return RedirectToCurrentPage();
        }

        try
        {
            await storage.IndexAsync(cancellationToken);
            var parent = GetParentDirectory(relativePath);
            var entries = await storage.ListDirectoryAsync(parent, cancellationToken);
            var entry = entries.FirstOrDefault(item =>
                string.Equals(
                    MediaPath.Normalize(item.Path),
                    relativePath,
                    StringComparison.Ordinal
                )
            );

            if (entry is null)
            {
                SetStatus("Файл не найден в индексе хранилища.", "danger");
                return RedirectToCurrentPage();
            }

            var result = await storage.SoftDeleteAsync(
                [entry.Id],
                cancellationToken: cancellationToken
            );
            SetBulkStatus("Удаление в корзину", result, false);
            InvalidateBrowseCache();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to delete MediaStorage file {Path}.", relativePath);
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }

    public async Task<IActionResult> OnPostUploadAsync(CancellationToken cancellationToken)
    {
        var uploads = new List<MediaUploadFile>();
        var skippedEmpty = 0;

        try
        {
            foreach (var file in UploadFiles)
            {
                if (file.Length <= 0)
                {
                    skippedEmpty++;
                    continue;
                }

                uploads.Add(
                    new MediaUploadFile(
                        Path.GetFileName(file.FileName),
                        file.OpenReadStream(),
                        file.ContentType,
                        file.Length
                    )
                );
            }

            if (uploads.Count == 0)
            {
                SetStatus("Выберите хотя бы один непустой файл.", "warning");
                return RedirectToCurrentPage();
            }

            var result = await storage.UploadAsync(uploads, UploadTarget, cancellationToken);
            var merged = result with
            {
                Requested = UploadFiles.Count,
                Failed = result.Failed + skippedEmpty,
                Errors =
                [
                    .. result.Errors,
                    .. Enumerable.Repeat("Пустой файл пропущен.", skippedEmpty),
                ],
            };

            SetBulkStatus("Загрузка файлов", merged, false);
            InvalidateBrowseCache();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MediaStorage upload failed.");
            SetStatus(exception.Message, "danger");
        }
        finally
        {
            foreach (var upload in uploads)
            {
                upload.Dispose();
            }
        }

        return RedirectToCurrentPage();
    }

    public async Task<IActionResult> OnPostReplaceAsync(CancellationToken cancellationToken)
    {
        var relativePath = MediaPath.Normalize(ReplacementPath);
        if (
            !MediaPath.IsSafeRelative(relativePath)
            || !MediaPath.IsVisibleRelativePath(relativePath)
        )
        {
            SetStatus("Недопустимый путь заменяемого файла.", "danger");
            return RedirectToCurrentPage();
        }

        var replacementFile = ReplacementFile;
        if (replacementFile is null || replacementFile.Length <= 0)
        {
            SetStatus("Выберите непустой файл для замены.", "warning");
            return RedirectToCurrentPage();
        }

        if (!TryResolveSafeFilePath(relativePath, out _))
        {
            SetStatus("Исходный файл не найден или недоступен.", "danger");
            return RedirectToCurrentPage();
        }

        Guid? oldEntryId = null;
        try
        {
            await storage.IndexAsync(cancellationToken);
            var parent = GetParentDirectory(relativePath);
            var entries = await storage.ListDirectoryAsync(parent, cancellationToken);
            var existing = entries.FirstOrDefault(item =>
                string.Equals(
                    MediaPath.Normalize(item.Path),
                    relativePath,
                    StringComparison.Ordinal
                )
            );

            if (existing is null)
            {
                SetStatus("Исходный файл не найден в индексе хранилища.", "danger");
                return RedirectToCurrentPage();
            }

            var existingExtension = Path.GetExtension(existing.Path);
            var replacementExtension = Path.GetExtension(replacementFile.FileName);
            if (
                !string.Equals(
                    existingExtension,
                    replacementExtension,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                SetStatus(
                    $"Формат не совпадает: исходный файл {existingExtension}, выбранный файл {replacementExtension}.",
                    "danger"
                );
                return RedirectToCurrentPage();
            }

            oldEntryId = existing.Id;
            var deleted = await storage.SoftDeleteAsync(
                [existing.Id],
                cancellationToken: cancellationToken
            );
            if (deleted.Succeeded != 1)
            {
                SetBulkStatus("Сохранение старой версии", deleted, false);
                return RedirectToCurrentPage();
            }

            using var replacement = new MediaUploadFile(
                Path.GetFileName(relativePath),
                replacementFile.OpenReadStream(),
                replacementFile.ContentType,
                replacementFile.Length
            );

            var uploaded = await storage.UploadAsync([replacement], parent, cancellationToken);
            if (uploaded.Succeeded == 1)
            {
                SetBulkStatus("Файл заменён", uploaded, false);
                InvalidateBrowseCache();
                return RedirectToCurrentPage();
            }

            await storage.RestoreAsync([existing.Id], cancellationToken: cancellationToken);
            SetBulkStatus("Замена не выполнена; исходная версия восстановлена", uploaded, false);
        }
        catch (Exception exception)
        {
            if (oldEntryId.HasValue)
            {
                try
                {
                    await storage.RestoreAsync(
                        [oldEntryId.Value],
                        cancellationToken: CancellationToken.None
                    );
                }
                catch (Exception restoreException)
                {
                    logger.LogError(
                        restoreException,
                        "Unable to restore old file after replacement failure."
                    );
                }
            }

            logger.LogError(
                exception,
                "MediaStorage file replacement failed for {Path}.",
                relativePath
            );
            SetStatus(exception.Message, "danger");
        }

        InvalidateBrowseCache();
        return RedirectToCurrentPage();
    }
}
