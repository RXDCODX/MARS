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
    public async Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
    {
        InvalidateBrowseCache();
        try
        {
            GitStatus = await gitService.GetStatusAsync(cancellationToken);
            SetStatus("Структура хранилища обновлена.", "success");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to refresh MediaStorage.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }

    public async Task<IActionResult> OnPostIndexAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await storage.IndexAsync(cancellationToken);
            InvalidateBrowseCache();
            SetStatus(
                $"Индексация завершена: добавлено {result.Added}, обновлено {result.Updated}.",
                "success"
            );
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MediaStorage indexing failed.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }
}
