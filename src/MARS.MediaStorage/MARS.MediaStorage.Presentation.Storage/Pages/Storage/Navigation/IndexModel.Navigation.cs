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
    public string DirectoryUrl(string? directory) =>
        BuildUrl(directory, null, 1, false, null, Expanded);

    public string PreviewPageUrl(string relativePath)
    {
        var normalized = MediaPath.Normalize(relativePath);
        return BuildUrl(GetParentDirectory(normalized), null, 1, false, normalized, Expanded);
    }

    public string FileDownloadUrl(string relativePath) =>
        $"/api/MediaStorage/file?path={Uri.EscapeDataString(MediaPath.Normalize(relativePath))}";

    public static string GetTreeFileDisplayName(string fileName) => GetFileDisplayName(fileName);

    /// <summary>
    /// Returns a file name without its extension for display in lists and the
    /// tree. Dotfiles such as <c>.gitignore</c> keep their full name.
    /// </summary>
    public static string GetFileDisplayName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var extension = Path.GetExtension(name);
        if (extension.Length == 0 || name.Length == extension.Length)
        {
            return name;
        }

        return name[..^extension.Length];
    }

    public static string GetTreeFileIcon(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".avif" => "🖼️",
            ".mp4" or ".webm" or ".ogv" or ".mov" or ".avi" or ".wmv" => "🎬",
            ".mp3" or ".wav" or ".ogg" or ".opus" or ".flac" => "🎵",
            ".pdf" => "📕",
            ".txt"
            or ".log"
            or ".json"
            or ".xml"
            or ".csv"
            or ".md"
            or ".markdown"
            or ".yaml"
            or ".yml" => "📄",
            _ => "📄",
        };
    }

    public string TrashUrl => BuildUrl(null, null, 1, true, null);

    public string BrowseUrl => BuildUrl(CurrentDirectory, null, 1, false, null, Expanded);

    public string SortUrl(string column)
    {
        var direction = SortColumn == column && SortDirection == "asc" ? "desc" : "asc";
        return BuildUrl(
            CurrentDirectory,
            SearchQuery,
            PageNumber,
            ShowTrash,
            PreviewPath,
            Expanded,
            column,
            direction
        );
    }

    public string SearchPageUrl(int page) =>
        BuildUrl(CurrentDirectory, SearchQuery, page, false, null, Expanded);

    public string GetFileTypeName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrEmpty(extension)
            ? "Файл"
            : extension.TrimStart('.').ToUpperInvariant();
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
}
