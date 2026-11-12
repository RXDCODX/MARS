using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MARS.MediaStorage.Application.Git;
using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Domain.Entities;
using MARS.MediaStorage.Presentation.Storage.Pages.Storage;
using MARS.MediaStorage.Presentation.Storage.Services;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Exercises the real Razor Pages pipeline with an isolated filesystem and mocked storage ports.
/// </summary>
public sealed class MediaStorageRazorPagesTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);

    private readonly MediaStorageEntry _rootEntry = new()
    {
        Path = "root.txt",
        FileName = "root.txt",
        Extension = ".txt",
        SizeBytes = 4,
        UploadedAt = Now,
    };

    private readonly MediaStorageEntry _subEntry = new()
    {
        Path = "Sub/notes.txt",
        FileName = "notes.txt",
        Extension = ".txt",
        SizeBytes = 25,
        UploadedAt = Now,
    };

    private readonly MediaStorageEntry _trashEntry = new()
    {
        Path = "_trash/deleted.txt",
        FileName = "deleted.txt",
        Extension = ".txt",
        SizeBytes = 3,
        UploadedAt = Now.AddDays(-2),
        DeletedAt = Now.AddDays(-1),
        OriginalPath = "Sub/deleted.txt",
    };

    private readonly Mock<IMediaStorageService> _storage = new();
    private readonly Mock<IMediaStorageBrowserService> _browser = new();
    private readonly Mock<IMediaGitService> _git = new();

    private string _webRoot = string.Empty;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "mars-storage-pages-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_webRoot, "Sub", "Nested"));

        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "root.txt"), "root", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "cover.png"), "image", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "clip.mp4"), "video", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "sound.mp3"), "audio", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "document.pdf"), "pdf", cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "Sub", "notes.txt"),
            "Preview contents for Razor Pages.",
            cancellationToken
        );
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "Sub", "Nested", "nested.mp4"),
            "nested video",
            cancellationToken
        );

        ConfigureStorage();
        ConfigureBrowser();

        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { WebRootPath = _webRoot }
        );
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRazorPages(options =>
                options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute())
            )
            .AddApplicationPart(typeof(IndexModel).Assembly);
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IMediaStorageService>(_storage.Object);
        builder.Services.AddSingleton<IMediaStorageBrowserService>(_browser.Object);
        builder.Services.AddSingleton<IMediaGitService>(_git.Object);
        builder.Services.AddSingleton<MediaStorageDirectoryTreeCache>();

        _app = builder.Build();
        _app.UseRouting();
        _app.MapRazorPages();

        await _app.StartAsync(cancellationToken);
        _client = _app.GetTestServer().CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();

        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetRequests_RenderBrowsingSearchTrashAndPreviewPages()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var browseResponse = await _client.GetAsync(
            "/storage-ui/index.html?directory=Sub&sort=unsupported&direction=DESC&expanded=Sub",
            cancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        var browseHtml = await browseResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Хранилище медиа", browseHtml, StringComparison.Ordinal);
        Assert.Contains("notes.txt", browseHtml, StringComparison.Ordinal);
        Assert.Contains("Nested", browseHtml, StringComparison.Ordinal);
        Assert.Contains("root", browseHtml, StringComparison.Ordinal);
        Assert.Contains("Текущий каталог", browseHtml, StringComparison.Ordinal);

        using var previewResponse = await _client.GetAsync(
            "/storage-ui/index.html?directory=Sub&preview=Sub%2Fnotes.txt&expanded=Sub",
            cancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewHtml = await previewResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Preview contents for Razor Pages.", previewHtml, StringComparison.Ordinal);
        Assert.Contains("Скачать", previewHtml, StringComparison.Ordinal);
        _storage.Verify(
            service => service.MarkDownloadedAsync(
                "Sub/notes.txt",
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        using var searchResponse = await _client.GetAsync(
            "/storage-ui/index.html?q=notes&page=2&sort=size&direction=desc",
            cancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchHtml = await searchResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Поиск: notes", searchHtml, StringComparison.Ordinal);
        Assert.Contains("Страница 2 / 3", searchHtml, StringComparison.Ordinal);
        Assert.Contains("notes.txt", searchHtml, StringComparison.Ordinal);

        using var trashResponse = await _client.GetAsync(
            "/storage-ui/index.html?trash=true",
            cancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, trashResponse.StatusCode);
        var trashHtml = await trashResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Корзина", trashHtml, StringComparison.Ordinal);
        Assert.Contains("deleted.txt", trashHtml, StringComparison.Ordinal);
        Assert.Contains("Восстановить", trashHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostHandlers_ExecuteStorageAndGitOperations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var deleteResponse = await PostFormAsync(
            "Delete",
            cancellationToken,
            ("SelectedIds", _subEntry.Id.ToString()),
            ("DryRun", "false")
        );
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        using var moveResponse = await PostFormAsync(
            "Move",
            cancellationToken,
            ("SelectedIds", _subEntry.Id.ToString()),
            ("TargetDirectory", "Archive"),
            ("DryRun", "false")
        );
        Assert.Equal(HttpStatusCode.Redirect, moveResponse.StatusCode);

        using var restoreResponse = await PostFormAsync(
            "Restore",
            cancellationToken,
            ("SelectedIds", _trashEntry.Id.ToString()),
            ("DryRun", "false")
        );
        Assert.Equal(HttpStatusCode.Redirect, restoreResponse.StatusCode);

        using var syncResponse = await PostFormAsync(
            "SyncGit",
            cancellationToken,
            ("SelectedIds", _subEntry.Id.ToString())
        );
        Assert.Equal(HttpStatusCode.Redirect, syncResponse.StatusCode);

        using var deletePathResponse = await PostFormAsync(
            "DeletePath",
            cancellationToken,
            ("DeletePath", "Sub/notes.txt")
        );
        Assert.Equal(HttpStatusCode.Redirect, deletePathResponse.StatusCode);

        using var replaceContent = new MultipartFormDataContent();
        replaceContent.Add(new StringContent("Sub/notes.txt"), "ReplacementPath");
        var replacement = new ByteArrayContent(Encoding.UTF8.GetBytes("replacement text"));
        replacement.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        replaceContent.Add(replacement, "ReplacementFile", "replacement.txt");
        using var replaceResponse = await _client.PostAsync(
            "/storage-ui/index.html?handler=Replace",
            replaceContent,
            cancellationToken
        );
        Assert.Equal(HttpStatusCode.Redirect, replaceResponse.StatusCode);

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new StringContent("Uploads"), "TargetDirectory");
        var upload = new ByteArrayContent(Encoding.UTF8.GetBytes("new upload"));
        upload.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(upload, "UploadFiles", "new-upload.txt");
        using var uploadResponse = await _client.PostAsync(
            "/storage-ui/index.html?handler=Upload",
            uploadContent,
            cancellationToken
        );
        Assert.Equal(HttpStatusCode.Redirect, uploadResponse.StatusCode);

        using var indexResponse = await PostFormAsync("Index", cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, indexResponse.StatusCode);

        using var refreshResponse = await PostFormAsync("Refresh", cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, refreshResponse.StatusCode);

        _storage.Verify(
            service => service.SoftDeleteAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ),
            Times.AtLeast(3)
        );
        _storage.Verify(
            service => service.MoveAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                "Archive",
                false,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        _storage.Verify(
            service => service.RestoreAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                false,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        _storage.Verify(
            service => service.UploadAsync(
                It.IsAny<IReadOnlyCollection<MediaUploadFile>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(2)
        );
        _git.Verify(
            service => service.SyncAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.Is<IReadOnlyCollection<string>>(paths =>
                    paths.Contains("Sub/notes.txt", StringComparer.Ordinal)
                )
            ),
            Times.Once
        );
        _storage.Verify(
            service => service.IndexAsync(It.IsAny<CancellationToken>()),
            Times.AtLeast(2)
        );
    }

    [Fact]
    public void FileDisplayHelpers_ClassifyMediaAndFormatNames()
    {
        Assert.Equal("🖼️", IndexModel.GetTreeFileIcon("cover.png"));
        Assert.Equal("🎬", IndexModel.GetTreeFileIcon("clip.mp4"));
        Assert.Equal("🎵", IndexModel.GetTreeFileIcon("sound.mp3"));
        Assert.Equal("📕", IndexModel.GetTreeFileIcon("document.pdf"));
        Assert.Equal("📄", IndexModel.GetTreeFileIcon("unknown.bin"));

        Assert.Equal("cover", IndexModel.GetFileDisplayName("cover.png"));
        Assert.Equal(".gitignore", IndexModel.GetFileDisplayName(".gitignore"));
        Assert.Equal("root", IndexModel.GetTreeFileDisplayName("root.txt"));
        Assert.Equal("0 Б", IndexModel.FormatBytes(0));
        Assert.Equal("1 КБ", IndexModel.FormatBytes(1024));
        Assert.Equal("—", IndexModel.FormatDate(null));
    }

    private void ConfigureStorage()
    {
        _storage
            .Setup(service =>
                service.ListAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((bool includeDeleted, CancellationToken _) =>
                includeDeleted
                    ? new MediaStorageEntry[] { _trashEntry }
                    : new MediaStorageEntry[] { _rootEntry, _subEntry }
            );
        _storage
            .Setup(service =>
                service.ListDirectoryAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string directory, CancellationToken _) =>
                string.Equals(directory, "Sub", StringComparison.Ordinal)
                    ? new MediaStorageEntry[] { _subEntry }
                    : new MediaStorageEntry[] { _rootEntry }
            );
        _storage
            .Setup(service =>
                service.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string _, int page, int pageSize, CancellationToken _) =>
                new MediaStorageSearchResult(
                    new MediaStorageEntry[] { _subEntry },
                    102,
                    page,
                    pageSize
                )
            );
        _storage
            .Setup(service => service.IndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexResult(1, 1));
        _storage
            .Setup(service =>
                service.MarkDownloadedAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        _storage
            .Setup(service =>
                service.SoftDeleteAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, bool _, CancellationToken _) =>
                new BulkOperationResult(ids.Count, ids.Count, 0, [])
            );
        _storage
            .Setup(service =>
                service.RestoreAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, bool _, CancellationToken _) =>
                ids.Count
            );
        _storage
            .Setup(service =>
                service.MoveAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, string _, bool _, CancellationToken _) =>
                new BulkOperationResult(ids.Count, ids.Count, 0, [])
            );
        _storage
            .Setup(service =>
                service.UploadAsync(
                    It.IsAny<IReadOnlyCollection<MediaUploadFile>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((IReadOnlyCollection<MediaUploadFile> files, string _, CancellationToken _) =>
                new BulkOperationResult(files.Count, files.Count, 0, [])
            );
    }

    private void ConfigureBrowser()
    {
        _browser
            .Setup(service =>
                service.BrowseAsync(
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string? directory, CancellationToken _) =>
                string.Equals(directory, "Sub", StringComparison.Ordinal)
                    ? new MediaStorageBrowseResult(
                        "Sub",
                        "Sub",
                        null,
                        new[] { new MediaStorageBrowseDirectory("Nested", "Sub/Nested", true) },
                        new[] { ToBrowseFile(_subEntry) }
                    )
                    : new MediaStorageBrowseResult(
                        "wwwroot",
                        string.Empty,
                        null,
                        new[] { new MediaStorageBrowseDirectory("Sub", "Sub", true) },
                        new[]
                        {
                            ToBrowseFile(_rootEntry),
                            new MediaStorageBrowseFile("cover.png", "cover.png", 5, Now, null, null, false, null, null),
                            new MediaStorageBrowseFile("clip.mp4", "clip.mp4", 5, Now, null, null, false, null, null),
                            new MediaStorageBrowseFile("sound.mp3", "sound.mp3", 5, Now, null, null, false, null, null),
                            new MediaStorageBrowseFile("document.pdf", "document.pdf", 5, Now, null, null, false, null, null),
                        }
                    )
            );

        _git
            .Setup(service => service.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitStatusResult(true, new[] { "Sub/notes.txt" }, "master", null));
        _git
            .Setup(service =>
                service.SyncAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IReadOnlyCollection<string>?>()
                )
            )
            .ReturnsAsync(new GitSyncResult(true, true, true, true, null));
    }

    private async Task<HttpResponseMessage> PostFormAsync(
        string handler,
        CancellationToken cancellationToken,
        params (string Name, string Value)[] fields
    )
    {
        using var content = new FormUrlEncodedContent(
            fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value))
        );
        return await _client.PostAsync(
            $"/storage-ui/index.html?handler={Uri.EscapeDataString(handler)}",
            content,
            cancellationToken
        );
    }

    private static MediaStorageBrowseFile ToBrowseFile(MediaStorageEntry entry) =>
        new(
            entry.FileName,
            entry.Path,
            entry.SizeBytes,
            Now,
            entry.Id,
            null,
            true,
            entry.UploadedAt,
            entry.LastDownloadedAt
        );
}
