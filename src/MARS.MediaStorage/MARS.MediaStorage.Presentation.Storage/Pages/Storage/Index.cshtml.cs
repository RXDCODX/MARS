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
using MARS.MediaStorage.Presentation.Storage.Services;

namespace MARS.MediaStorage.Presentation.Storage.Pages.Storage;

/// <summary>
/// Серверная страница браузера хранилища. Данные и операции остаются на сервере;
/// JavaScript используется только для локальных UI-взаимодействий.
/// </summary>
[RequestSizeLimit(1024L * 1024 * 1024)]
public sealed partial class IndexModel(
    IMediaStorageService storage,
    IMediaStorageBrowserService browser,
    IMediaGitService gitService,
    IWebHostEnvironment environment,
    IMemoryCache memoryCache,
    MediaStorageDirectoryTreeCache directoryTreeCache,
    ILogger<IndexModel> logger
) : PageModel
{
    private const int PageSize = 50;
    private const int TextPreviewLimitBytes = 512 * 1024;
    private static int _browseCacheGeneration;

    private static readonly TimeSpan BrowseCacheLifetime = TimeSpan.FromSeconds(20);

    private static readonly HashSet<string> ImageExtensions = new(
        [".png", ".jpg", ".jpeg", ".gif", ".webp", ".avif"],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> VideoExtensions = new(
        [".mp4", ".webm", ".ogv", ".mov", ".avi", ".wmv"],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> AudioExtensions = new(
        [".mp3", ".wav", ".ogg", ".opus", ".flac"],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> TextExtensions = new(
        [".txt", ".log", ".json", ".xml", ".csv", ".md", ".markdown", ".yaml", ".yml"],
        StringComparer.OrdinalIgnoreCase
    );

    [BindProperty(Name = "directory", SupportsGet = true)]
    public string? CurrentDirectory { get; set; }

    [BindProperty(Name = "q", SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(Name = "page", SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(Name = "trash", SupportsGet = true)]
    public bool ShowTrash { get; set; }

    [BindProperty(Name = "preview", SupportsGet = true)]
    public string? PreviewPath { get; set; }

    [BindProperty(Name = "expanded", SupportsGet = true)]
    public string[] Expanded { get; set; } = [];

    [BindProperty(Name = "sort", SupportsGet = true)]
    public string SortColumn { get; set; } = "name";

    [BindProperty(Name = "direction", SupportsGet = true)]
    public string SortDirection { get; set; } = "asc";

    [BindProperty]
    public List<Guid> SelectedIds { get; set; } = [];

    [BindProperty]
    public bool DryRun { get; set; }

    [BindProperty]
    public string? TargetDirectory { get; set; }

    [BindProperty]
    public List<IFormFile> UploadFiles { get; set; } = [];

    [BindProperty]
    public string? DeletePath { get; set; }

    [BindProperty]
    public string? ReplacementPath { get; set; }

    [BindProperty]
    public IFormFile? ReplacementFile { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusTone { get; set; }

    public MediaStorageBrowseResult? BrowseResult { get; private set; }

    public MediaStorageSearchResult? SearchResult { get; private set; }

    public IReadOnlyList<MediaStorageEntry> TrashEntries { get; private set; } = [];

    public GitStatusResult? GitStatus { get; private set; }

    public MediaStorageBrowseFile? PreviewFile { get; private set; }

    public string? PreviewKind { get; private set; }

    public string? PreviewText { get; private set; }

    public bool PreviewTextTruncated { get; private set; }

    public string? PreviewMessage { get; private set; }

    public List<DirectoryTreeItem> TreeItems { get; } = [];

    public List<TreeFileItem> RootFiles { get; } = [];

    public IEnumerable<DirectoryBreadcrumb> Breadcrumbs
    {
        get
        {
            var current = string.Empty;
            foreach (var segment in (BrowseResult?.RelativePath ?? string.Empty).Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries
            ))
            {
                current = MediaPath.Combine(current, segment);
                yield return new DirectoryBreadcrumb(segment, current);
            }
        }
    }

    public IEnumerable<MediaStorageBrowseFile> SortedFiles
    {
        get
        {
            var files = BrowseResult?.Files ?? [];
            return SortColumn switch
            {
                "size" => SortDirection == "desc"
                    ? files.OrderByDescending(file => file.SizeBytes)
                    : files.OrderBy(file => file.SizeBytes),
                "modified" => SortDirection == "desc"
                    ? files.OrderByDescending(file => file.LastWriteTimeUtc)
                    : files.OrderBy(file => file.LastWriteTimeUtc),
                _ => SortDirection == "desc"
                    ? files.OrderByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
                    : files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase),
            };
        }
    }

    public IEnumerable<MediaStorageEntry> SortedSearchEntries
    {
        get
        {
            var entries = SearchResult?.Entries ?? [];
            return SortColumn switch
            {
                "size" => SortDirection == "desc"
                    ? entries.OrderByDescending(entry => entry.SizeBytes)
                    : entries.OrderBy(entry => entry.SizeBytes),
                "uploaded" => SortDirection == "desc"
                    ? entries.OrderByDescending(entry => entry.UploadedAt)
                    : entries.OrderBy(entry => entry.UploadedAt),
                "downloaded" => SortDirection == "desc"
                    ? entries.OrderByDescending(entry => entry.LastDownloadedAt)
                    : entries.OrderBy(entry => entry.LastDownloadedAt),
                _ => SortDirection == "desc"
                    ? entries.OrderByDescending(entry => entry.FileName, StringComparer.OrdinalIgnoreCase)
                    : entries.OrderBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase),
            };
        }
    }

    public IEnumerable<MediaStorageEntry> SortedTrashEntries =>
        SortDirection == "desc"
            ? TrashEntries.OrderByDescending(entry => entry.DeletedAt)
            : TrashEntries.OrderBy(entry => entry.DeletedAt);

    public int SearchPageCount =>
        SearchResult is null ? 1 : Math.Max(1, (int)Math.Ceiling(SearchResult.Total / (double)SearchResult.PageSize));

    public string PreviewUrl =>
        PreviewFile is null
            ? string.Empty
            : $"/api/MediaStorage/preview?path={Uri.EscapeDataString(PreviewFile.RelativePath)}";

    public string DownloadUrl =>
        PreviewFile is null
            ? string.Empty
            : $"/api/MediaStorage/file?path={Uri.EscapeDataString(PreviewFile.RelativePath)}";

    public string UploadTarget =>
        string.IsNullOrWhiteSpace(TargetDirectory)
            ? string.IsNullOrWhiteSpace(CurrentDirectory) ? "Uploads" : CurrentDirectory
            : TargetDirectory;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(1, PageNumber);
        SortColumn = NormalizeSortColumn(SortColumn);
        SortDirection = string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";
        CurrentDirectory = MediaPath.Normalize(CurrentDirectory);
        TargetDirectory ??= string.IsNullOrEmpty(CurrentDirectory) ? "Uploads" : CurrentDirectory;

        try
        {
            GitStatus = await gitService.GetStatusAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read MediaStorage Git status.");
            GitStatus = new GitStatusResult(false, [], null, exception.Message);
        }

        try
        {
            if (ShowTrash)
            {
                var entries = await storage.ListAsync(includeDeleted: true, cancellationToken);
                TrashEntries = entries.Where(entry => entry.IsDeleted).ToArray();
            }
            else if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResult = await storage.SearchAsync(
                    SearchQuery.Trim(),
                    PageNumber,
                    PageSize,
                    cancellationToken
                );
            }
            else
            {
                BrowseResult = await GetBrowseAsync(CurrentDirectory, cancellationToken);
                CurrentDirectory = BrowseResult.RelativePath;
            }

            await LoadDirectoryTreeAsync(cancellationToken);
            await LoadPreviewAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to render MediaStorage directory {Directory}.", CurrentDirectory);
            SetStatus(exception.Message, "danger");
            BrowseResult = null;
        }
    }
}
