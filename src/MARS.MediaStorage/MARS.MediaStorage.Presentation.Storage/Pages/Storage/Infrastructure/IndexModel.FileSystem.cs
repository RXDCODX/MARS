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
                if (!System.IO.File.Exists(current) && !Directory.Exists(current))
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

        return System.IO.File.Exists(fullPath);
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (System.IO.File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
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
}
