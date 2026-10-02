using MARS.Shikimori.Data;
using MARS.Shikimori.Entities;
using MARS.Shikimori.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Shikimori.Tests;

/// <summary>
/// Хранение выбранного на Shikimori — пункт W3. Провайдер — SQLite in-memory:
/// проверяется поведение (upsert, история выдачи), а не диалект PostgreSQL.
/// </summary>
public class ShikimoriCatalogTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<ShikimoriDbContext> _factory;
    private readonly ShikimoriCatalog _catalog;

    public ShikimoriCatalogTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ShikimoriDbContext>()
            .UseSqlite(_connection)
            .Options;

        _factory = new TestDbContextFactory(options);

        using var dbContext = new ShikimoriDbContext(options);
        dbContext.Database.EnsureCreated();
        _catalog = new ShikimoriCatalog(_factory, NullLogger<ShikimoriCatalog>.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestDbContextFactory(DbContextOptions<ShikimoriDbContext> options)
        : IDbContextFactory<ShikimoriDbContext>
    {
        public ShikimoriDbContext CreateDbContext() => new(options);
    }

    private static Shared.Clients.ShikimoriCharacterRef Character(
        string animeTitle = "Название аниме",
        string mangaTitle = "Название манги"
    ) =>
        new(
            42,
            "Naruto",
            "Наруто",
            "Описание",
            "https://shikimori.one/images/42/original.jpg",
            "/images/42/original.jpg",
            animeTitle,
            mangaTitle
        );

    [Fact]
    public async Task SaveCharacterAsync_StoresTheCharacter()
    {
        var ct = TestContext.Current.CancellationToken;

        var result = await _catalog.SaveCharacterAsync(Character(), ct);

        Assert.Equal(42, result.Id);
        Assert.Equal("Наруто", result.RussianName);

        await using var dbContext = _factory.CreateDbContext();
        var stored = await dbContext.Characters.SingleAsync(entity => entity.Id == 42, ct);

        Assert.Equal("Naruto", stored.Name);
        Assert.Equal("Описание", stored.Description);
        Assert.Equal("https://shikimori.one/images/42/original.jpg", stored.ImageUrl);
    }

    /// <summary>
    /// Повторный запрос того же персонажа обновляет строку, а не плодит новые:
    /// иначе история разрасталась бы дублями на каждый запрос.
    /// </summary>
    [Fact]
    public async Task SaveCharacterAsync_UpdatesTheExistingRow()
    {
        var ct = TestContext.Current.CancellationToken;

        await _catalog.SaveCharacterAsync(Character(animeTitle: "Старое"), ct);
        await _catalog.SaveCharacterAsync(
            Character(animeTitle: "Новое") with
            {
                RussianName = "Наруто 2",
            },
            ct
        );

        await using var dbContext = _factory.CreateDbContext();
        var stored = await dbContext.Characters.SingleAsync(entity => entity.Id == 42, ct);

        Assert.Equal("Новое", stored.AnimeTitle);
        Assert.Equal("Наруто 2", stored.RussianName);
    }

    /// <summary>
    /// Ответ зрителя не должен зависеть от того, записалась ли история: сначала
    /// данные Shikimori, потом попытка их сохранить.
    /// </summary>
    [Fact]
    public async Task SaveCharacterAsync_ReturnsTheCharacter_EvenWhenStorageFails()
    {
        var catalog = new ShikimoriCatalog(
            new FailingDbContextFactory(),
            NullLogger<ShikimoriCatalog>.Instance
        );
        var ct = TestContext.Current.CancellationToken;

        var result = await catalog.SaveCharacterAsync(Character(), ct);

        Assert.Equal(42, result.Id);
        Assert.Equal("Naruto", result.Name);
    }

    [Fact]
    public async Task RecordPickAsync_KeepsTheHistoryOfWhatWasShown()
    {
        var ct = TestContext.Current.CancellationToken;

        await _catalog.RecordPickAsync(
            "anime",
            new Shared.Clients.ShikimoriTitleRef(
                1,
                "Name",
                "Имя",
                2010,
                "https://shikimori.one/animes/1"
            ),
            ct
        );
        await _catalog.RecordPickAsync(
            "manga",
            new Shared.Clients.ShikimoriTitleRef(
                2,
                "Name2",
                "Имя2",
                2011,
                "https://shikimori.one/mangas/2"
            ),
            ct
        );

        await using var dbContext = _factory.CreateDbContext();
        var picks = await dbContext.TitlePicks.OrderBy(pick => pick.Kind).ToListAsync(ct);

        Assert.Equal(2, picks.Count);
        Assert.Equal(["anime", "manga"], picks.Select(pick => pick.Kind));
        Assert.Equal(2010, picks[0].Year);
    }

    [Fact]
    public async Task FindCharacterAsync_ReturnsNull_ForAnUnknownCharacter()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Null(await _catalog.FindCharacterAsync(999, ct));
    }

    [Fact]
    public async Task FindCharacterAsync_ReturnsWhatWasSavedBefore()
    {
        var ct = TestContext.Current.CancellationToken;
        await _catalog.SaveCharacterAsync(Character(), ct);

        var result = await _catalog.FindCharacterAsync(42, ct);

        Assert.NotNull(result);
        Assert.Equal("Название аниме", result.AnimeTitle);
        Assert.Equal("Название манги", result.MangaTitle);
    }

    private sealed class FailingDbContextFactory : IDbContextFactory<ShikimoriDbContext>
    {
        public ShikimoriDbContext CreateDbContext() =>
            throw new InvalidOperationException("БД недоступна");
    }
}
