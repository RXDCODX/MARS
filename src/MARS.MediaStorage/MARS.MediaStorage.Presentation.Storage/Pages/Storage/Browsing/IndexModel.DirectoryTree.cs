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

public sealed partial class IndexModel
{
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
        RootFiles.Clear();
        var expanded = new HashSet<string>(
            Expanded.Where(path => MediaPath.IsSafeRelative(path) && MediaPath.IsVisibleRelativePath(path)),
            StringComparer.Ordinal
        );

        var current = string.Empty;
        foreach (var segment in (CurrentDirectory ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = MediaPath.Combine(current, segment);
            if (MediaPath.IsSafeRelative(current) && MediaPath.IsVisibleRelativePath(current))
            {
                expanded.Add(current);
            }
        }

        Expanded = expanded.ToArray();

        try
        {
            var snapshot = await directoryTreeCache.GetSnapshotAsync(cancellationToken);
            RootFiles.AddRange(
                snapshot.Files.Select(file => new TreeFileItem(file.Name, file.RelativePath, PreviewPageUrl(file.RelativePath)))
            );
            foreach (var directory in snapshot.Directories)
            {
                TreeItems.Add(CreateDirectoryTreeItem(directory, 0, expanded));
            }
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Unable to load the cached MediaStorage directory tree.");
        }
    }

    private DirectoryTreeItem CreateDirectoryTreeItem(
        MediaStorageDirectoryTreeCache.DirectoryTreeNode directory,
        int depth,
        HashSet<string> expanded
    )
    {
        var children = directory.Children
            .Select(child => CreateDirectoryTreeItem(child, depth + 1, expanded))
            .ToArray();
        var files = directory.Files
            .Select(file => new TreeFileItem(file.Name, file.RelativePath, PreviewPageUrl(file.RelativePath)))
            .ToArray();

        return new DirectoryTreeItem(
            directory.Name,
            directory.RelativePath,
            depth,
            children.Length > 0 || files.Length > 0,
            expanded.Contains(directory.RelativePath),
            string.Equals(directory.RelativePath, CurrentDirectory, StringComparison.Ordinal),
            DirectoryUrl(directory.RelativePath),
            children,
            files
        );
    }
}
