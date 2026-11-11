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
    private static string NormalizeSortColumn(string? value) =>
        value is "size" or "modified" or "uploaded" or "downloaded" ? value : "name";

    private void SetBulkStatus(string title, BulkOperationResult result, bool dryRun)
    {
        var prefix = dryRun ? $"Проверка: {title.ToLowerInvariant()}" : title;
        var summary = $"{prefix}: успешно {result.Succeeded} из {result.Requested}, ошибок {result.Failed}.";
        var errors = result.Errors.Take(3).ToArray();
        if (errors.Length > 0)
        {
            summary += " " + string.Join(" ", errors);
        }

        SetStatus(summary, result.Failed > 0 ? "warning" : "success");
    }

    private void SetStatus(string message, string tone)
    {
        StatusMessage = message;
        StatusTone = tone;
    }

    private void InvalidateBrowseCache()
    {
        Interlocked.Increment(ref _browseCacheGeneration);
        directoryTreeCache.Invalidate();
    }

    private IActionResult RedirectToCurrentPage() =>
        Redirect(BuildUrl(CurrentDirectory, SearchQuery, PageNumber, ShowTrash, PreviewPath, Expanded));

    private string BuildUrl(
        string? directory,
        string? query,
        int page,
        bool trash,
        string? preview,
        IEnumerable<string>? expanded = null,
        string? sort = null,
        string? direction = null
    )
    {
        var values = new List<string>();
        Add("directory", directory);
        Add("q", query);
        Add("page", page > 1 ? page.ToString() : null);
        Add("trash", trash ? "true" : null);
        Add("preview", preview);
        Add("sort", sort ?? (SortColumn == "name" ? null : SortColumn));
        Add("direction", direction ?? (SortDirection == "asc" ? null : SortDirection));
        Add("nameWidth", NameWidth == DefaultNameWidth ? null : NameWidth.ToString());
        Add("typeWidth", TypeWidth == DefaultTypeWidth ? null : TypeWidth.ToString());
        Add("sizeWidth", SizeWidth == DefaultSizeWidth ? null : SizeWidth.ToString());

        if (expanded is not null)
        {
            foreach (var path in expanded.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                values.Add($"expanded={Uri.EscapeDataString(path)}");
            }
        }

        return values.Count == 0
            ? "/storage-ui/index.html"
            : $"/storage-ui/index.html?{string.Join("&", values)}";

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add($"{name}={Uri.EscapeDataString(value)}");
            }
        }
    }

    public sealed record DirectoryBreadcrumb(string Name, string RelativePath);

    public sealed record DirectoryTreeItem(
        string Name,
        string RelativePath,
        int Depth,
        bool HasChildren,
        bool IsExpanded,
        bool IsCurrent,
        string Url,
        IReadOnlyList<DirectoryTreeItem> Children
    );
}
