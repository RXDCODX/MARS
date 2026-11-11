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
}
