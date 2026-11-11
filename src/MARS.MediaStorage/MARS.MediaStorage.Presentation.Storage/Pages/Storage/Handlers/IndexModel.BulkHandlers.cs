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
    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedIds.Count == 0)
        {
            SetStatus("Сначала выберите файлы.", "warning");
            return RedirectToCurrentPage();
        }

        try
        {
            var result = await storage.SoftDeleteAsync(
                SelectedIds.Distinct().ToArray(),
                DryRun,
                cancellationToken
            );
            SetBulkStatus("Удаление в корзину", result, DryRun);
            if (!DryRun)
            {
                InvalidateBrowseCache();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to move selected MediaStorage files to trash.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(CancellationToken cancellationToken)
    {
        if (SelectedIds.Count == 0)
        {
            SetStatus("Сначала выберите файлы в корзине.", "warning");
            return RedirectToCurrentPage();
        }

        try
        {
            var restored = await storage.RestoreAsync(
                SelectedIds.Distinct().ToArray(),
                DryRun,
                cancellationToken
            );
            SetStatus(
                DryRun
                    ? $"Проверка восстановления завершена: затронуто {restored} файл(ов)."
                    : $"Восстановлено файлов: {restored}.",
                "success"
            );
            if (!DryRun)
            {
                InvalidateBrowseCache();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to restore MediaStorage files.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }

    public async Task<IActionResult> OnPostMoveAsync(CancellationToken cancellationToken)
    {
        if (SelectedIds.Count == 0)
        {
            SetStatus("Сначала выберите файлы для перемещения.", "warning");
            return RedirectToCurrentPage();
        }

        var targetDirectory = TargetDirectory;
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            SetStatus("Укажите каталог назначения.", "danger");
            return RedirectToCurrentPage();
        }

        try
        {
            var result = await storage.MoveAsync(
                SelectedIds.Distinct().ToArray(),
                targetDirectory,
                DryRun,
                cancellationToken
            );
            SetBulkStatus("Перемещение файлов", result, DryRun);
            if (!DryRun)
            {
                InvalidateBrowseCache();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to move MediaStorage files.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }

    public async Task<IActionResult> OnPostSyncGitAsync(CancellationToken cancellationToken)
    {
        if (SelectedIds.Count == 0)
        {
            SetStatus("Сначала выберите файлы для синхронизации.", "warning");
            return RedirectToCurrentPage();
        }

        try
        {
            var ids = SelectedIds.Distinct().ToHashSet();
            var entries = await storage.ListAsync(includeDeleted: false, cancellationToken);
            var selected = entries.Where(entry => ids.Contains(entry.Id)).ToArray();

            if (selected.Length != ids.Count)
            {
                SetStatus("Некоторые выбранные файлы не найдены или находятся в корзине.", "danger");
                return RedirectToCurrentPage();
            }

            var paths = new List<string>(selected.Length);
            foreach (var entry in selected)
            {
                var relative = MediaPath.Normalize(entry.Path);
                if (!MediaPath.IsSafeRelative(relative) || !TryResolveSafeFilePath(relative, out _))
                {
                    SetStatus($"Файл отсутствует или недоступен: {relative}", "danger");
                    return RedirectToCurrentPage();
                }

                paths.Add(relative);
            }

            var result = await gitService.SyncAsync(
                $"Ручная синхронизация медиа: {paths.Count} файл(ов)",
                cancellationToken: cancellationToken,
                paths: paths
            );

            if (!result.Attempted)
            {
                SetStatus("Git-синхронизация отключена (MEDIA_GIT_ENABLED=false).", "warning");
            }
            else if (!result.Success)
            {
                SetStatus(result.Error ?? "Не удалось синхронизировать выбранные файлы.", "danger");
            }
            else
            {
                SetStatus(
                    result.Committed
                        ? $"В Git отправлено файлов: {paths.Count}."
                        : "Изменений для Git-коммита нет.",
                    "success"
                );
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MediaStorage Git synchronization failed.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }
}
