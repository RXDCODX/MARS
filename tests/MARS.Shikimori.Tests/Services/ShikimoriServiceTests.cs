using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.Shikimori.Controllers;
using MARS.Shikimori.Data;
using MARS.Shikimori.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shikimori.Tests.Services;

/// <summary>
/// Единственный владелец клиента Shikimori: перевод ответа в доменные ссылки.
///
/// Проверяется то, ради чего класс и написан: наружу уходит абсолютная ссылка и
/// русское название, а произведения персонажа — самые короткие из доступных.
/// Настоящий клиент уходит в сеть и подменён фейком.
/// </summary>
public class ShikimoriServiceTests
{
    private readonly StubClient _client = new();
    private readonly StubRateLimiter _limiter = new();
    private readonly ShikimoriService _service;

    public ShikimoriServiceTests() =>
        _service = new ShikimoriService(
            NullLogger<ShikimoriService>.Instance,
            Options.Create(new ShikimoriClientOptions { ShikimoriSite = "https://shikimori.one" }),
            _limiter,
            _client
        );

    [Fact]
    public async Task AnimeUrlIsAbsolute()
    {
        _client.Anime = new AnimeRef(123, "Наруто", "Наруто", 2002);

        var anime = await _service.GetRandomAnimeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("https://shikimori.one/animes/123", anime!.Url);
    }

    [Fact]
    public async Task RussianNameIsPreferred()
    {
        _client.Anime = new AnimeRef(1, "Kimetsu", "Кимэцу", 2019);

        var anime = await _service.GetRandomAnimeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Кимэцу", anime!.RussianName);
    }

    [Fact]
    public async Task MissingAnimeYieldsNull()
    {
        _client.Anime = null;

        Assert.Null(await _service.GetRandomAnimeAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MangaUrlIsAbsolute()
    {
        _client.Manga = new MangaRef(77, "Berserk", "Берсерк", 1989);

        var manga = await _service.GetRandomMangaAsync(TestContext.Current.CancellationToken);

        Assert.Equal("https://shikimori.one/mangas/77", manga!.Url);
    }

    [Fact]
    public async Task MissingMangaYieldsNull()
    {
        Assert.Null(await _service.GetRandomMangaAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Рейт-лимит запрашивается до похода в Shikimori: без этого сервис упирался
    /// бы в лимит запросов и получал 429.
    /// </summary>
    [Fact]
    public async Task RateLimiterIsWaitedBeforeEachCall()
    {
        _client.Anime = new AnimeRef(1, "A", "A", 2000);
        _client.Manga = new MangaRef(1, "M", "M", 2000);

        await _service.GetRandomAnimeAsync(TestContext.Current.CancellationToken);
        await _service.GetRandomMangaAsync(TestContext.Current.CancellationToken);
        await _service.GetCharacterAsync(1, TestContext.Current.CancellationToken);

        Assert.Equal(3, _limiter.Calls);
    }

    [Fact]
    public async Task CharacterUsesShortestAnimeTitle()
    {
        _client.Character = new CharacterDetails(
            42,
            "Наруто",
            "Наруто",
            "описание",
            "https://shikimori.one/images/42.jpg",
            ["Kimetsu no Yaiba", "Наруто", "Bleach"],
            ["Берсерк"]
        );

        var character = await _service.GetCharacterAsync(42, TestContext.Current.CancellationToken);

        Assert.Equal("Наруто", character!.AnimeTitle);
        Assert.Equal("Берсерк", character.MangaTitle);
    }

    [Fact]
    public async Task CharacterWithoutTitlesYieldsEmptyNames()
    {
        _client.Character = new CharacterDetails(42, "X", null, null, null, [], []);

        var character = await _service.GetCharacterAsync(42, TestContext.Current.CancellationToken);

        Assert.Null(character!.AnimeTitle);
        Assert.Null(character.MangaTitle);
    }

    [Fact]
    public async Task CharacterImageUrlIsAbsolute()
    {
        _client.Character = new CharacterDetails(42, "X", null, null, "/images/42.jpg", [], []);

        var character = await _service.GetCharacterAsync(42, TestContext.Current.CancellationToken);

        Assert.Equal("https://shikimori.one/images/42.jpg", character!.ImageUrl);
    }

    /// <summary>
    /// Неположительный id не доходит до клиента: это ошибка запроса, а не отказ
    /// внешнего сервиса, и рейт-лимит на неё тратить незачем.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveCharacterIdIsNotRequested(long id)
    {
        _client.Character = new CharacterDetails(1, "X", null, null, null, [], []);

        Assert.Null(await _service.GetCharacterAsync(id, TestContext.Current.CancellationToken));
        Assert.Equal(0, _limiter.Calls);
    }

    [Fact]
    public async Task MissingCharacterYieldsNull()
    {
        Assert.Null(await _service.GetCharacterAsync(42, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Отказ Shikimori не выходит наружу: метод возвращает null, и вызывающий
    /// отдаст конверт с сообщением, а не HTTP 500.
    /// </summary>
    [Fact]
    public async Task UnavailableShikimoriYieldsNull()
    {
        _client.Failure = new HttpRequestException("Shikimori недоступен");

        Assert.Null(await _service.GetRandomAnimeAsync(TestContext.Current.CancellationToken));
        Assert.Null(await _service.GetRandomMangaAsync(TestContext.Current.CancellationToken));
        Assert.Null(await _service.GetCharacterAsync(42, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RateLimiterInfoIsPassedToAdminPanel()
    {
        _limiter.Info = new RateLimiterInfo
        {
            AvailablePerSecond = 2,
            AvailablePerMinute = 30,
            TimeToResetSecond = TimeSpan.FromSeconds(1.5),
            TimeToResetMinute = TimeSpan.FromMinutes(2),
        };

        var info = _service.GetRateLimiterInfo();

        Assert.Equal(2, info.AvailablePerSecond);
        Assert.Equal(30, info.AvailablePerMinute);
        Assert.Equal(1.5, info.SecondsToResetSecondWindow);
    }

    /// <summary>
    /// Фейк клиента вместо ShikimoriSharp: сети в тесте быть не должно, а
    /// проверяется перевод ответа, а не доступность внешнего API.
    /// </summary>
    private sealed class StubClient : IShikimoriClient
    {
        public AnimeRef? Anime { get; set; }

        public MangaRef? Manga { get; set; }

        public CharacterDetails? Character { get; set; }

        public Exception? Failure { get; set; }

        public Task<AnimeRef?> GetRandomAnimeAsync(
            int minimumScore,
            CancellationToken cancellationToken = default
        ) => Failure is null ? Task.FromResult(Anime) : Task.FromException<AnimeRef?>(Failure);

        public Task<MangaRef?> GetRandomMangaAsync(
            int minimumScore,
            CancellationToken cancellationToken = default
        ) => Failure is null ? Task.FromResult(Manga) : Task.FromException<MangaRef?>(Failure);

        public Task<CharacterDetails?> GetCharacterAsync(
            long id,
            CancellationToken cancellationToken = default
        ) =>
            Failure is null
                ? Task.FromResult(Character)
                : Task.FromException<CharacterDetails?>(Failure);
    }

    private sealed class StubRateLimiter : IShikimoriRateLimiter
    {
        public int Calls { get; private set; }

        public RateLimiterInfo Info { get; set; } = new();

        public Task<bool> TryAcquireAsync()
        {
            Calls++;

            return Task.FromResult(true);
        }

        public Task WaitForSlotAsync(CancellationToken cancellationToken = default)
        {
            Calls++;

            return Task.CompletedTask;
        }

        public RateLimiterInfo GetInfo() => Info;
    }
}
