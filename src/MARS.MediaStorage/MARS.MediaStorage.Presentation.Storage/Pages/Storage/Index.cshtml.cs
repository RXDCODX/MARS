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

/// <summary>
/// Серверная страница браузера хранилища. Все действия выполняются обычными
/// GET/POST-запросами; клиентский JavaScript для работы страницы не требуется.
/// </summary>
[RequestSizeLimit(1024L * 1024 * 1024)]
public sealed class IndexModel(
    IMediaStorageService storage,
    IMediaStorageBrowserService browser,
    IMediaGitService gitService,
    IWebHostEnvironment environment,
    IMemoryCache memoryCache,
    ILogger<IndexModel> logger
) : PageModel
{
    private const int PageSize = 50;
    private const int TextPreviewLimitBytes = 512 * 1024;
    private const int DefaultNameWidth = 360;
    private const int DefaultTypeWidth = 120;
    private const int DefaultSizeWidth = 110;
    private const int MaximumTreeItems = 500;
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

    [BindProperty(Name = "nameWidth", SupportsGet = true)]
    public int NameWidth { get; set; } = DefaultNameWidth;

    [BindProperty(Name = "typeWidth", SupportsGet = true)]
    public int TypeWidth { get; set; } = DefaultTypeWidth;

    [BindProperty(Name = "sizeWidth", SupportsGet = true)]
    public int SizeWidth { get; set; } = DefaultSizeWidth;

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
        if (NameWidth <= 0)
        {
            NameWidth = DefaultNameWidth;
        }

        NameWidth = Math.Clamp(NameWidth, 160, 900);
        TypeWidth = Math.Clamp(TypeWidth <= 0 ? DefaultTypeWidth : TypeWidth, 70, 220);
        SizeWidth = Math.Clamp(SizeWidth <= 0 ? DefaultSizeWidth : SizeWidth, 80, 220);
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
            SetStatus($"Индексация завершена: добавлено {result.Added}, обновлено {result.Updated}.", "success");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MediaStorage indexing failed.");
            SetStatus(exception.Message, "danger");
        }

        return RedirectToCurrentPage();
    }

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

    public async Task<IActionResult> OnPostDeletePathAsync(CancellationToken cancellationToken)
    {
        var relativePath = MediaPath.Normalize(DeletePath);
        if (!MediaPath.IsSafeRelative(relativePath) || !MediaPath.IsVisibleRelativePath(relativePath))
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
                string.Equals(MediaPath.Normalize(item.Path), relativePath, StringComparison.Ordinal)
            );

            if (entry is null)
            {
                SetStatus("Файл не найден в индексе хранилища.", "danger");
                return RedirectToCurrentPage();
            }

            var result = await storage.SoftDeleteAsync([entry.Id], cancellationToken: cancellationToken);
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
                Errors = [.. result.Errors, .. Enumerable.Repeat("Пустой файл пропущен.", skippedEmpty)],
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
        if (!MediaPath.IsSafeRelative(relativePath) || !MediaPath.IsVisibleRelativePath(relativePath))
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
                string.Equals(MediaPath.Normalize(item.Path), relativePath, StringComparison.Ordinal)
            );

            if (existing is null)
            {
                SetStatus("Исходный файл не найден в индексе хранилища.", "danger");
                return RedirectToCurrentPage();
            }

            oldEntryId = existing.Id;
            var deleted = await storage.SoftDeleteAsync([existing.Id], cancellationToken: cancellationToken);
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
                    await storage.RestoreAsync([oldEntryId.Value], cancellationToken: CancellationToken.None);
                }
                catch (Exception restoreException)
                {
                    logger.LogError(restoreException, "Unable to restore old file after replacement failure.");
                }
            }

            logger.LogError(exception, "MediaStorage file replacement failed for {Path}.", relativePath);
            SetStatus(exception.Message, "danger");
        }

        InvalidateBrowseCache();
        return RedirectToCurrentPage();
    }

    public string DirectoryUrl(string? directory) =>
        BuildUrl(directory, null, 1, false, null, Expanded);

    public string PreviewPageUrl(string relativePath) =>
        BuildUrl(CurrentDirectory, SearchQuery, PageNumber, ShowTrash, relativePath, Expanded);

    public string TreeToggleUrl(string relativePath)
    {
        var expanded = new HashSet<string>(Expanded, StringComparer.Ordinal);
        if (!expanded.Add(relativePath))
        {
            expanded.Remove(relativePath);
        }

        return BuildUrl(CurrentDirectory, SearchQuery, PageNumber, ShowTrash, PreviewPath, expanded);
    }

    public string TrashUrl => BuildUrl(null, null, 1, true, null);

    public string BrowseUrl => BuildUrl(CurrentDirectory, null, 1, false, null, Expanded);

    public string SortUrl(string column)
    {
        var direction = SortColumn == column && SortDirection == "asc" ? "desc" : "asc";
        return BuildUrl(CurrentDirectory, SearchQuery, PageNumber, ShowTrash, PreviewPath, Expanded, column, direction);
    }

    public string SearchPageUrl(int page) =>
        BuildUrl(CurrentDirectory, SearchQuery, page, false, null, Expanded);

    public string GetFileTypeName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrEmpty(extension) ? "Файл" : extension.TrimStart('.').ToUpperInvariant();
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 Б";
        }

        string[] units = ["Б", "КБ", "МБ", "ГБ", "ТБ"];
        var value = (double)bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    public static string FormatDate(DateTimeOffset? value) =>
        value?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "—";

    private async Task<MediaStorageBrowseResult> GetBrowseAsync(
        string? directory,
        CancellationToken cancellationToken
    )
    {
        var relativeDirectory = MediaPath.Normalize(directory);
        var key = $"MediaStorage.Presentation.Browse:{Volatile.Read(ref _browseCacheGeneration)}:{relativeDirectory}";

        var result = await memoryCache.GetOrCreateAsync(
            key,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = BrowseCacheLifetime;
                return await browser.BrowseAsync(relativeDirectory, cancellationToken);
            }
        );

        return result ?? await browser.BrowseAsync(relativeDirectory, cancellationToken);
    }

    private async Task LoadDirectoryTreeAsync(CancellationToken cancellationToken)
    {
        TreeItems.Clear();
        var expanded = new HashSet<string>(
            Expanded.Where(path => MediaPath.IsSafeRelative(path) && MediaPath.IsVisibleRelativePath(path)),
            StringComparer.Ordinal
        );

        var current = string.Empty;
        foreach (var segment in (CurrentDirectory ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = MediaPath.Combine(current, segment);
            expanded.Add(current);
        }

        try
        {
            var root = await GetBrowseAsync(string.Empty, cancellationToken);
            await AddTreeItemsAsync(root, 0, expanded, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Unable to load MediaStorage directory tree.");
        }

        Expanded = expanded.ToArray();
    }

    private async Task AddTreeItemsAsync(
        MediaStorageBrowseResult parent,
        int depth,
        HashSet<string> expanded,
        CancellationToken cancellationToken
    )
    {
        if (depth >= 16 || TreeItems.Count >= MaximumTreeItems)
        {
            return;
        }

        foreach (var directory in parent.Directories)
        {
            if (TreeItems.Count >= MaximumTreeItems)
            {
                return;
            }

            var isExpanded = expanded.Contains(directory.RelativePath);
            TreeItems.Add(
                new DirectoryTreeItem(
                    directory.Name,
                    directory.RelativePath,
                    depth,
                    directory.HasChildren,
                    isExpanded,
                    string.Equals(directory.RelativePath, CurrentDirectory, StringComparison.Ordinal)
                )
            );

            if (!isExpanded || !directory.HasChildren)
            {
                continue;
            }

            try
            {
                var child = await GetBrowseAsync(directory.RelativePath, cancellationToken);
                await AddTreeItemsAsync(child, depth + 1, expanded, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Unable to expand MediaStorage directory {Directory}.", directory.RelativePath);
            }
        }
    }

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

    private bool TryResolveSafeFilePath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        var normalized = MediaPath.Normalize(relativePath);
        if (!MediaPath.IsSafeRelative(normalized) || !MediaPath.IsVisibleRelativePath(normalized))
        {
            return false;
        }

        var root = Path.GetFullPath(environment.WebRootPath);
        if (!Directory.Exists(root) || IsReparsePoint(root))
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;

        fullPath = Path.GetFullPath(
            Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar))
        );
        if (!fullPath.StartsWith(rootPrefix, comparison))
        {
            fullPath = string.Empty;
            return false;
        }

        var current = root;
        try
        {
            foreach (var segment in normalized.Split('/'))
            {
                current = Path.Combine(current, segment);
                if (!File.Exists(current) && !Directory.Exists(current))
                {
                    fullPath = string.Empty;
                    return false;
                }

                if (IsReparsePoint(current))
                {
                    fullPath = string.Empty;
                    return false;
                }
            }
        }
        catch (IOException)
        {
            fullPath = string.Empty;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            fullPath = string.Empty;
            return false;
        }

        return File.Exists(fullPath);
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException)
        {
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static string GetParentDirectory(string relativePath)
    {
        var separatorIndex = relativePath.LastIndexOf('/');
        return separatorIndex < 0 ? string.Empty : relativePath[..separatorIndex];
    }

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

    private void InvalidateBrowseCache() => Interlocked.Increment(ref _browseCacheGeneration);

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
        bool IsCurrent
    );
}
