using MARS.MediaStorage.Services.Git;
using MARS.MediaStorage.Services.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Тестовое окружение для сервиса хранилища: реальная файловая система во
/// временном каталоге, реальный контекст EF поверх SQLite in-memory и
/// заглушка git, которая лишь считает вызовы — сеть в тестах не нужна.
/// </summary>
public sealed class StorageTestContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public StorageTestContext()
    {
        Root = Path.Combine(Path.GetTempPath(), "mars-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options =
            new DbContextOptionsBuilder<MARS.MediaStorage.DataBaseContext.MediaStorageDbContext>()
                .UseSqlite(_connection)
                .Options;

        Factory = new TestDbContextFactory(options);
        Git = new RecordingGitService();
        Now = new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.Zero);
        Time = new FakeTimeProvider(Now);

        using var db = Factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public string Root { get; }

    public DateTimeOffset Now { get; }

    public FakeTimeProvider Time { get; }

    public TestDbContextFactory Factory { get; }

    public RecordingGitService Git { get; }

    public MediaStorageService CreateService() =>
        new(
            Factory,
            Root,
            Git,
            Time,
            TimeSpan.FromDays(30),
            MaxUploadBytes,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MediaStorageService>.Instance
        );

    /// <summary>
    /// Лимит загрузки для тестов. По умолчанию большой, чтобы обычные файлы
    /// проходили; для проверки отказа по размеру создаётся сервис с меньшим.
    /// </summary>
    public long MaxUploadBytes { get; set; } = 95L * 1024 * 1024;

    public string WriteFile(string relativePath, string content = "x")
    {
        var full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public bool Exists(string relativePath) =>
        File.Exists(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    public void Dispose()
    {
        _connection.Dispose();

        try
        {
            if (Directory.Exists(Root))
            {
                foreach (
                    var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                )
                {
                    try
                    {
                        var a = File.GetAttributes(file);
                        if (a.HasFlag(FileAttributes.ReadOnly))
                        {
                            File.SetAttributes(file, a & ~FileAttributes.ReadOnly);
                        }
                    }
                    catch (IOException) { }
                }

                Directory.Delete(Root, true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

public sealed class TestDbContextFactory(
    DbContextOptions<MARS.MediaStorage.DataBaseContext.MediaStorageDbContext> options
) : IDbContextFactory<MARS.MediaStorage.DataBaseContext.MediaStorageDbContext>
{
    public MARS.MediaStorage.DataBaseContext.MediaStorageDbContext CreateDbContext() =>
        new(options);
}

/// <summary>
/// Заглушка git: считает коммиты, чтобы проверить, что каждое событие
/// хранилища порождает коммит, но не ходит в сеть.
/// </summary>
public sealed class RecordingGitService : IMediaGitService
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public int SyncCalls => _messages.Count;

    public Task<GitSyncResult> EnsureInitializedAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(new GitSyncResult(true, true, false, true, null));

    public Task<GitSyncResult> SyncAsync(
        string message,
        bool allowEmptyCommit = false,
        CancellationToken cancellationToken = default
    )
    {
        _messages.Add(message);
        return Task.FromResult(new GitSyncResult(true, true, true, true, null));
    }

    public Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitStatusResult(true, [], "master", null));
}

public sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}
